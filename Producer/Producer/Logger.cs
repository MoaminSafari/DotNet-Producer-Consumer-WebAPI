using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Logger
{
    private static readonly object _lock = new object();
    private static string logFilePath = "logs/app_log.txt";

    public enum LogLevel { Info, Warning, Error }

    static Logger()
    {
        Directory.CreateDirectory("logs");
    }

    public static void Log(LogLevel level, string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        Console.WriteLine(logEntry);

        lock (_lock)
        {
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
    }
}