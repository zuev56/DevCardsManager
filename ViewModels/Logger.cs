using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCardsManager.ViewModels;

public class Logger : ObservableObject
{
    private string _log = string.Empty;

    public string Log
    {
        get => _log;
        private set
        {
            _log = value;
            OnPropertyChanged();
        }
    }

    public void LogInfo(string message)
        => LogMessage($"INF: {message}");

    public void LogError(string message)
        => LogMessage($"ERR: {message}");

    public void LogException(Exception exception) =>
        LogError($"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");

    private void LogMessage(string message)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        Console.WriteLine(text);
        Log += $"{Environment.NewLine}{text}";
    }
}