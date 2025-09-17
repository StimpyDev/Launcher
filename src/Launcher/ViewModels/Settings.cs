using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Helpers;
using Launcher.Models;
using Launcher.Services;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.ViewModels;
public partial class Settings : ObservableObject
{
    private static Settings? _instance;
    private static readonly string _savePath;
    private static readonly Lock _lock = new();
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private bool discordActivity = true;

    [ObservableProperty]
    private bool parallelDownload = true;

    [ObservableProperty]
    private LocaleType locale = LocaleType.en_US;

    [ObservableProperty]
    private AvaloniaList<ServerInfo> serverInfoList = [];

    public event EventHandler? LocaleChanged;
    public event EventHandler? DiscordActivityChanged;

    static Settings()
    {
        _savePath = Path.Combine(Constants.SavePath, Constants.SettingsFile);
    }

    private Settings() { }

    public static Settings Instance
    {
        get
        {
            if (_instance is not null)
                return _instance;

            lock (_lock)
            {
                if (_instance is null)
                {
                    if (File.Exists(_savePath))
                        XmlHelper.TryDeserialize(_savePath, out _instance);

                    _instance ??= new Settings();
                }
            }
            return _instance;
        }
    }

    public void Save()
    {
        if (_instance == null) return;
        try
        {
            XmlHelper.TrySerialize(_instance, _savePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings.");
        }
    }

    partial void OnLocaleChanged(LocaleType value)
        => LocaleChanged?.Invoke(this, EventArgs.Empty);

    partial void OnDiscordActivityChanged(bool value)
    {
        if (value)
            DiscordService.Start();
        else
            DiscordService.Stop();

        DiscordActivityChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task OpenLogs()
    {
        string logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (!Directory.Exists(logsDir))
        {
            await UIThreadHelper.InvokeAsync(() =>
                App.AddNotification("Logs directory does not exist.", true)
            ).ConfigureAwait(false);
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logsDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", logsDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", logsDir);
            }
            else
            {
                await UIThreadHelper.InvokeAsync(() =>
                    App.AddNotification("Unknown OS platform. Please open the folder manually: " + logsDir, true)
                ).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error opening logs directory");
            await UIThreadHelper.InvokeAsync(() =>
                App.AddNotification($"Failed to open logs directory. Error: {ex.Message}", true)
            ).ConfigureAwait(false);
        }
    }
}
