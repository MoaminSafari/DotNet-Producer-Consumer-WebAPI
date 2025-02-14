using System.Collections.Concurrent;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace MessageBroker.Controller
{
    [ApiController]
    [Route("api/broker")]
    public class MessageBrokerController : ControllerBase
    {
        private static readonly Dictionary<string, ConcurrentQueue<object>> _queues = new();
        private static readonly object _fileLock = new object();

        static MessageBrokerController()
        {
            Logger.Log(Logger.LogLevel.Info, "Loading queues from file...");
            LoadQueueFromFile();
        }

        [HttpPost("publish")]
        public IActionResult Publish([FromBody] PublishRequest request)
        {
            try
            {
                Logger.Log(Logger.LogLevel.Info, $"Publish request received from {request.ProducerName}");
                using (var scope = new TransactionScope())
                {
                    if (!_queues.ContainsKey(request.ProducerName))
                    {
                        Logger.Log(Logger.LogLevel.Info, $"Creating new queue for {request.ProducerName}");
                        _queues[request.ProducerName] = new();
                    }
                    Logger.Log(Logger.LogLevel.Info, $"Enqueuing message from {request.ProducerName}");
                    _queues[request.ProducerName].Enqueue(request.Message);

                    BackupQueueToFile();
                    Logger.Log(Logger.LogLevel.Info, $"From {request.ProducerName}, Message {request.Message} received");
                    scope.Complete();
                    return Ok("Message published");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogLevel.Error, ex.Message);
                return StatusCode(500);
            }
        }

        [HttpGet("consume")]
        public IActionResult Consume([FromQuery] string producer)
        {
            try
            {
                Logger.Log(Logger.LogLevel.Info, $"Consume request received for {producer}");
                using (var scope = new TransactionScope())
                {
                    if (_queues.TryGetValue(producer, out var queue) && queue.TryDequeue(out var message))
                    {
                        BackupQueueToFile();
                        Logger.Log(Logger.LogLevel.Info, $"Message sent: {message}");
                        scope.Complete();
                        return Ok(message);
                    }
                    Logger.Log(Logger.LogLevel.Info, $"No message found for {producer}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogLevel.Error, ex.Message);
                return StatusCode(500);
            }
            return NoContent();
        }

        [HttpGet("queue")]
        public IActionResult GetQueues()
        {
            try
            {
                Logger.Log(Logger.LogLevel.Info, "Queue request received");
                return Ok(_queues.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogLevel.Error, ex.Message);
                return StatusCode(500);
            }
        }

        private static void BackupQueueToFile()
        {
            try
            {
                Logger.Log(Logger.LogLevel.Info, "Backing up queues to file...");
                lock (_fileLock)
                {
                    foreach (var producer in _queues.Keys)
                    {
                        var queueArray = _queues[producer].ToArray();
                        var json = JsonSerializer.Serialize(queueArray);
                        System.IO.File.WriteAllText($"{producer}_queue_backup.json", json);
                        Logger.Log(Logger.LogLevel.Info, $"Queue backed up for {producer}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogLevel.Error, ex.Message);
            }
        }

        private static void LoadQueueFromFile()
        {
            try
            {
                Logger.Log(Logger.LogLevel.Info, "Loading queues from file...");
                var backupFiles = Directory.GetFiles(".", "*_queue_backup.json");
                foreach (var backupFilePath in backupFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(backupFilePath);
                    var producer = fileName.Substring(0, fileName.Length - "_queue_backup".Length);

                    Logger.Log(Logger.LogLevel.Info, $"Loading backup for {producer}");
                    var json = System.IO.File.ReadAllText(backupFilePath);
                    var queueArray = JsonSerializer.Deserialize<object[]>(json);
                    if (queueArray == null)
                    {
                        Logger.Log(Logger.LogLevel.Warning, $"Backup file for {producer} is empty");
                        continue;
                    }

                    if (!_queues.ContainsKey(producer))
                    {
                        Logger.Log(Logger.LogLevel.Info, $"Creating new queue for {producer}");
                        _queues[producer] = new();
                    }
                    foreach (var item in queueArray)
                    {
                        Logger.Log(Logger.LogLevel.Info, $"Enqueuing item for {producer}");
                        _queues[producer].Enqueue(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogLevel.Error, ex.Message);
            }
        }

    }

    public class PublishRequest
    {
        public string ProducerName { get; set; }
        public object Message { get; set; }
    }

}
