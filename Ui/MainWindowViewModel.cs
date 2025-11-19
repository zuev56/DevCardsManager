using System;
using DevCardsManager.Models;
using DevCardsManager.Services;
using DevCardsManager.Ui.Controls;

namespace DevCardsManager.Ui;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DirectoryWatcher _directoryWatcher;
    private readonly SettingsManager _settingsManager;

    public MainWindowViewModel(DirectoryWatcher directoryWatcher, SettingsManager settingsManager,
        CardManager cardManager, Logger logger, SettingsViewModel settingsViewModel,
        CardManagerViewModel cardManagerViewModel, UtilitiesViewModel utilitiesViewModel)
    {
        _directoryWatcher = directoryWatcher;
        _settingsManager = settingsManager;

        CardManager = cardManager;
        Logger = logger;
        SettingsViewModel = settingsViewModel;
        UtilitiesViewModel = utilitiesViewModel;
        CardManagerViewModel = cardManagerViewModel;
        try
        {
            ApplySettingsOnStartup();
            SettingsViewModel.ActualizeTheme();
            _settingsManager.ParameterChanged += SettingsParameterChanged;
        }
        catch (Exception e)
        {
            Logger!.LogException(e);
        }
    }

    private Settings Settings => _settingsManager.Settings;
    public Logger Logger { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public UtilitiesViewModel UtilitiesViewModel { get; }
    public CardManagerViewModel CardManagerViewModel { get; }
    public CardManager CardManager { get; }

    public int SelectedTabIndex
    {
        set
        {
            switch (value)
            {
                case 0: CardManagerViewModel.ActualizeCardList(); break;
                case 1: SettingsViewModel.UpdateParameters(); break;
            }
        }
    }

    private void SettingsParameterChanged(string parameterName)
    {
        switch (parameterName)
        {
            case nameof(Settings.AllCardsPath):
                _directoryWatcher.SetDirectory(nameof(Settings.AllCardsPath), Settings.AllCardsPath);
                break;
            case nameof(Settings.InsertedCardPath):
                _directoryWatcher.SetDirectory(nameof(Settings.InsertedCardPath), Settings.InsertedCardPath);
                break;
            case nameof(Settings.DetailedLogging):
                Logger.DetailedLogging = Settings.DetailedLogging;
                break;
        }
    }

    private void ApplySettingsOnStartup()
    {
        Logger.DetailedLogging = Settings.DetailedLogging;
        _directoryWatcher.SetDirectory(nameof(Settings.AllCardsPath), Settings.AllCardsPath);
        _directoryWatcher.SetDirectory(nameof(Settings.InsertedCardPath), Settings.InsertedCardPath);
    }
}