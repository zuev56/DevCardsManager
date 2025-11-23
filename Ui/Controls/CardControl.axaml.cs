using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DevCardsManager.Ui.Controls;

public sealed partial class CardControl : UserControl
{
    public CardControl()
    {
        InitializeComponent();
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: CardViewModel cardViewModel } border)
        {
            if (border.Parent.Parent.DataContext is CardManagerViewModel viewModel)
                viewModel.SelectedCard = cardViewModel;
        }
    }

    private void InfoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (InfoPopup.IsOpen)
            InfoPopup.Close();
        else
            InfoPopup.Open();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e) => InfoPopup.Close();
}