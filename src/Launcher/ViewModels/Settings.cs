﻿using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Helpers;
using Launcher.Models;
using Launcher.Services;
using NLog;
using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace Launcher.ViewModels;
public partial class Settings : ObservableObject
{
    private static Settings? _instance;
    private static readonly string _savePath;
    private static readonly Lock _lock = new();
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private bool discordActivity = true;

    [ObservableProperty]
    private bool parallelDownload = true;

    [ObservableProperty]
    private int downloadThreads = 4;

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

    [XmlIgnore]
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
                        if (!XmlHelper.TryDeserialize(_savePath, out _instance))
                        {
                            _logger.Error($"Failed to deserialize settings from '{_savePath}'.");
                        }
                    }

                    _instance ??= new Settings();
                }
            }
            return _instance;
        }
    }

    public void Save()
    {
        if (!XmlHelper.TrySerialize(_instance, _savePath))
        {
            _logger.Error($"Failed to serialize and save settings to '{_savePath}'.");
        }
    }

    partial void OnParallelDownloadChanged(bool value)
    {
        Save();
    }
    partial void OnDownloadThreadsChanged(int value)
    {
        if (value != Math.Clamp(value, 2, 8))
            DownloadThreads = Math.Clamp(value, 2, 8);
        else
            Save();
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
}
