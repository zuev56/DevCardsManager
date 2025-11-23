using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DevCardsManager.Extensions;
using DevCardsManager.Models;
using DevCardsManager.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DevCardsManager.Ui.Controls;

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
        UpdateCardInfo();

        InsertCommand = new AsyncRelayCommand(InsertAsync);
        InsertOnTimeCommand = new AsyncRelayCommand(InsertOnTimeAsync);
        RemoveCommand = new RelayCommand(Remove, () => _card.IsInserted);
        PinCommand = new RelayCommand(Pin);
    }

    public string Path => _card.Path;
    public string CardName => System.IO.Path.GetFileNameWithoutExtension(_card.Path);
    public int PinIndex => _card.PinIndex ?? -1;
    public bool IsPinned => PinIndex >= 0;
    public bool IsInserted => _card.IsInserted;
    [Reactive]
    public bool IsSelected { get; set; }

    public string UidString { get; private set; }
    public string ChipSerialNumber { get; private set; }
    public string CardModel { get; private set; }

    public ICommand InsertCommand { get; }
    public ICommand InsertOnTimeCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PinCommand { get; }

    private void UpdateCardInfo()
    {
        (UidString, ChipSerialNumber) = GetUidAndChipSerialNumber(_card.Dump.Uid);
        CardModel = _card.Dump.Model.GetDescription();
    }

    public void Refresh(string changedPropertyName)
    {
        _card = _cardManager.Cards.Single(c => c.Path == Path);
        _logger.LogTrace($"Card's '{CardName}' property '{changedPropertyName}' is changed to '{GetType().GetProperties().First(p => p.Name == changedPropertyName).GetValue(this)}'");

        UpdateCardInfo();

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

    private (string Uid, string ChipSerialNumber) GetUidAndChipSerialNumber(byte[] uid)
    {
        var (uidString, csnString) = ("-", "-");
        try
        {
            var uid8Bytes = new byte[8];
            Array.Copy(uid, uid8Bytes, uid.Length);
            uidString = uid.Any(b => b != 0)
                ? BitConverter.ToUInt64(uid8Bytes, 0).ToString()
                : "-";

            var base64Uid = uid.Length is 7 or 4
                ? string.Join(string.Empty, uid.Select(x => x.ToString("X2", CultureInfo.InvariantCulture)))
                : throw new ArgumentException($"UID must be 4 or 7 bytes length. Current: {uid}");

            if (base64Uid.Length == 14)
                csnString = long.Parse(base64Uid, NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString();
            else if (base64Uid.Length == 8)
            {
                var base64UidArr = new Regex("(?<=\\G.{2})").Split(base64Uid);
                csnString = long.Parse($"{base64UidArr[3]}{base64UidArr[2]}{base64UidArr[1]}{base64UidArr[0]}",
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString();
            }
        }
        catch (Exception e)
        {
            _logger.LogException(e);
        }

        return (uidString, csnString);
    }
}