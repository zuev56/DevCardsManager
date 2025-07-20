using System.Collections.Generic;

namespace DevCardsManager.ViewModels;

public abstract class ParameterViewModel : ViewModelBase
{
    public abstract string PropertyName { get; }
};

public abstract class ParameterViewModel<TValue>(string propertyName, string displayName, TValue value) : ParameterViewModel
{
    private TValue _value = value;

    public override string PropertyName { get; } = propertyName;
    public string DisplayName { get; } = displayName;

    public TValue Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<TValue>.Default.Equals(value, _value))
                return;

            _value = value;
            OnPropertyChanged(PropertyName);
        }
    }
}

public sealed class IntegerParameterViewModel(string propertyName, string displayName, int value)
    : ParameterViewModel<int>(propertyName, displayName, value);
public sealed class StringParameterViewModel(string propertyName, string displayName, string value)
    : ParameterViewModel<string>(propertyName, displayName, value);
public sealed class BooleanParameterViewModel(string propertyName, string displayName, bool value)
    : ParameterViewModel<bool>(propertyName, displayName, value);