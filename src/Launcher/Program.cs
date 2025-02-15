using Avalonia;
using Avalonia.Logging;
using Launcher.Extensions;
using Launcher.Helpers;
using Launcher.Services;
using Launcher.ViewModels;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using Velopack;

namespace Launcher;

internal sealed class Program
{
    [STAThread]
    internal static void Main(string[] args)
    {
        SetupNLog();

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        VelopackApp.Build().Run();

        if (Settings.Instance.DiscordActivity)
            DiscordService.Start();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        LogManager.Shutdown();
    }

    internal static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>();

        builder.WithInterFont();
        builder.UsePlatformDetect();

#if DEBUG
        builder.LogToTrace();
#endif

        builder.LogToNLog(LogEventLevel.Error);

        return builder;
    }

    private static void SetupNLog()
    {
        var loggingConfiguration = new LoggingConfiguration();

#if DEBUG
        var debuggerTarget = new DebuggerTarget("debugger");
        loggingConfiguration.AddRule(LogLevel.Debug, LogLevel.Fatal, debuggerTarget);
#endif

        var fileTarget = new FileTarget("file")
        {
            DeleteOldFileOnStartup = true,
            FileName = Path.Combine(Constants.SavePath, Constants.LogFile)
        };

        loggingConfiguration.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);

        LogManager.Configuration = loggingConfiguration;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = LogManager.GetCurrentClassLogger();

        if (e.ExceptionObject is Exception exception)
            logger.Log(LogLevel.Fatal, exception.ToString());
    }
}