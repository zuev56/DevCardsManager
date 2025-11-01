using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevCardsManager.Services;
using DevCardsManager.ViewModels;
using DevCardsManager.Views;
using Prism.DryIoc;
using Prism.Ioc;

namespace DevCardsManager;

public sealed partial class App : PrismApplication
{
    public override void Initialize()
    {
        // TODO: Есть сомнения в необходимости AvaloniaXamlLoader.Load после перехода на Prism, ведь теперь есть base.Initialize();
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        var container = (IContainerExtension)Container;
        container.RegisterSingleton<Logger>();
        container.RegisterSingleton<SettingsManager>();
        container.RegisterSingleton<CardManager>();
        container.RegisterSingleton<DirectoryWatcher>();

    }

    protected override AvaloniaObject CreateShell() => Container.Resolve<MainWindow>();

    protected override void OnInitialized()
    {
        // Инициализация после создания Shell
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = (Window)MainWindow;
        }

        base.OnInitialized();
    }
}