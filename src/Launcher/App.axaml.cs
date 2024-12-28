using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;

using NLog;

using Velopack;
using Velopack.Sources;

using NuGet.Versioning;

using Launcher.Models;
using Launcher.Helpers;
using Launcher.ViewModels;

namespace Launcher;

public partial class App : Application
{
    private ResourceDictionary? _activeLocale;

    private readonly Logger _logger;

    private Main? _main;

    private const string GitHubRepoUrl = "https://github.com/Open-Source-Free-Realms/Launcher";
    private static readonly UpdateManager _updateManager = new(new GithubSource(GitHubRepoUrl, null, false));

    public static SemanticVersion CurrentVersion => _updateManager.CurrentVersion ?? new SemanticVersion(0, 0, 0);

    public App()
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        SetLocale(Settings.Instance.Locale);

        Settings.Instance.LocaleChanged += (s, e) =>
        {
            SetLocale(Settings.Instance.Locale);
        };
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime applicationLifetime)
            return;

        var settings = Settings.Instance;

        var splash = new Views.Splash();

        applicationLifetime.MainWindow = splash;

#if RELEASE
        if (_updateManager.IsInstalled)
        {
            splash.ViewModel.Message = GetText("Text.Splash.CheckForUpdates");

            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo is not null)
            {
                // Migrate the files outside of the install directory
                if (_updateManager.CurrentVersion == new SemanticVersion(1, 0, 0) &&
                    updateInfo.TargetFullRelease.Version == new SemanticVersion(1, 0, 1))
                {
                    if (!MigrateFiles())
                    {
                        splash.ViewModel.Message = GetText("Text.Splash.MigrateError");

                        await Task.Delay(1000);
                    }
                }

                await _updateManager.DownloadUpdatesAsync(updateInfo, (p) =>
                {
                    splash.ViewModel.Message = GetText("Text.Splash.DownloadProgress", updateInfo.TargetFullRelease.Version, p);
                });

                splash.ViewModel.Message = GetText("Text.Splash.RestartLauncher");

                await Task.Delay(500);

                _updateManager.ApplyUpdatesAndRestart(updateInfo);

                return;
            }
        }
#endif

        splash.ViewModel.Message = GetText("Text.Splash.LauncherUpToDate");

        await Task.Delay(500);

        var main = new Views.Main();

        _main = main.ViewModel;

        applicationLifetime.MainWindow = main;

        main.Show();
        splash.Close();

        base.OnFrameworkInitializationCompleted();
    }

    public static void SetLocale(LocaleType value)
    {
        if (Current is not App app)
            return;

        if (!app.Resources.TryGetValue(value.ToString(), out var localeResource) ||
            localeResource is not ResourceDictionary targetLocale)
        {
            app._logger.Error("Invalid locale. {locale}", value);

            return;
        }

        if (app._activeLocale is not null)
            app.Resources.MergedDictionaries.Remove(app._activeLocale);

        app.Resources.MergedDictionaries.Add(targetLocale);

        app._activeLocale = targetLocale;
    }

    public static string GetText(string key, params object?[] args)
    {
        if (Current is not App app)
            return key;

        var text = Current.FindResource(key) as string;

        if (text is null)
            return $"#{key}";

        return string.Format(text, args);
    }

    public static void AddNotification(string message, bool isError = false)
    {
        if (Current is not App app || app._main is null)
            return;

        var notice = new Notification()
        {
            IsError = isError,
            Message = message
        };

        app._logger.Log(isError ? LogLevel.Error : LogLevel.Info, message);

        app._main.OnReceiveNotification(notice);
    }

    public static void ShowSettings()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            var dialog = new Views.Settings();

            dialog.ShowDialog(desktop.MainWindow);
        }
    }

    public static void ClearServerSelection()
    {
        if (Current is not App app || app._main is null)
            return;

        app._main.ActiveServer = null;
    }

    public static void ShowPopup(Popup popup, bool process = false)
    {
        if (Current is not App app || app._main is null)
            return;

        if (app._main.Popup is not null && app._main.Popup.InProgress)
            return;

        app._main.Popup = popup;

        if (process)
            ProcessPopup();
    }

    public static async void ProcessPopup()
    {
        if (Current is not App app || app._main is null)
            return;

        if (app._main.Popup is null || !app._main.Popup.Validate())
            return;

        app._main.Popup.InProgress = true;

        var task = app._main.Popup.ProcessAsync();

        if (task is null)
        {
            app._main.Popup = null;
            return;
        }

        var finished = await task;

        if (finished)
            app._main.Popup = null;
        else
            app._main.Popup.InProgress = false;
    }

    public static void CancelPopup()
    {
        if (Current is not App app || app._main is null)
            return;

        if (app._main.Popup is null || app._main.Popup.InProgress)
            return;

        app._main.Popup = null;
    }

    private bool MigrateFiles()
    {
        // Migrate settings
        var oldSettingsFile = Path.Combine(Environment.CurrentDirectory, Constants.SettingsFile);

        var newSettingsFile = Path.Combine(Constants.SavePath, Constants.SettingsFile);

        try
        {
            File.Move(oldSettingsFile, newSettingsFile);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to migrate settings. {error}", ex.ToString());

            return false;
        }

        // Migrate servers
        var oldServersDirectory = Path.Combine(Environment.CurrentDirectory, Constants.ServersDirectory);

        var newServersDirectory = Path.Combine(Constants.SavePath, Constants.ServersDirectory);

        try
        {
            Directory.Move(oldServersDirectory, newServersDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to migrate servers. {error}", ex.ToString());

            return false;
        }

        return true;
    }
}