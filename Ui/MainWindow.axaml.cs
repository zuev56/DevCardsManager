using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace DevCardsManager.Ui;

// TODO: Сохранять размер экрана и положение при выходе
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Activated += (_, _) => Opacity = 1.0;
        Deactivated += (_, _) =>
        {
            if (Topmost)
            {
                var viewModel = (MainWindowViewModel)DataContext!;
                Opacity = 1 - viewModel.SettingsViewModel.Settings.KeepOnTopTransparency / 100d;
            }
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        viewModel.CardManager.AddToClipboardAsync = text => Clipboard!.SetTextAsync(text);
        viewModel.CardManager.ReadClipboardAsync = () => Clipboard!.GetTextAsync();

        viewModel.UtilitiesViewModel.LogFilterViewModel.AddToClipboardAsync = text => Clipboard!.SetTextAsync(text);
        viewModel.UtilitiesViewModel.LogFilterViewModel.ReadClipboardAsync = () => Clipboard!.GetTextAsync();

        ForceLayoutRecalculation();
    }

    /// <summary>
    /// Принудительная перерисовка всех элементов.
    /// </summary>
    /// <remarks>
    /// После подключения Prism и ReactiveUI интерфейс как-будто бы стал перестраиваться при первом навидении мышью на контролы.
    /// Таким образом эта проблема разрешиась.
    /// </remarks>
    private void ForceLayoutRecalculation()
    {
        InvalidateArrange();
        InvalidateMeasure();
        InvalidateVisual();

        foreach (var child in this.GetVisualDescendants().OfType<Layoutable>())
        {
            child.InvalidateArrange();
            child.InvalidateMeasure();
            child.InvalidateVisual();
        }
    }
}