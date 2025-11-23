using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;

namespace DevCardsManager.Ui.Controls;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;
    private readonly Logger _logger;

    public SettingsViewModel(SettingsManager settingsManager, Logger logger)
    {
        _settingsManager = settingsManager;
        _logger = logger;

        ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);
    }

    public void UpdateParameters()
    {
        foreach (var parameter in Parameters)
            parameter.PropertyChanged -= OnParameterChanged;

        Parameters = _settingsManager.Settings.ToParameters(_logger);
        this.RaisePropertyChanged(nameof(Parameters));

        foreach (var parameter in Parameters)
            parameter.PropertyChanged += OnParameterChanged;
    }

    private void OnParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        var newValue = sender switch
        {
            StringParameterViewModel stringParameter => stringParameter.Value,
            IntegerParameterViewModel integerParameter => integerParameter.Value.ToString(),
            BooleanParameterViewModel booleanParameter => booleanParameter.Value.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(sender), "Unknown parameter type!")
        };
        _logger.LogInfo($"Update parameter: '{e.PropertyName}', new value: '{newValue}'");

        _settingsManager.SaveSettings(Parameters.ToSettings(Settings.PinnedCards, Settings.LogRowPatternsToRemove, Settings.LogRowPatternsToLeave));

        if (e.PropertyName == nameof(Settings.UseDarkTheme))
            ActualizeTheme();

        if (e.PropertyName == nameof(Settings.SortAscending))
            ActualizeTheme();
    }

    public Settings Settings => _settingsManager.Settings;
    public List<ParameterViewModel> Parameters { get; private set; } = [];
    public ICommand ToggleThemeCommand { get; }

    private void ToggleTheme()
    {
        Settings.UseDarkTheme = !Settings.UseDarkTheme;
        _settingsManager.SaveSettings();
        ActualizeTheme();
    }

    public void ActualizeTheme()
    {
        Application.Current!.RequestedThemeVariant = Settings.UseDarkTheme
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}