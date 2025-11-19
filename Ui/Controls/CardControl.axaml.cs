using Avalonia.Controls;
using Avalonia.Input;

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
}