using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevCardsManager.Extensions;
using DevCardsManager.Models;
using DevCardsManager.ViewModels;

namespace DevCardsManager.Services;

public sealed class CardManager
{
    private int _insertionInProgress;
    private int _removeCardOnTimeoutTaskId = -1;

    private readonly SettingsManager _settingsManager;
    private readonly Logger _logger;

    public CardManager(SettingsManager settingsManager, Logger logger)
    {
        _settingsManager = settingsManager;
        _logger = logger;

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

            var insertedCardPath = GetInsertedCardPath();
            if (string.IsNullOrWhiteSpace(insertedCardPath))
                return;

            CopyInsertedCardToAllCardsDirIfNotExists();

            var insertedCard = Cards.Single(c => GetFileName(c.Path) == GetFileName(insertedCardPath));
            insertedCard.IsInserted = true;

            var cardPathToDataMap = Cards
                .Select(c => c.Path).AsParallel()
                .ToDictionary(path => path, ReadCardData);
            Cards.ForEach(c => c.Data = cardPathToDataMap[c.Path]!);

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

    public void CopyInsertedCardToAllCardsDirIfNotExists()
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
                    var currentlyInsertedCardPath = GetInsertedCardPath();
                    if (currentlyInsertedCardPath != null)
                    {
                        var currentlyInsertedCard =
                            Cards.Single(c => GetFileName(c.Path) == GetFileName(currentlyInsertedCardPath));
                        RemoveCard(currentlyInsertedCard);

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
    public void RemoveCard(Card card)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            ArgumentNullException.ThrowIfNull(card);

            var insertedCardPath = GetInsertedCardPath();
            if (insertedCardPath != null)
            {
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

    private bool SetInsertionInProgress() => Interlocked.CompareExchange(ref _insertionInProgress, 1, 0) == 0;

    private void SetInsertionFinished() => Interlocked.Exchange(ref _insertionInProgress, 0);

    private string? GetFileName(string? path) => Path.GetFileNameWithoutExtension(path);

    private string? GetCardName(Card? card) => GetFileName(card?.Path);


    public static void AnalyzeCardDump(byte[] data)
    {
        // TODO: доразобраться
        // var info = new CardInfo
        // {
        //     FileSize = data.Length,
        //     IsValid = true
        // };
        //
        // // Определяем тип карты по размеру
        // info.Type = data.Length switch
        // {
        //     1024 => CardType.MifareClassic1K,
        //     4096 => CardType.MifareClassic4K,
        //     64 => CardType.MifareUltralight,
        //     _ => CardType.Unknown
        // };

        // Извлекаем UID в зависимости от типа карты
        // info.Uid = info.Type switch
        // {
        //     CardType.MifareClassic1K or CardType.MifareClassic4K => GetMifareClassicUid(data),
        //     CardType.MifareUltralight => GetMifareUltralightUid(data),
        //     _ => GetGenericUid(data)
        // };

        //info.UidString = BitConverter.ToString(info.Uid).Replace("-", "");

        // return info;
    }

    private static CardData? ReadCardData(string cardImagePath)
    {
        try
        {
            var imageBytes = File.ReadAllBytes(cardImagePath);
            using var fileStream = File.Open(cardImagePath, FileMode.Open);
            using var reader = new BinaryReader(fileStream);

            var cardType = imageBytes[0] switch
            {
                0 => CardType.MifareUltralightC,
                1 => CardType.MifareClassic1K,
                2 => CardType.MifareClassic4K,
                3 => CardType.MifarePlus2K,
                4 => CardType.MifareUltralightEv1,
                _ => CardType.Unknown
            };
            var uidLength = imageBytes[1];
            var uid = imageBytes[2..(2 + uidLength)];

            return new CardData(imageBytes, cardType, uid);
        }
        catch
        {
            return null;
        }
    }

}

public enum CardType
{
    /*
     Модель карты	        Общий объем памяти	Размер UID (байт) Примечания
     MIFARE Classic 1K	    1024 байт (1 КБ)	4	              Стандартная карта. UID постоянный и не может быть изменен.
     MIFARE Classic 4K	    4096 байт (4 КБ)	4 или 7	          Имеет 4-байтный UID, но некоторые версии (например, Plus 4K) поддерживают 7-байтный.
     MIFARE Plus 2K	        2048 байт (2 КБ)	4 или 7	          Более безопасная замена Classic. Поддерживает оба размера UID.
     MIFARE Plus 4K	        4096 байт (4 КБ)	4 или 7	          Аналог Classic 4K с улучшенной криптографией.
     MIFARE Ultralight	    64 байта	        7	              Бюджетная карта для одноразовых применений. Часто используется в NFC-билетах.
     MIFARE Ultralight C	192 байта	        7	              Версия Ultralight с шифрованием (3DES).
     MIFARE Ultralight EV1	64 или 192 байта	7	              Улучшенная версия с большим объемом памяти и функциями защиты.
     MIFARE DESFire EV2 2K	2048 байт (2 КБ)	7	              Карта высокого класса с файловой системой и продвинутой безопасностью.
     MIFARE DESFire EV3 4K	4096 байт (4 КБ)	7	              Флагманская модель с наибольшей памятью и безопасностью.
    */

    Unknown,
    MifareClassic1K,    // Объём памяти: 1024     Размер UID: 4
    MifareClassic4K,    // Объём памяти: 4096     Размер UID: 4 или 7
    MifarePlus2K,       // Объём памяти: 2048     Размер UID: 4 или 7
    MifarePlus4K,       // Объём памяти: 4096     Размер UID: 4 или 7
    MifareUltralight,   // Объём памяти: 64       Размер UID: 7
    MifareUltralightC,  // Объём памяти: 192      Размер UID: 7
    MifareUltralightEv1 // Объём памяти: 64/192   Размер UID: 7
}

public sealed class CardData
{
    public CardData(byte[] data, CardType type, byte[] uid)
    {
        Data = data;
        Type = type;
        FileSize = data.Length;

        var arr = new byte[8];
        Array.Copy(uid, arr, uid.Length);
        Uid = arr;
        UidString = BitConverter.ToUInt64(Uid, 0).ToString();
    }

    public byte[] Data { get; }
    public CardType Type { get; }
    public byte[] Uid { get; }
    public string UidString { get; }
    public int FileSize { get; }

    // TODO: доразобраться
    // private byte[] GetMifareClassicUid()
    // {
    //     // Для Mifare Classic UID находится в блоке 0 (байты 0-3)
    //     if (Data.Length >= 16)
    //     {
    //         var uid = new byte[4];
    //         Array.Copy(Data, 0, uid, 0, 4);
    //         return uid;
    //     }
    //     return [];
    // }
    //
    // private byte[] GetMifareUltralightUid()
    // {
    //     // Для Mifare Ultralight UID находится в страницах 0-1
    //     if (Data.Length >= 8)
    //     {
    //         var uid = new byte[7];
    //         Array.Copy(Data, 0, uid, 0, 7);
    //         return uid;
    //     }
    //     return [];
    // }
    //
    // private byte[] GetGenericUid()
    // {
    //     // Универсальный метод - пытаемся найти UID в начале файла
    //     if (Data.Length >= 4)
    //     {
    //         var uid = new byte[4];
    //         Array.Copy(Data, 0, uid, 0, 4);
    //         return uid;
    //     }
    //     return [];
    // }
}