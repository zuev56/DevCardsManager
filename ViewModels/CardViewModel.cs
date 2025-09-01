using ReactiveUI.Fody.Helpers;

namespace DevCardsManager.ViewModels;

public sealed class CardViewModel : ViewModelBase
{
    public required string Path { get; init; }
    public string CardName => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string FileName => System.IO.Path.GetFileName(Path);

    [Reactive]
    public int PinIndex { get; private set; } = -1;

    public bool IsPinned => PinIndex >= 0;

    [Reactive]
    public bool IsInserted { get; set; }

    public void Pin(int? index) => PinIndex = index ?? -1;
}