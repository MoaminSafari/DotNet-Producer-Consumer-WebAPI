using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Transactions;
using ConsumerLib.Class;

public class Program
{
    private static string _targetProducer = string.Empty;
    private static Assembly? _consumerLib;
    private static Type? _consumerType;
    private static object? _consumer;
    private static MethodInfo? _printMethod;
    private static int _rateLimit;
    private static readonly HttpClient _httpClient = new HttpClient();
    public static async Task Main(string[] args)
    {
        try
        {
            Console.Write("Enter Target Producer's Name: ");
            _targetProducer = Console.ReadLine() ?? throw new ArgumentNullException("Producer name not found.");

            _consumerLib = Assembly.LoadFrom("ConsumerLib")
                ?? throw new FileNotFoundException("ConsumerLib.dll not found.");

            _consumerType = _consumerLib.GetType("ConsumerLib.Class.Consumer")
                ?? throw new TypeLoadException("Consumer type not found.");

            _consumer = Activator.CreateInstance(_consumerType)
                ?? throw new TypeLoadException("Consumer type not found.");

            _printMethod = _consumerType.GetMethod("Printer")
                ?? throw new MissingMethodException("ConsumeMessage method not found.");

            _rateLimit = GetAttribute<int>("RateLimit");
            Console.WriteLine($"Rate limit: {_rateLimit}");

            await new Program().ConsumeMessage();

        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
    }

    public async Task ConsumeMessage()
    {
        try
        {
            Logger.Log(Logger.LogLevel.Info, "Consumer started.");
            var semaphore = new SemaphoreSlim(_rateLimit);
            while (true)
            {
                await semaphore.WaitAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _printMethod.Invoke(_consumer, new object[] { await RecieveTask() });
                        Thread.Sleep(5000);
                    }
                    finally
                    {
                        Logger.Log(Logger.LogLevel.Info, "Message consumed.");
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

    public async Task<object> RecieveTask()
    {
        try
        {
            Logger.Log(Logger.LogLevel.Info, "Recieving message from broker.");
            var response = await _httpClient.GetAsync($"http://localhost:5000/api/broker/consume?producer={_targetProducer}");
            response.EnsureSuccessStatusCode();
            string message = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(message))
            {
                Logger.Log(Logger.LogLevel.Warning, "Queue is empty or Message is null");
                return false;
            }
            var obj = JsonSerializer.Deserialize<object>(message);
            Logger.Log(Logger.LogLevel.Info, $"Message recieved: {obj}");
            return obj;
        }
        catch (Exception ex)
        {
            Logger.Log(Logger.LogLevel.Error, ex.Message);
        }
        return null;
    }

    private static T GetAttribute<T>(string attributeName)
    {
        try
        {
            var type = _consumerLib?.GetType($"ConsumerLib.AttributeFile.{attributeName}Attribute");

            var attribute = _consumerType?.GetCustomAttribute(type)
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
