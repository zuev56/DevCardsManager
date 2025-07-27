using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace DevCardsManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{

    private string _allCardsDirectoryPath;
    private string _insertedCardDirectoryPath;
    private ObservableCollection<CardViewModel> _cards;
    private string _log = string.Empty;
    private FileSystemWatcher _fileSystemWatcher = new();
    private string _filterText;

    public MainWindowViewModel()
    {
        try
        {
            Logger = new Logger();
            SettingsViewModel = new SettingsViewModel(Logger);
            SettingsViewModel.ActualizeTheme();

            AllCardsDirectoryPath = Settings.AllCardsPath;
            InsertedCardDirectoryPath = Settings.InsertedCardPath;

            InsertCardCommand = new RelayCommand<CardViewModel>(InsertCard);
            InsertCardOnTimeCommand = new RelayCommand<CardViewModel>(InsertCardOnTimeAsync);
            RemoveCardCommand = new RelayCommand<CardViewModel>(RemoveCard, CanRemoveCard);
            PinCardCommand = new RelayCommand<CardViewModel>(PinCard);
            UpdateCardsCommand = new RelayCommand(ActualizeCurrentState);
            ChangeSortOrderCommand = new RelayCommand(ChangeSortOrder);

            ActualizeCurrentState();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
        }
    }

    public Logger Logger { get; }
    public SettingsViewModel SettingsViewModel { get; }
    private Settings Settings => SettingsViewModel.Settings;
    public int InsertCardOnTimeSeconds => Settings.InsertCardOnTimeMs / 1000;
    public Func<string, Task> AddToClipboardAsync { get; set; }
    public Func<Task<string?>> ReadClipboardAsync { get; set; }

    public int SelectedTabIndex
    {
        set
        {
            switch (value)
            {
                case 0: SortCards(); break;
                case 1: SettingsViewModel.UpdateParameters(); break;
            }
        }
    }

    public string AllCardsDirectoryPath
    {
        get => _allCardsDirectoryPath;
        set
        {
            if (value == _allCardsDirectoryPath)
                return;

            _allCardsDirectoryPath = value.ToOsSpecificDirectorySeparatorChar();

            ActualizeCurrentState();

            OnPropertyChanged();
        }
    }

    public string InsertedCardDirectoryPath
    {
        get => _insertedCardDirectoryPath;
        set
        {
            if (value == _insertedCardDirectoryPath)
                return;

            if (Directory.Exists(value))
            {
                _fileSystemWatcher.Created -= FileSystemWatcher_Changed;
                _fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
                _fileSystemWatcher.Deleted -= FileSystemWatcher_Changed;
                _fileSystemWatcher.Error -= FileSystemWatcher_OnError;
                _fileSystemWatcher.Dispose();

                _insertedCardDirectoryPath = value.ToOsSpecificDirectorySeparatorChar();
                _fileSystemWatcher = new FileSystemWatcher(_insertedCardDirectoryPath, "*.bin");
                _fileSystemWatcher.EnableRaisingEvents = true;
                _fileSystemWatcher.Created += FileSystemWatcher_Changed;
                _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                _fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
                _fileSystemWatcher.Error += FileSystemWatcher_OnError;
            }

            OnPropertyChanged();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (value == _filterText)
                return;

            _filterText = value;
            ActualizeCurrentState();
        }
    }

    private void ActualizeCurrentState()
    {
        if (!Directory.Exists(_allCardsDirectoryPath))
            return;

        var cards = Directory.GetFiles(_allCardsDirectoryPath).Select(path => new CardViewModel { Path = path });
        if (!string.IsNullOrWhiteSpace(_filterText))
            cards = cards.Where(c => c.CardName.Contains(_filterText));

        _cards = new ObservableCollection<CardViewModel>(cards.OrderBy(card => card.CardName));

        Settings.PinnedCards.ForEach(pinnedCardName =>
        {
            var card = _cards.FirstOrDefault(c => c.CardName == pinnedCardName || c.FileName == pinnedCardName);
            if (card != null)
                card.PinIndex = _cards.Count(c => c.IsPinned);
        });

        SortCards();

        var insertedCardPath = GetInsertedCardPath();
        if (string.IsNullOrWhiteSpace(insertedCardPath))
            return;

        var copyInAllCardsPath = Path.Combine(AllCardsDirectoryPath, Path.GetFileName(insertedCardPath));
        if (!File.Exists(copyInAllCardsPath))
            File.Copy(insertedCardPath, copyInAllCardsPath, overwrite: false);

        Cards.Single(c => c.FileName == Path.GetFileName(insertedCardPath)).IsInserted = true;
    }

    private string? GetInsertedCardPath()
    {
        if (string.IsNullOrWhiteSpace(InsertedCardDirectoryPath))
            return null;

        var insertedCardDirFiles = Directory.GetFiles(InsertedCardDirectoryPath, "*.bin");
        if (insertedCardDirFiles.Length > 1)
        {
            Logger.LogInfo("В каталоге с приложенными картами лежит несколько карт!");
        };

        return insertedCardDirFiles.SingleOrDefault();
    }

    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs args)
    {
        Logger.LogException(args.GetException());
    }

    private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        //throw new NotImplementedException();
    }

    public ObservableCollection<CardViewModel> Cards
    {
        get => _cards;
        set
        {
            if (Equals(value, _cards))
                return;

            _cards = value;
            OnPropertyChanged();
        }
    }

    public ICommand InsertCardCommand { get; }
    public ICommand InsertCardOnTimeCommand { get; }
    public RelayCommand<CardViewModel> RemoveCardCommand { get; }
    public ICommand PinCardCommand { get; }
    public ICommand UpdateCardsCommand { get; }
    public ICommand ChangeSortOrderCommand { get; }


    private async void InsertCard(CardViewModel? card)
    {
        try
        {
            var currentlyInsertedCardPath = GetInsertedCardPath();
            if (currentlyInsertedCardPath != null)
            {
                var currentlyInsertedCard = _cards.Single(c => c.FileName == Path.GetFileName(currentlyInsertedCardPath));
                RemoveCard(currentlyInsertedCard);
                Thread.Sleep(Settings.ReplaceCardDelayMs);
            }

            Logger.LogInfo($"Inserting card: '{card!.CardName}'");

            var destinationPath = Path.Combine(InsertedCardDirectoryPath, card.FileName);

            File.Copy(card.Path, destinationPath);

            card.IsInserted = true;
            Logger.LogInfo($"Card '{card.CardName}' inserted!");

            await AddToClipboardAsync(card.CardName);

            var checkClipboardText = await ReadClipboardAsync();
            Logger.LogInfo($"Clipboard: '{checkClipboardText}'");

            RemoveCardCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
        }
    }

    private async void InsertCardOnTimeAsync(CardViewModel? card)
    {
        InsertCard(card);

        Logger.LogInfo($"Wait for {TimeSpan.FromMilliseconds(Settings.InsertCardOnTimeMs).TotalSeconds} seconds.");
        await Task.Delay(Settings.InsertCardOnTimeMs);

        if (card!.IsInserted)
            RemoveCard(card);
        else
            Logger.LogInfo($"Card '{card.CardName}' already removed.");
    }

    private void RemoveCard(CardViewModel? card)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(card);

            var insertedCardPath = GetInsertedCardPath();
            if (insertedCardPath != null)
            {
                Logger.LogInfo($"Removing card: '{Path.GetFileNameWithoutExtension(insertedCardPath)}'.");

                File.Move(insertedCardPath,
                    Path.Combine(AllCardsDirectoryPath, Path.GetFileName(insertedCardPath)),
                    overwrite: true);

                var insertedCard = Cards.SingleOrDefault(c => c.IsInserted);
                if (insertedCard != null)
                    insertedCard.IsInserted = false;
            }

            Logger.LogInfo($"Card '{Path.GetFileNameWithoutExtension(insertedCardPath)}' successfully removed.");
            RemoveCardCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
        }
    }

    private static bool CanRemoveCard(CardViewModel? card) => card?.IsInserted == true;

    private void PinCard(CardViewModel? card)
    {
        if (card == null)
            return;

        if (card.IsPinned)
        {
            card.UnPin();
            Settings.PinnedCards.Remove(card.CardName);
        }
        else
        {
            var lastPinIndex = _cards.OrderBy(c => c.PinIndex).Last().PinIndex;
            card.PinIndex = lastPinIndex + 1;
            Settings.PinnedCards.Add(card.CardName);
        }

        Settings.PinnedCards.RemoveAll(cardName => Cards.All(c => c.CardName != cardName));
        SettingsViewModel.SaveSettings();

        SortCards();
    }

    private void ChangeSortOrder()
    {
        Settings.SortAscending = !Settings.SortAscending;
        SettingsViewModel.SaveSettings();
        SortCards();
    }

    private void SortCards()
    {
        var pinnedCards = _cards.Where(c => c.IsPinned).ToList();
        var unpinnedCards = _cards.Except(pinnedCards);

        unpinnedCards = Settings.SortAscending
            ? unpinnedCards.OrderBy(c => c.CardName)
            : unpinnedCards.OrderByDescending(c => c.CardName);

        Cards = new ObservableCollection<CardViewModel>(pinnedCards.Union(unpinnedCards));
    }
}