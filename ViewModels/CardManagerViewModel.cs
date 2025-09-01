using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;

namespace DevCardsManager.ViewModels;

public sealed class CardManagerViewModel : ViewModelBase
{
    private ObservableCollection<CardViewModel> _filteredCards;
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

            InsertCardCommand = new AsyncRelayCommand<CardViewModel>(InsertCardAsync);
            InsertCardOnTimeCommand = new AsyncRelayCommand<CardViewModel>(InsertCardOnTimeAsync);
            RemoveCardCommand = new RelayCommand<CardViewModel>(RemoveCard, cardVm => cardVm?.IsInserted == true);
            PinCardCommand = new RelayCommand<CardViewModel>(PinCard);
            UpdateCardsCommand = ReactiveCommand.Create(_cardManager.ActualizeCardList);
            ChangeSortOrderCommand = ReactiveCommand.Create(ChangeSortOrder);
            ClearFilterCommand = ReactiveCommand.Create(() => FilterText = null);

            ActualizeCardList();
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

    public ObservableCollection<CardViewModel> FilteredCards
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

    public ICommand InsertCardCommand { get; }
    public ICommand InsertCardOnTimeCommand { get; }
    public ICommand RemoveCardCommand { get; }
    public ICommand PinCardCommand { get; }
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

            var allCards = _cardManager.Cards.Select(Mapper.ToCardViewModel);

            var filteredCards = string.IsNullOrWhiteSpace(_filterText)
                ? allCards
                : allCards.Where(c => c.CardName.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase));

            _filteredCards = new ObservableCollection<CardViewModel>(filteredCards);

            Settings.PinnedCards.ForEach(pinnedCardName =>
            {
                var card = _filteredCards.FirstOrDefault(c => c.CardName == pinnedCardName || c.FileName == pinnedCardName);
                card?.Pin(_filteredCards.Count(c => c.IsPinned));
            });

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

    private void ChangeSortOrder()
    {
        Settings.SortAscending = !Settings.SortAscending;
        _settingsManager.SaveSettings();
        SortCards();
    }

    private async Task InsertCardAsync(CardViewModel? cardVm)
    {
        await _cardManager.InsertCardAsync(cardVm!.ToCard());
        ActualizeCardViewModel(cardVm!);
    }

    private async Task InsertCardOnTimeAsync(CardViewModel? cardVm)
    {
        await _cardManager.InsertCardAsync(cardVm!.ToCard(), removeOnTimeout: true);
        ActualizeCardViewModel(cardVm!);
    }

    private void RemoveCard(CardViewModel? cardVm)
    {
        _cardManager.RemoveCard(cardVm!.ToCard());
        ActualizeCardViewModel(cardVm!);
    }

    private void PinCard(CardViewModel? cardVm)
    {
        _cardManager.PinCard(cardVm!.ToCard());
        ActualizeCardViewModel(cardVm!);
    }

    private void ActualizeCardViewModel(CardViewModel cardVm)
    {
        ActualizeCardList();

        // TODO: ActualizeCardList() слишком тяжеловесный в этом случае, но пока иначе не справиться
        // var card = _cardManager.Cards.Single(c => c.Path == cardVm.Path);
        // var cardIndex = _filteredCards.IndexOf(cardVm);
        // Dispatcher.UIThread.Post(() =>
        // {
        //     _filteredCards.RemoveAt(cardIndex);
        //     _filteredCards.Insert(cardIndex, card.ToCardViewModel());
        //     this.RaisePropertyChanged(nameof(FilteredCards));
        //     ((RelayCommand<CardViewModel>) RemoveCardCommand).NotifyCanExecuteChanged();
        // });
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
            var pinnedCards = _filteredCards.Where(c => c.IsPinned).ToList();
            var unpinnedCards = _filteredCards.Except(pinnedCards);

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