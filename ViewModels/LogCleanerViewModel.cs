using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DevCardsManager.ViewModels;

public sealed class LogFilterViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;

    public LogFilterViewModel(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        PatternsToRemove.AddRange(settingsManager.Settings.LogRowPatternsToRemove);
        PatternsToLeave.AddRange(settingsManager.Settings.LogRowPatternsToLeave);
        foreach (var pattern in PatternsToRemove.Union(PatternsToLeave))
            pattern.Changed += OnLogItemPatternChanged;

        RemoveMode = true;
        LeaveOnlyMode = false;

        ProcessClipboardCommand = ReactiveCommand.CreateFromTask(ProcessClipboardAsync);
        AddLogItemPatternCommand = ReactiveCommand.Create(AddLogItemPattern);
        RemoveLogItemPatternCommand = new DelegateCommand<LogItemPattern>(RemoveLogItemPattern);
    }

    public ObservableCollection<LogItemPattern> PatternsToRemove { get; } = [];
    public ObservableCollection<LogItemPattern> PatternsToLeave { get; } = [];


    [Reactive]
    public bool RemoveMode { get; set; }

    [Reactive]
    public bool LeaveOnlyMode { get; set; }

    [Reactive]
    public string? SuccessText { get; set; }

    [Reactive]
    public string? ErrorText { get; set; }

    public Func<string, Task> AddToClipboardAsync { get; set; } = null!;
    public Func<Task<string?>> ReadClipboardAsync { get; set; } = null!;

    public ICommand ProcessClipboardCommand { get; set; }

    public ICommand AddLogItemPatternCommand { get; set; }

    public ICommand RemoveLogItemPatternCommand { get; set; }

    private async Task ProcessClipboardAsync()
    {
        if (RemoveMode == LeaveOnlyMode)
            throw new InvalidOperationException();

        SuccessText = null;
        ErrorText = null;

        var clipboardText = await ReadClipboardAsync.Invoke();
        var rows = clipboardText?.Split(Environment.NewLine).ToList() ?? [];

        if (!rows.Any())
        {
            ErrorText = "Буфер обмена пустой";
            return;
        }

        var deletedRowCounter = 0;
        var stringBuilder = new StringBuilder();

        if (RemoveMode)
        {
            var patternsToRemove = PatternsToRemove
                .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.Content))
                .Select(p => p.Content!)
                .ToHashSet();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                if (!patternsToRemove.Any(pattern => row.Contains(pattern)))
                    stringBuilder.AppendLine(row);
                else
                    deletedRowCounter++;
            }
        }
        else if (LeaveOnlyMode)
        {
            var patternsToLeave = PatternsToLeave
                .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.Content))
                .Select(p => p.Content!)
                .ToHashSet();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                if (patternsToLeave.Any(pattern => row.Contains(pattern)))
                    stringBuilder.AppendLine(row);
                else
                    deletedRowCounter++;
            }
        }

        if (deletedRowCounter == 0)
        {
            ErrorText = "Удалено 0 строк";
            return;
        }

        SuccessText = $"Удалено {deletedRowCounter} строк";

        _ = AddToClipboardAsync(stringBuilder.ToString());
    }

    private void AddLogItemPattern()
    {
        var pattern = new LogItemPattern { IsActive = true };
        pattern.Changed += OnLogItemPatternChanged;

        if (RemoveMode)
            PatternsToRemove.Add(pattern);
        if (LeaveOnlyMode)
            PatternsToLeave.Add(pattern);
    }

    private void RemoveLogItemPattern(LogItemPattern pattern)
    {
        pattern.Changed -= OnLogItemPatternChanged;
        if (RemoveMode)
            PatternsToRemove.Remove(pattern);
        if (LeaveOnlyMode)
            PatternsToLeave.Remove(pattern);
    }

    private void OnLogItemPatternChanged()
    {
        if (RemoveMode)
        {
            _settingsManager.Settings.LogRowPatternsToRemove.Clear();
            _settingsManager.Settings.LogRowPatternsToRemove.AddRange(PatternsToRemove.Where(p => !string.IsNullOrWhiteSpace(p.Content)));
        }

        if (LeaveOnlyMode)
        {
            _settingsManager.Settings.LogRowPatternsToLeave.Clear();
            _settingsManager.Settings.LogRowPatternsToLeave.AddRange(PatternsToLeave.Where(p => !string.IsNullOrWhiteSpace(p.Content)));
        }

        _settingsManager.SaveSettings();
    }
}