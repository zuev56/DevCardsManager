using System;
using DevCardsManager.Models;
using DevCardsManager.Services;

namespace DevCardsManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DirectoryWatcher _directoryWatcher;
    private readonly SettingsManager _settingsManager;

    public MainWindowViewModel()
    {
        try
        {
            Logger = new Logger();
            _settingsManager = new SettingsManager(Logger);
            CardManager = new CardManager(_settingsManager, Logger);
            SettingsViewModel = new SettingsViewModel(_settingsManager, Logger);
            CardManagerViewModel = new CardManagerViewModel(CardManager, _settingsManager, Logger);
            _directoryWatcher = new DirectoryWatcher(Logger);

            _directoryWatcher.DirectoryChanged += path =>
            {
                if (path == Settings.AllCardsPath)
                    CardManagerViewModel.ActualizeCardList();
                else if (path == Settings.InsertedCardPath)
                    CardManager.CopyInsertedCardToAllCardsDirIfNotExists();
            };
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