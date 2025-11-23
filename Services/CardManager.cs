using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevCardsManager.Extensions;
using DevCardsManager.Models;

namespace DevCardsManager.Services;

public sealed class CardManager
{
    private int _insertionInProgress;
    private int _removeCardOnTimeoutTaskId = -1;

    private readonly DirectoryWatcher _directoryWatcher;
    private readonly SettingsManager _settingsManager;
    private readonly Logger _logger;

    public CardManager(DirectoryWatcher directoryWatcher, SettingsManager settingsManager, Logger logger)
    {
        _directoryWatcher = directoryWatcher;
        _settingsManager = settingsManager;
        _logger = logger;

        _directoryWatcher.DirectoryChanged += OnDirectoryChanged;

        ActualizeCardList();
    }

    public List<Card> Cards { get; } = [];
    private Settings Settings => _settingsManager.Settings;
    public Func<string, Task> AddToClipboardAsync { get; set; } = null!;
    public Func<Task<string?>> ReadClipboardAsync { get; set; } = null!;

    public delegate void CardStateUpdatedEvent(Card card, string updatedProperty);
    public event CardStateUpdatedEvent? CardStateUpdated;

    public void ActualizeCardList()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!Directory.Exists(Settings.AllCardsPath))
                return;

            var actualCards = Directory.GetFiles(Settings.AllCardsPath)
                .Select(path =>
                {
                    var pinIndex = Settings.PinnedCards.IndexOf(Path.GetFileNameWithoutExtension(path));
                    return new Card {Path = path, PinIndex = pinIndex == -1 ? null : pinIndex };
                })
                .ToList();

            Cards.RemoveAll(c => actualCards.All(ac => ac.Path != c.Path));
            Cards.AddRange(actualCards.Where(ac => Cards.All(c => c.Path != ac.Path)));

            var cardPathToDumpMap = Cards
                .Select(c => c.Path).AsParallel()
                .ToDictionary(path => path, path => ReadCardDump(path));
            Cards.ForEach(c => c.Dump = cardPathToDumpMap[c.Path]!);

            var insertedCardPath = GetInsertedCardPath();
            if (string.IsNullOrWhiteSpace(insertedCardPath))
            {
                foreach (var card in Cards.Where(c => c.IsInserted))
                {
                    card.IsInserted = false;
                    CardStateUpdated?.Invoke(card, nameof(Card.IsInserted));
                }
                return;
            }

            CopyInsertedCardToAllCardsDirIfNotExists();

            var insertedCard = Cards.Single(c => GetFileName(c.Path) == GetFileName(insertedCardPath));
            insertedCard.IsInserted = true;

            CardStateUpdated?.Invoke(insertedCard, nameof(Card.IsInserted));
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

    private void CopyInsertedCardToAllCardsDirIfNotExists()
    {
        var insertedCardPath = GetInsertedCardPath();
        if (insertedCardPath == null)
            return;

        var copyInAllCardsPath = Path.Combine(Settings.AllCardsPath, Path.GetFileName(insertedCardPath));
        if (!File.Exists(copyInAllCardsPath))
            File.Copy(insertedCardPath, copyInAllCardsPath, overwrite: false);
    }

    private string? GetInsertedCardPath()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(Settings.InsertedCardPath) || !Path.Exists(Settings.InsertedCardPath))
                return null;

            var insertedCardDirFiles = Directory.GetFiles(Settings.InsertedCardPath, "*.bin");
            if (insertedCardDirFiles.Length > 1)
                _logger.LogInfo("В каталоге с приложенными картами лежит несколько карт!");

            return insertedCardDirFiles.SingleOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            throw;
        }
        finally
        {
            _logger.LogPerformance(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Вставить карту
    /// </summary>
    public Task InsertCardAsync(Card card, bool removeOnTimeout = false)
    {
        if (!SetInsertionInProgress())
        {
            _logger.LogWarning("Inserting other card in a process!");
            return Task.CompletedTask;
        }

        return Task.Factory.StartNew(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Interlocked.Exchange(ref _insertionInProgress, 1);

                    _directoryWatcher.DirectoryChanged -= OnDirectoryChanged;

                    var currentlyInsertedCardPath = GetInsertedCardPath();
                    if (currentlyInsertedCardPath != null)
                    {
                        var currentlyInsertedCard =
                            Cards.Single(c => GetFileName(c.Path) == GetFileName(currentlyInsertedCardPath));
                        RemoveCard(currentlyInsertedCard, removeDuringInserting: true);

                        stopwatch.Stop();
                        await Task.Delay(Settings.ReplaceCardDelayMs);
                        stopwatch.Start();
                    }

                    var cardName = GetCardName(card);
                    _logger.LogInfo($"Inserting card: '{cardName}'");

                    if (!Settings.InsertedCardPath.IsValidDirectoryPath())
                    {
                        _logger.LogInfo($"Invalid inserted card directory path: '{Settings.InsertedCardPath}'");
                        return;
                    }

                    if (!Path.Exists(Settings.InsertedCardPath))
                    {
                        _logger.LogInfo($"Trying to create inserted card directory: '{Settings.InsertedCardPath}'");
                        Directory.CreateDirectory(Settings.InsertedCardPath);
                    }

                    var destinationPath = Path.Combine(Settings.InsertedCardPath, Path.GetFileName(card.Path));

                    File.Copy(card.Path, destinationPath);

                    card.IsInserted = true;
                    _logger.LogInfo($"Card '{cardName}' inserted!");

                    CardStateUpdated?.Invoke(card, nameof(Card.IsInserted));

                    AddToClipboardAsync(cardName)
                        .ContinueWith(_ => ReadClipboardAsync()
                            .ContinueWith(readClipboardTask
                                =>_logger.LogInfo($"Clipboard: '{readClipboardTask.Result}'")));
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
                finally
                {
                    SetInsertionFinished();
                    _directoryWatcher.DirectoryChanged += OnDirectoryChanged;
                    _logger.LogPerformance(stopwatch.Elapsed);
                }
            })
            .ContinueWith(insertResult =>
            {
                if (!insertResult.IsCompletedSuccessfully || !removeOnTimeout)
                    return;

                _logger.LogInfo($"Wait for {TimeSpan.FromMilliseconds(Settings.InsertCardOnTimeMs).TotalSeconds} seconds.");

                var removeCardTask = new Task(() =>
                {
                    if (card!.IsInserted)
                        RemoveCard(card);
                    else
                        _logger.LogInfo($"Card '{GetCardName(card)}' already removed.");
                });

                _removeCardOnTimeoutTaskId = removeCardTask.Id;

                Task.Delay(Settings.InsertCardOnTimeMs)
                    .ContinueWith(_ =>
                    {
                        if (removeCardTask.Id != _removeCardOnTimeoutTaskId)
                        {
                            _logger.LogInfo($"Card '{GetCardName(card)}' removing on timeout is cancelled.");
                            return;
                        }

                        removeCardTask.Start();
                    });
            });
    }

    /// <summary>
    /// Урать карту - удалить или переместить в каталог со всеми картами.
    /// </summary>
    public void RemoveCard(Card card, bool removeDuringInserting = false)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            ArgumentNullException.ThrowIfNull(card);

            var insertedCardPath = GetInsertedCardPath();
            if (insertedCardPath != null)
            {
                _directoryWatcher.DirectoryChanged -= OnDirectoryChanged;

                if (Settings.SaveCardChangesOnReturn)
                {
                    _logger.LogInfo($"Removing card: '{GetFileName(insertedCardPath)}' (move back to the all cards directory).");
                    File.Move(insertedCardPath,
                        Path.Combine(Settings.AllCardsPath, Path.GetFileName(insertedCardPath)),
                        overwrite: true);
                }
                else
                {
                    _logger.LogInfo($"Removing card: '{GetFileName(insertedCardPath)}'. (delete the file)");
                    File.Delete(insertedCardPath);
                }

                var insertedCard = Cards.SingleOrDefault(c => c.IsInserted);
                if (insertedCard != null)
                    insertedCard.IsInserted = false;
            }

            CardStateUpdated?.Invoke(card, nameof(Card.IsInserted));

            _logger.LogInfo($"Card '{GetFileName(insertedCardPath)}' successfully removed.");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
        finally
        {
            _removeCardOnTimeoutTaskId = -1;
            if (!removeDuringInserting)
                _directoryWatcher.DirectoryChanged += OnDirectoryChanged;
            _logger.LogPerformance(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Закрепить карту вверху списка.
    /// </summary>
    public void PinCard(Card card)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (card.PinIndex.HasValue)
            {
                card.UnPin();
                Settings.PinnedCards.Remove(GetCardName(card)!);
            }
            else
            {
                var lastPinIndex = Cards.OrderBy(c => c.PinIndex).Last().PinIndex ?? -1;
                card.PinIndex = lastPinIndex + 1;
                Settings.PinnedCards.Add(GetCardName(card)!);
            }

            Settings.PinnedCards.RemoveAll(cardName => Cards.All(c => Path.GetFileNameWithoutExtension(c.Path) != cardName));
            _settingsManager.SaveSettings();

            CardStateUpdated?.Invoke(card, nameof(Card.PinIndex));
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

    // Метод должен выполняться только если каталог был изменён не этой программой
    private void OnDirectoryChanged(string? path)
    {
        if (path.PathEquals(Settings.InsertedCardPath))
            CopyInsertedCardToAllCardsDirIfNotExists();

        ActualizeCardList();

        _logger.LogTrace($"Directory changed: {path}");
    }

    private bool SetInsertionInProgress() => Interlocked.CompareExchange(ref _insertionInProgress, 1, 0) == 0;

    private void SetInsertionFinished() => Interlocked.Exchange(ref _insertionInProgress, 0);

    private string? GetFileName(string? path) => Path.GetFileNameWithoutExtension(path);

    private string? GetCardName(Card? card) => GetFileName(card?.Path);

    private static CardDump? ReadCardDump(string cardImagePath, int attempt = 3)
    {
        try
        {
            var dumpBytes = File.ReadAllBytes(cardImagePath);
            using var fileStream = File.Open(cardImagePath, FileMode.Open);
            using var reader = new BinaryReader(fileStream);

            var cardModel = dumpBytes[0] switch
            {
                0 => CardModel.MifareUltralightC,
                1 => CardModel.MifareClassic1K,
                2 => CardModel.MifareClassic4K,
                3 => CardModel.MifarePlus2K,
                4 => CardModel.MifareUltralightEv1,
                _ => CardModel.Unknown
            };
            var uidLength = dumpBytes[1];
            var uid = dumpBytes[2..(2 + uidLength)];

            return new CardDump(dumpBytes, cardModel, uid);
        }
        catch
        {
            if (--attempt > 0)
                // TODO: Delay
                return ReadCardDump(cardImagePath, attempt);

            return null;
        }
    }
}