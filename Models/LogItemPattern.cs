using System;

namespace DevCardsManager.Models;

public sealed class LogItemPattern
{
    public event Action? Changed;

    private string? _content;
    private bool _isActive;

    public string? Content
    {
        get => _content;
        set
        {
            if (string.Equals(value, _content, StringComparison.InvariantCultureIgnoreCase))
                return;

            _content = value;
            Changed?.Invoke();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;

            _isActive = value;
            Changed?.Invoke();
        }
    }
}