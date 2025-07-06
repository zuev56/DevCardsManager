using Avalonia.Controls;
using Avalonia.Interactivity;
using DevCardsManager.ViewModels;

namespace DevCardsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        viewModel.AddToClipboardAsync = text => Clipboard!.SetTextAsync(text);
        viewModel.ReadClipboardAsync = () => Clipboard!.GetTextAsync();
    }
}