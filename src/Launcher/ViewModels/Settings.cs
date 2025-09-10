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
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.ViewModels;

public partial class Settings : ObservableObject
{
    private static Settings? _instance = null;
    private static readonly string _savePath = Path.Combine(Constants.SavePath, Constants.SettingsFile);
    private static readonly Lock _lock = new();

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

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    partial void OnLocaleChanged(LocaleType value)
    {
        LocaleChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnDiscordActivityChanged(bool value)
    {
        if (value)
            DiscordService.Start();
        else
            DiscordService.Stop();

        DiscordActivityChanged?.Invoke(this, EventArgs.Empty);
    }

    private Settings()
    {

    }

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
                    {
                        XmlHelper.TryDeserialize(_savePath, out _instance);
                    }
                    _instance ??= new Settings();
                }
            }
            return _instance;
        }
    }

    public static void Save()
    {
        XmlHelper.TrySerialize(_instance, _savePath);
    }

    [RelayCommand]
    public async Task OpenLogs()
    {
        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

        if (!Directory.Exists(logsDir))
        {
            await UIThreadHelper.InvokeAsync(async () =>
            {
                await App.AddNotification("Logs directory does not exist.", true).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo()
            {
                Verb = "open",
                UseShellExecute = true,
                WorkingDirectory = logsDir,
                FileName = logsDir
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            await UIThreadHelper.InvokeAsync(async () =>
            {
                await App.AddNotification($"An exception was thrown while opening logs. Exception: {ex}", true).ConfigureAwait(false);
                _logger.Error(ex.ToString());
            }).ConfigureAwait(false);
        }
    }
}