using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Transactions;

public class Program
{
    private static string _producerName = string.Empty;
    private static Assembly? _producerLib;
    private static Type? _producerType;
    private static object? _producer;
    private static MethodInfo? _produceMessageMethod;
    private static int _rateLimit;
    private static int _retryLimit;
    private static readonly HttpClient _httpClient = new HttpClient();
    public static async Task Main(string[] args)
    {
        try
        {
            Console.Write("Enter Producer's Name: ");
            _producerName = Console.ReadLine() ?? throw new ArgumentNullException("Producer name not found.");

            _producerLib = Assembly.LoadFrom("ProducerLib")
                ?? throw new FileNotFoundException("ProducerLib.dll not found.");

            _producerType = _producerLib.GetType("ProducerLib.Class.Producer")
                ?? throw new TypeLoadException("Producer type not found.");

            _producer = Activator.CreateInstance(_producerType)
                ?? throw new TypeLoadException("Producer type not found.");

            _rateLimit = GetAttribute<int>("RateLimit");
            Console.WriteLine($"Rate limit: {_rateLimit}");

            _retryLimit = GetAttribute<int>("RetryLimit");
            Console.WriteLine($"Retry limit: {_retryLimit}");

            _produceMessageMethod = _producerType.GetMethod("RandomObjectProducer")
                ?? throw new Exception("ProduceMessage method not found.");

            await new Program().ProduceMessage();
        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
    }

    public async Task ProduceMessage()
    {
        try
        {
            var semaphore = new SemaphoreSlim(_rateLimit);
            while (true)
            {
                await semaphore.WaitAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var message = _produceMessageMethod?.Invoke(_producer, null) ?? throw new Exception("Message not found.");
                        Logger.Log(Logger.LogLevel.Info, $"Message({message.GetType()}) produced: {message.ToString()}");
                        while (!await SendTask(message))
                        {
                            Logger.Log(Logger.LogLevel.Warning, $"Failed to send message({message.GetType()}): {message.ToString()}");
                            Thread.Sleep(5000);
                        }
                        Logger.Log(Logger.LogLevel.Info, $"Message({message.GetType()}) sent successfully: {message.ToString()}");
                    }
                    finally
                    {
                        Logger.Log(Logger.LogLevel.Info, "Semaphore released.");
                        semaphore.Release();
                    }
                });
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
    }
    public async Task<bool> SendTask(object message)
    {
        try
        {
            Logger.Log(Logger.LogLevel.Info, $"Sending message({message.GetType()}): {message.ToString()}");
            var content = new StringContent(JsonSerializer.Serialize(new { producerName = _producerName, message = message.ToString() }), Encoding.UTF8, "application/json");
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                for (int i = 0; i < _retryLimit; i++)
                {
                    var response = await _httpClient.PostAsync("http://localhost:5000/api/broker/publish", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(Logger.LogLevel.Info, $"Message({message.GetType()}) sent: {message.ToString()}");
                        transaction.Complete();
                        return true;
                    }
                    else
                    {
                        Logger.Log(Logger.LogLevel.Warning, $"Failed in {i + 1}th try for message({message.GetType()}): {message.ToString()}");
                    }
                }
                Logger.Log(Logger.LogLevel.Error, $"Failed to send message({message.GetType()}): {message.ToString()}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
        return false;
    }

    private static T GetAttribute<T>(string attributeName)
    {
        try
        {
            var type = _producerLib?.GetType($"ProducerLib.AttributeFile.{attributeName}Attribute");

            var attribute = _producerType?.GetCustomAttribute(type)
                ?? throw new Exception($"{attributeName} type not found.");

            var property = attribute.GetType().GetProperty(attributeName)
                ?? throw new Exception($"{attributeName} property not found.");

            var value = property.GetValue(attribute)
                ?? throw new Exception($"{attributeName} value not found.");

            return (T)value;
        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
        return default;
    }
}
