using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using static System.Environment;

namespace DevCardsManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string _appsettingsFileName = "appsettings.json";

    private string _allCardsDirectoryPath;
    private string _insertedCardDirectoryPath;
    private ObservableCollection<CardViewModel> _cards;
    private string _log = string.Empty;
    private FileSystemWatcher _fileSystemWatcher = new();
    private bool _sortAscending = true;
    private string _filterText;

    public MainWindowViewModel()
    {
        try
        {
            Settings = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_appsettingsFileName, optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build()
                .Get<Settings>()!;

            AllCardsDirectoryPath = Settings.AllCardsPath;
            InsertedCardDirectoryPath = Settings.InsertedCardPath;

            InsertCardCommand = new RelayCommand<CardViewModel>(InsertCard);
            InsertCardOnTimeCommand = new RelayCommand<CardViewModel>(InsertCardOnTimeAsync);
            RemoveCardCommand = new RelayCommand<CardViewModel>(RemoveCard, CanRemoveCard);
            PinCardCommand = new RelayCommand<CardViewModel>(PinCard);
            ReloadSettingsFileContentCommand = new RelayCommand(LoadSettingsFileContent);
            SaveSettingsFileContentCommand = new RelayCommand(SaveSettingsFileContent);
            UpdateCardsCommand = new RelayCommand(ActualizeCurrentState);
            SortCardsCommand = new RelayCommand(() => SortCards());

            ActualizeCurrentState();
        }
        catch (Exception e)
        {
            LogException(e);
        }
    }

    private Settings Settings { get; set; }
    public int InsertCardOnTimeSeconds => Settings.InsertCardOnTimeMs / 1000;
    public Func<string, Task> AddToClipboardAsync { get; set; }
    public Func<Task<string?>> ReadClipboardAsync { get; set; }

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

        SortCards(Settings.SortAscending);

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

        var insertedCardDirFiles = Directory.GetFiles(InsertedCardDirectoryPath);
        if (insertedCardDirFiles.Length > 1)
            throw new InvalidOperationException("В каталоге с приложенными картами несколько файлов!");

        return insertedCardDirFiles.SingleOrDefault();
    }

    private void FileSystemWatcher_OnError(object sender, ErrorEventArgs args)
    {
        LogException(args.GetException());
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

    public string SettingsFileContent { get; set; }

    public string Log
    {
        get => _log;
        set
        {
            if (value == _log)
                return;

            _log = value;
            OnPropertyChanged();
        }
    }

    public ICommand InsertCardCommand { get; }
    public ICommand InsertCardOnTimeCommand { get; }
    public RelayCommand<CardViewModel> RemoveCardCommand { get; }
    public ICommand PinCardCommand { get; }
    public ICommand UpdateCardsCommand { get; }
    public ICommand SortCardsCommand { get; }
    public ICommand ReloadSettingsFileContentCommand { get; private set; }
    public ICommand SaveSettingsFileContentCommand { get; private set; }


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

            LogMessage($"Inserting card: '{card!.CardName}'");

            var destinationPath = Path.Combine(InsertedCardDirectoryPath, card.FileName);

            File.Copy(card.Path, destinationPath);

            card.IsInserted = true;
            LogMessage($"Card '{card.CardName}' inserted!");

            await AddToClipboardAsync(card.CardName);

            var checkClipboardText = await ReadClipboardAsync();
            LogMessage($"Clipboard: '{checkClipboardText}'");

            RemoveCardCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            LogException(e);
        }
    }

    private async void InsertCardOnTimeAsync(CardViewModel? card)
    {
        InsertCard(card);

        LogMessage($"Wait for {TimeSpan.FromMilliseconds(Settings.InsertCardOnTimeMs).TotalSeconds} seconds.");
        await Task.Delay(Settings.InsertCardOnTimeMs);

        if (card!.IsInserted)
            RemoveCard(card);
        else
            LogMessage($"Card '{card.CardName}' already removed.");
    }

    private void RemoveCard(CardViewModel? card)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(card);

            var insertedCardPath = GetInsertedCardPath();
            if (insertedCardPath != null)
            {
                LogMessage($"Removing card: '{Path.GetFileNameWithoutExtension(insertedCardPath)}'.");

                File.Move(insertedCardPath,
                    Path.Combine(AllCardsDirectoryPath, Path.GetFileName(insertedCardPath)),
                    overwrite: true);

                var insertedCard = Cards.SingleOrDefault(c => c.IsInserted);
                if (insertedCard != null)
                    insertedCard.IsInserted = false;
            }

            LogMessage($"Card '{Path.GetFileNameWithoutExtension(insertedCardPath)}' successfully removed.");
            RemoveCardCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            LogException(e);
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
        SaveSettings();

        SortCards(_sortAscending);
    }

    private void SaveSettings()
    {
        try
        {
            var settingsContent = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), _appsettingsFileName), settingsContent);
        }
        catch (Exception e)
        {
            LogException(e);
        }
    }

    private void LoadSettingsFileContent()
    {
        throw new NotImplementedException();
    }


    private void SaveSettingsFileContent()
    {
        throw new NotImplementedException();
    }

    private void SortCards(bool? ascending = null)
    {
        _sortAscending = ascending ?? !_sortAscending;

        var pinnedCards = _cards.Where(c => c.IsPinned).ToList();
        var unpinnedCards = _cards.Except(pinnedCards);

        unpinnedCards = _sortAscending
            ? unpinnedCards.OrderBy(c => c.CardName)
            : unpinnedCards.OrderByDescending(c => c.CardName);

        Cards = new ObservableCollection<CardViewModel>(pinnedCards.Union(unpinnedCards));
    }

    private void LogMessage(string message)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        Console.WriteLine(text);
        Log += $"{NewLine}{text}";
    }

    private void LogException(Exception exception)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff]}] {exception.GetType().Name}: {exception.Message}{NewLine}{exception.StackTrace}";
        Console.WriteLine(text);
        Log += $"{NewLine}{text}";
    }
}