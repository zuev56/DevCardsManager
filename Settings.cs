namespace DevCardsManager;

public sealed class Settings
{
    public required string AllCardsPath { get; set; }
    public required string InsertedCardPath { get; set; }
    public required int ReplaceCardDelayMs { get; set; }
    public required int InsertCardOnTimeMs { get; set; }
}