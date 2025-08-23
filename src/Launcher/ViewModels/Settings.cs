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
using System.Xml.Serialization;

namespace Launcher.ViewModels;

public partial class Settings : ObservableObject
{
    private static Settings? _instance = null;
    private static readonly string _savePath = Path.Combine(Constants.SavePath, Constants.SettingsFile);

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

    [XmlIgnore]
    public static Settings Instance
    {
        get
        {
            if (_instance is not null)
                return _instance;

            if (File.Exists(_savePath))
                XmlHelper.TryDeserialize(_savePath, out _instance);

            _instance ??= new Settings();

            return _instance;
        }
    }

    public void Save()
    {
        XmlHelper.TrySerialize(_instance, _savePath);
    }

    [RelayCommand]
    public void OpenLogs()
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                Verb = "open",
                UseShellExecute = true,
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs"),
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "logs")
            });
        }
        catch (Exception ex)
        {
            UIThreadHelper.Invoke(() =>
            {
                App.AddNotification($"""
                                     An exception was thrown while opening logs.
                                     Exception: {ex}
                                     """, true);

                _logger.Error(ex.ToString());
            });
        }
    }
}