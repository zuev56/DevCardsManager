using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;

namespace DevCardsManager.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly Logger _logger;
    private const string AppsettingsFileName = "appsettings.json";

    public SettingsViewModel(Logger logger)
    {
        _logger = logger;
        InitializeSettings();

        UpdateParameters();

        ToggleThemeCommand = new RelayCommand(ToggleTheme);
    }

    public void UpdateParameters()
    {
        foreach (var parameter in Parameters)
            parameter.PropertyChanged -= Parameter_OnChanged;

        Parameters = Settings.ToParameters();
        OnPropertyChanged(nameof(Parameters));

        foreach (var parameter in Parameters)
            parameter.PropertyChanged += Parameter_OnChanged;
    }

    private void Parameter_OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        Settings = Parameters.ToSettings(Settings.PinnedCards);

        SaveSettings();

        if (e.PropertyName == nameof(Settings.UseDarkTheme))
            ActualizeTheme();

        if (e.PropertyName == nameof(Settings.SortAscending))
            ActualizeTheme();
    }

    public Settings Settings { get; private set; } = null!;
    public List<ParameterViewModel> Parameters { get; private set; } = [];
    public ICommand ToggleThemeCommand { get; }

    private void InitializeSettings()
    {
        Settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(AppsettingsFileName, optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .Get<Settings>()!;
    }

    internal void SaveSettings()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var settingsContent = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), AppsettingsFileName), settingsContent);

            OnPropertyChanged(nameof(Settings));
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
        finally
        {
            _logger.LogPerformance(stopwatch.Elapsed);
        }
    }

    private void ToggleTheme()
    {
        Settings.UseDarkTheme = !Settings.UseDarkTheme;
        SaveSettings();
        ActualizeTheme();
    }

    public void ActualizeTheme()
    {
        Application.Current!.RequestedThemeVariant = Settings.UseDarkTheme
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}