namespace DevCardsManager.Models;

/// <summary>
/// Низкоуровневая модель карты
/// </summary>
public sealed class CardDump
{
    public CardDump(byte[] bytes, CardModel model, byte[] uid)
    {
        Bytes = bytes;
        Model = model;
        Uid = uid;
        FileSize = bytes.Length;
    }

    public byte[] Bytes { get; }
    public byte[] Uid { get; }
    public int FileSize { get; }
    public CardModel Model { get; }
}