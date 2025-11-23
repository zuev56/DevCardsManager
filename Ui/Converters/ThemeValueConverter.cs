using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ArgumentException = System.ArgumentException;

namespace DevCardsManager.Ui.Converters;

public sealed class ThemeValueConverter : MarkupExtension, IValueConverter
{
    public object DarkValue { get; set; } = null!;
    public object LightValue { get; set; } = null!;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ThemeVariant theme)
            throw new ArgumentException("value is not a ThemeVariant");

        return theme switch
        {
            {} when theme == ThemeVariant.Dark => DarkValue,
            {} when theme == ThemeVariant.Light => LightValue,
            _ => throw new ArgumentOutOfRangeException("Theme must be Dark or Light")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}