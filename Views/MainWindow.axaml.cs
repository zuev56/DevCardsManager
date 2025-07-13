using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using DevCardsManager.ViewModels;

namespace DevCardsManager.Views;

public sealed partial class MainWindow : Window
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

    private void ChangeThemeMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}