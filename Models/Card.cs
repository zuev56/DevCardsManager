namespace DevCardsManager.Models;

public sealed class Card
{
    public required string Path { get; init; }
    public int? PinIndex { get; set; }
    public bool IsInserted { get; set; }
    public void UnPin() => PinIndex = null;
}