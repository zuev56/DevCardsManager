using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DevCardsManager.Models;

public sealed class Settings
{
    public const string FileName = "appsettings.json";

    [DisplayName("Каталог со всеми картами")]
    public required string AllCardsPath { get; set; }

    [DisplayName("Каталог со вставленной картой")]
    public required string InsertedCardPath { get; set; }

    [DisplayName("Интервал между заменой карт, мс")]
    public required int ReplaceCardDelayMs { get; set; }

    [DisplayName("Интервал временного прикладывания карты, мс")]
    public required int InsertCardOnTimeMs { get; set; }

    [DisplayName("Сортировка в алфавитном порядке")]
    public bool SortAscending { get; set; } = true;

    [DisplayName("Тёмная тема")]
    public bool UseDarkTheme { get; set; } = true;

    [DisplayName("Отображать поверх остальных окон")]
    public bool KeepOnTop { get; set; }

    [DisplayName("Сохранять изменения на карте после её использования")]
    public bool SaveCardChangesOnReturn { get; set; } = true;

    [DisplayName("Детальное логирование")]
    public bool DetailedLogging { get; set; }

    // [DisplayName("Включить возможность прикладывания карты на время")]
    // public bool AllowTemporarilyAttach { get; set; }

    [Ignore]
    public List<string> PinnedCards { get; set; } = [];
}

/// <summary>
/// Свойства, помеченные этим атрибутом не будут выводиться в окне настроек
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute;