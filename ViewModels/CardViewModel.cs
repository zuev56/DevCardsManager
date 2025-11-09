using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DevCardsManager.ViewModels;

public sealed class CardViewModel : ViewModelBase
{
    private Card _card;
    private readonly CardManager _cardManager;
    private readonly Logger _logger;

    public CardViewModel(Card card, CardManager cardManager, Logger logger)
    {
        _card = card;
        _cardManager = cardManager;
        _logger = logger;

        Uid = card.Data.Uid.Any(b => b != 0)
            ? card.Data.UidString
            : "-";

        InsertCommand = new AsyncRelayCommand(InsertAsync);
        InsertOnTimeCommand = new AsyncRelayCommand(InsertOnTimeAsync);
        RemoveCommand = new RelayCommand(Remove, () => _card.IsInserted);
        PinCommand = new RelayCommand(Pin);
    }

    public string Path => _card.Path;
    public string CardName => System.IO.Path.GetFileNameWithoutExtension(_card.Path);
    public string Uid { get; }
    public int PinIndex => _card.PinIndex ?? -1;
    public bool IsPinned => PinIndex >= 0;
    public bool IsInserted => _card.IsInserted;
    [Reactive]
    public bool IsSelected { get; set; }

    public ICommand InsertCommand { get; }
    public ICommand InsertOnTimeCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PinCommand { get; }

    public void Refresh(string changedPropertyName)
    {
        _card = _cardManager.Cards.Single(c => c.Path == Path);
        _logger.LogTrace($"Card's '{CardName}' property '{changedPropertyName}' is changed to '{GetType().GetProperties().First(p => p.Name == changedPropertyName).GetValue(this)}'");

        Dispatcher.UIThread.Invoke(() =>
        {
            if (changedPropertyName == nameof(Card.PinIndex))
                this.RaisePropertyChanged(nameof(IsPinned));
            if (changedPropertyName == nameof(Card.IsInserted))
                ((RelayCommand)RemoveCommand).NotifyCanExecuteChanged();
        });
    }

    private Task InsertAsync() => _cardManager.InsertCardAsync(_card);
    private Task InsertOnTimeAsync() => _cardManager.InsertCardAsync(_card, removeOnTimeout: true);
    private void Remove() => _cardManager.RemoveCard(_card);
    private void Pin() => _cardManager.PinCard(_card);
}