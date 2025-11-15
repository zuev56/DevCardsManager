using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DevCardsManager.ViewModels;

public sealed class CardManagerViewModel : ViewModelBase
{
    private List<CardViewModel> _filteredCards = [];
    private string? _filterText;
    private bool _keepOnTop;
    private bool _saveCardChangesOnReturn;
    private CardViewModel? _selectedCard;

    private readonly CardManager _cardManager;
    private readonly SettingsManager _settingsManager;
    private readonly Logger _logger;

    public CardManagerViewModel(CardManager cardManager, SettingsManager settingsManager, Logger logger)
    {
        _cardManager = cardManager;
        _settingsManager = settingsManager;
        _logger = logger;

        _keepOnTop = Settings.KeepOnTop;
        _saveCardChangesOnReturn = Settings.SaveCardChangesOnReturn;

        _cardManager.CardStateUpdated += OnCardStateUpdated;
        _settingsManager.ParameterChanged += OnSettingsParameterChanged;

        UpdateCardsCommand = ReactiveCommand.Create(ActualizeCardList);
        ChangeSortOrderCommand = ReactiveCommand.Create(ChangeSortOrder);
        ClearFilterCommand = ReactiveCommand.Create(() => FilterText = null);

        ActualizeCardList();
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

    public bool KeepOnTop
    {
        get => _keepOnTop;
        set
        {
            if (value == _keepOnTop)
                return;

            _keepOnTop = value;
            if (Settings.KeepOnTop != _keepOnTop)
            {
                Settings.KeepOnTop = _keepOnTop;
                _settingsManager.SaveSettings();
            }

            this.RaisePropertyChanged();
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

    [Reactive]
    public ObservableCollection<CardViewModel> FilteredCards { get; init; } = [];

    public CardViewModel? SelectedCard
    {
        get => _selectedCard;
        set
        {
            if (_selectedCard != value)
            {
                if (_selectedCard != null)
                    _selectedCard.IsSelected = false;

                _selectedCard = value;

                if (_selectedCard != null)
                    _selectedCard.IsSelected = true;

                _logger.LogInfo($"SelectedCard was changed: {_selectedCard?.CardName}");
                this.RaisePropertyChanged();
            }
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
                _filteredCards.Clear();
                return;
            }

            _cardManager.ActualizeCardList();

            var allCards = _cardManager.Cards.Select(card => new CardViewModel(card, _cardManager, _logger)).ToList();

            // На случай, когда вставленную карту заменили в обход этой программы
            if (allCards.Count(c => c.IsInserted) > 1)
                foreach (var cardVm in allCards.Where(c => c.IsInserted))
                    OnCardStateUpdated(_cardManager.Cards.Single(c => c.Path == cardVm.Path), nameof(Card.IsInserted));

            _filteredCards = string.IsNullOrWhiteSpace(_filterText)
                ? allCards
                : allCards.Where(c => c.CardName.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)
                                   || c.Uid.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();

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
        FilteredCards.SingleOrDefault(c => c.CardName == Path.GetFileNameWithoutExtension(card.Path))
            ?.Refresh(changedPropertyName);

        if (changedPropertyName == nameof(Card.PinIndex))
            SortCards();
    }

    private void ChangeSortOrder()
    {
        Settings.SortAscending = !Settings.SortAscending;
        _settingsManager.SaveSettings();
        SortCards();
    }

    private void OnSettingsParameterChanged(string parameterName)
    {
        switch (parameterName)
        {
            case nameof(Settings.AllCardsPath):
                ActualizeCardList();
                break;
            case nameof(Settings.KeepOnTop):
                KeepOnTop = Settings.KeepOnTop;
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
            var pinnedCards = _filteredCards.Where(c => c.IsPinned).OrderBy(c => c.PinIndex).ToList();
            var unpinnedCards = _filteredCards.Except(pinnedCards);

            unpinnedCards = Settings.SortAscending
                ? unpinnedCards.OrderBy(c => c.CardName)
                : unpinnedCards.OrderByDescending(c => c.CardName);

            var actualizedFilteredCards = pinnedCards.Union(unpinnedCards);

            FilteredCards.Clear();
            FilteredCards.AddRange(actualizedFilteredCards);
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