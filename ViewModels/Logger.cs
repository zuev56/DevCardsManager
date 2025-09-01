using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ReactiveUI;

namespace DevCardsManager.ViewModels;

// TODO: Переделать на стандартный логгер
public sealed class Logger : ViewModelBase
{
    private static class LogLevel
    {
        public const string Trace = "TRC";
        public const string Information = "INF";
        public const string Warning = "WRN";
        public const string Error = "ERR";
    }

    private string _log = string.Empty;
    private static readonly Lock Locker = new();

    public bool DetailedLogging { get; set; }

    public string Log
    {
        get => _log;
        private set
        {
            _log = value;
            this.RaisePropertyChanged();
        }
    }

    public void LogInfo(string message)
        => LogMessage(LogLevel.Information, message);

    public void LogWarning(string message)
        => LogMessage(LogLevel.Warning, message);

    public void LogError(string message)
        => LogMessage(LogLevel.Error, message);

    public void LogException(Exception exception) =>
        LogError($"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");

    public void LogPerformance(TimeSpan duration, string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string sourceFilePath = "")
    {
        var logLevel = duration.TotalMilliseconds switch
        {
            <= 200 => LogLevel.Trace,
            _ => LogLevel.Warning
        };

        var className = Path.GetFileNameWithoutExtension(sourceFilePath);
        LogMessage(logLevel, $"{className}.{methodName} ran for {duration.TotalMilliseconds:F2} ms{(string.IsNullOrWhiteSpace(message) ? "" : $". {message}")}");
    }

    private void LogMessage(string logLevel, string message)
    {
        if (logLevel == LogLevel.Trace && !DetailedLogging)
            return;

        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {logLevel}: {message}";
        Console.WriteLine(text);

        Log += string.IsNullOrWhiteSpace(Log)
            ? text
            : $"{Environment.NewLine}{text}";

        try
        {
            lock (Locker)
            {
                File.AppendAllLines("log.txt", [text]);
            }
        }
        catch (Exception e)
        {
            Log += $"{e.GetType().Name}: {e.Message}{Environment.NewLine}{e.StackTrace}";
            Console.WriteLine(e);
            throw;
        }
    }
}