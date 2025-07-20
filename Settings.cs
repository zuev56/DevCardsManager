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

    [Ignore]
    public List<string> PinnedCards { get; set; } = [];
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute;