using System;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace DevCardsManager.Extensions;

public static class CommandExtensions
{
    public static void NotifyCanExecuteChanged(this ICommand command)
    {
        if (command is IRelayCommand relayCommand)
            Dispatcher.UIThread.InvokeAsync(() => relayCommand.NotifyCanExecuteChanged());
        else
            throw new ArgumentException("Command is not a RelayCommand");
    }
}