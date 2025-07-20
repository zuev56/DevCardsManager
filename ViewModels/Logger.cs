using System;

namespace DevCardsManager.ViewModels;

internal static class Logger
{
    public static string Log { get; private set; } = string.Empty;

    public static void LogMessage(string message)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        Console.WriteLine(text);
        Log += $"{Environment.NewLine}{text}";
    }

    public static void LogException(Exception exception)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff]}] {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
        Console.WriteLine(text);
        Log += $"{Environment.NewLine}{text}";
    }
}