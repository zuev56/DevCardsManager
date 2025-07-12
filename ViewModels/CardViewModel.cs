namespace DevCardsManager.ViewModels;

public sealed class CardViewModel : ViewModelBase
{
    private bool _isInserted;
    private int _pinIndex = -1;
    public required string Path { get; init; }
    public string CardName => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string FileName => System.IO.Path.GetFileName(Path);

    public int PinIndex
    {
        get => _pinIndex;
        internal set
        {
            _pinIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsPinned => PinIndex >= 0;

    public bool IsInserted
    {
        get => _isInserted;
        internal set
        {
            _isInserted = value;
            OnPropertyChanged();
        }
    }

    public void UnPin() => PinIndex = -1;
}