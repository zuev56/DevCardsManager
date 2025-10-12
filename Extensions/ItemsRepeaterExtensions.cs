using Avalonia;
using Avalonia.Controls;

namespace DevCardsManager.Extensions;

public static class ItemsRepeaterSelection
{
    public static readonly AttachedProperty<object?> SelectedItemProperty =
        AvaloniaProperty.RegisterAttached<ItemsRepeater, object?>(
            "SelectedItem", typeof(ItemsRepeaterSelection));

    public static object? GetSelectedItem(ItemsRepeater element) =>
        element.GetValue(SelectedItemProperty);

    public static void SetSelectedItem(ItemsRepeater element, object? value) =>
        element.SetValue(SelectedItemProperty, value);
}