using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;

namespace DevCardsManager.ViewModels;

public sealed class CardViewModel : ViewModelBase
{
    private readonly Card _card;
    private readonly CardManager _cardManager;

    public CardViewModel(Card card, CardManager cardManager)
    {
        _card = card;
        _cardManager = cardManager;

        InsertCommand = new AsyncRelayCommand(InsertAsync);
        InsertOnTimeCommand = new AsyncRelayCommand(InsertOnTimeAsync);
        RemoveCommand = new RelayCommand(Remove, () => _card.IsInserted);
        PinCommand = new RelayCommand(Pin);
    }

    public string Path => _card.Path;
    public string CardName => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string FileName => System.IO.Path.GetFileName(Path);
    public int PinIndex => _card.PinIndex ?? -1;
    public bool IsPinned => PinIndex >= 0;

    public bool IsInserted
    {
        get => _card.IsInserted;
        private set
        {
            // TODO: скорее всего, сеттер тут не актуален.
            if (value == _card.IsInserted)
                return;

            _card.IsInserted = value;
            this.RaisePropertyChanged();
        }
    }

    public ICommand InsertCommand { get; }
    public ICommand InsertOnTimeCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PinCommand { get; }

    public void Refresh(string changedPropertyName)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (changedPropertyName == nameof(PinIndex))
            {
                this.RaisePropertyChanged(nameof(IsPinned));
                this.RaisePropertyChanged(nameof(PinIndex));
            }
            if (changedPropertyName == nameof(IsInserted))
                ((RelayCommand)RemoveCommand).NotifyCanExecuteChanged();
        });
    }

    private Task InsertAsync() => _cardManager.InsertCardAsync(_card);
    private Task InsertOnTimeAsync() => _cardManager.InsertCardAsync(_card, removeOnTimeout: true);
    private void Remove() => _cardManager.RemoveCard(_card);
    private void Pin() => _cardManager.PinCard(_card);
}