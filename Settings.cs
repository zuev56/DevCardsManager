using System.Collections.Generic;

namespace DevCardsManager;

public sealed class Settings
{
    public required string AllCardsPath { get; set; }
    public required string InsertedCardPath { get; set; }
    public required int ReplaceCardDelayMs { get; set; }
    public required int InsertCardOnTimeMs { get; set; }
    public bool SortAscending { get; set; } = true;
    public List<string> PinnedCards { get; set; } = [];
}