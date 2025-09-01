using Avalonia;
using Avalonia.Controls;

namespace DevCardsManager.Extensions;

public static class ControlExtensions
{
    public static readonly AttachedProperty<string> ToolTipTextProperty =
        AvaloniaProperty.RegisterAttached<Control, string>(
            "ToolTipText",
            typeof(ControlExtensions),
            defaultValue: null!,
            inherits: false);

    public static string GetToolTipText(Control control) =>
        control.GetValue(ToolTipTextProperty);

    public static void SetToolTipText(Control control, string value)
    {
        control.SetValue(ToolTipTextProperty, value);

        if (!string.IsNullOrEmpty(value))
        {
            ToolTip.SetTip(control, value);
            ToolTip.SetShowDelay(control, 500);
        }
        else
        {
            ToolTip.SetTip(control, null);
        }
    }
}