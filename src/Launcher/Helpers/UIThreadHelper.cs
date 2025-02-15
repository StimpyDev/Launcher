using Avalonia.Threading;
using System;

namespace Launcher.Helpers;

public static class UIThreadHelper
{
    public static void Invoke(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Invoke(action);
    }
}