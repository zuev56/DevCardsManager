using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DevCardsManager;

public sealed class Settings
{
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

    [DisplayName("Сохранять изменения на карте при её возврате")]
    public bool SaveCardChangesOnReturn { get; set; } = true;

    [DisplayName("Детальное логирование")]
    public bool DetailedLogging { get; set; }

    [Ignore]
    public List<string> PinnedCards { get; set; } = [];
}

/// <summary>
/// Свойства, помеченные этим атрибутом не будут выводиться в окне настроек
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute;