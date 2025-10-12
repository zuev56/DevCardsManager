using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;

namespace DevCardsManager.ViewModels;

public sealed class CardManagerViewModel : ViewModelBase
{
    private ObservableCollection<CardViewModel>? _filteredCards;
    private string? _filterText;
    private bool _saveCardChangesOnReturn;

    private readonly CardManager _cardManager;
    private readonly SettingsManager _settingsManager;
    private readonly Logger _logger;

    public CardManagerViewModel(CardManager cardManager, SettingsManager settingsManager, Logger logger)
    {
        try
        {
            _cardManager = cardManager;
            _settingsManager = settingsManager;
            _logger = logger;

            UpdateCardsCommand = ReactiveCommand.Create(_cardManager.ActualizeCardList);
            ChangeSortOrderCommand = ReactiveCommand.Create(ChangeSortOrder);
            ClearFilterCommand = ReactiveCommand.Create(() => FilterText = null);

            ActualizeCardList();

            _cardManager.CardStateUpdated += OnCardStateUpdated;
            _settingsManager.ParameterChanged += SettingsParameterChanged;

            _saveCardChangesOnReturn = Settings.SaveCardChangesOnReturn;
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }

    private Settings Settings => _settingsManager.Settings;
    public int InsertCardOnTimeSeconds => Settings.InsertCardOnTimeMs / 1000;

    public string? FilterText
    {
        get => _filterText;
        set
        {
            if (value == _filterText)
                return;

            _filterText = value;
            this.RaisePropertyChanged();

            ActualizeCardList();
        }
    }

    public bool SaveCardChangesOnReturn
    {
        get => _saveCardChangesOnReturn;
        set
        {
            if (value == _saveCardChangesOnReturn)
                return;

            _saveCardChangesOnReturn = value;
            if (Settings.SaveCardChangesOnReturn != _saveCardChangesOnReturn)
            {
                Settings.SaveCardChangesOnReturn = _saveCardChangesOnReturn;
                _settingsManager.SaveSettings();
            }

            _logger.LogInfo($"Cards will {(_saveCardChangesOnReturn ? "save" : "lose")} changes when return");

            this.RaisePropertyChanged();
        }
    }

    public ObservableCollection<CardViewModel>? FilteredCards
    {
        get => _filteredCards;
        private set
        {
            if (Equals(value, _filteredCards))
                return;

            _filteredCards = value;
            this.RaisePropertyChanged();
        }
    }

    public ICommand UpdateCardsCommand { get; }
    public ICommand ChangeSortOrderCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public void ActualizeCardList()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!Directory.Exists(Settings.AllCardsPath))
            {
                FilteredCards?.Clear();
                return;
            }

            _cardManager.ActualizeCardList();

            var allCards = _cardManager.Cards.Select(card => new CardViewModel(card, _cardManager));

            var filteredCards = string.IsNullOrWhiteSpace(_filterText)
                ? allCards
                : allCards.Where(c => c.CardName.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase));

            _filteredCards = new ObservableCollection<CardViewModel>(filteredCards);

            SortCards();
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

    private void OnCardStateUpdated(Card card, string changedPropertyName)
    {
        FilteredCards!.FirstOrDefault(c => c.Path == card.Path)?.Refresh(changedPropertyName);

        if (changedPropertyName == nameof(Card.PinIndex))
            ActualizeCardList();
    }

    private void ChangeSortOrder()
    {
        Settings.SortAscending = !Settings.SortAscending;
        _settingsManager.SaveSettings();
        SortCards();
    }

    private void SettingsParameterChanged(string parameterName)
    {
        switch (parameterName)
        {
            case nameof(Settings.AllCardsPath):
                ActualizeCardList();
                break;
            case nameof(Settings.SaveCardChangesOnReturn):
                SaveCardChangesOnReturn = Settings.SaveCardChangesOnReturn;
                break;
        }
    }

    private void SortCards()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var pinnedCards = _filteredCards!.Where(c => c.IsPinned).ToList();
            var unpinnedCards = _filteredCards!.Except(pinnedCards);

            unpinnedCards = Settings.SortAscending
                ? unpinnedCards.OrderBy(c => c.CardName)
                : unpinnedCards.OrderByDescending(c => c.CardName);

            FilteredCards = new ObservableCollection<CardViewModel>(pinnedCards.Union(unpinnedCards));
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
}