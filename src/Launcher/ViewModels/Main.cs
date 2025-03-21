﻿using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Helpers;
using Launcher.Models;
using Launcher.Services;
using NuGet.Versioning;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Launcher.ViewModels;

public partial class Main : ObservableObject
{
    [ObservableProperty]
    private Popup? popup;

    [ObservableProperty]
    private Server? activeServer;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private SemanticVersion version = App.CurrentVersion;

    public AvaloniaList<Server> Servers { get; set; } = [];
    public AvaloniaList<Notification> Notifications { get; set; } = [];

    public Main()
    {
#if DESIGNMODE
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            Servers.Clear();

            Servers.Add(new Server());

            ActiveServer = Servers.FirstOrDefault();
        }
#endif

        Settings.Instance.DiscordActivityChanged += (s, e) =>
        {
            UpdateDiscordActivity();
        };

        Settings.Instance.ServerInfoList.CollectionChanged += ServerInfoList_CollectionChanged;
    }

    private void ServerInfoList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex != -1)
        {
            var serverInfo = Settings.Instance.ServerInfoList[e.NewStartingIndex];

            Servers.Add(new Server(serverInfo, this));
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex != -1)
        {
            Servers.RemoveAt(e.OldStartingIndex);
        }
    }

    public void OnLoad()
    {
        foreach (var serverInfo in Settings.Instance.ServerInfoList)
        {
            var server = new Server(serverInfo, this);

            // TODO: Check if a client is already open per server

            Servers.Add(server);
        }

        UpdateDiscordActivity();
    }

    public void UpdateDiscordActivity()
    {
        if (!Settings.Instance.DiscordActivity)
            return;

        var serversPlaying = Servers.Where(x => x.Process is not null).Select(x => x.Info.Name);

        if (serversPlaying.Any())
        {
            var servers = string.Join(", ", serversPlaying);
            DiscordService.UpdateActivity(App.GetText("Text.Discord.Playing"), servers);
        }
        else
        {
            DiscordService.UpdateActivity(App.GetText("Text.Discord.Idle"), "");
        }
    }

    [RelayCommand]
    public async Task CheckForUpdates()
    {
        await App.CheckForUpdatesAsync();
    }

    [RelayCommand]
    public void ShowSettings()
    {
        App.ShowSettings();
    }

    [RelayCommand]
    public void OpenFolder()
    {
        Process.Start(new ProcessStartInfo()
        {
            Verb = "open",
            UseShellExecute = true,
            FileName = Constants.SavePath
        });
    }

    [RelayCommand]
    public async Task AddServer()
    {
        await App.ShowPopupAsync(new AddServer());
    }

    [RelayCommand]
    public async Task DeleteServer()
    {
        if (ActiveServer is null)
            return;

        await App.ShowPopupAsync(new DeleteServer(ActiveServer.Info));
    }

    public void OnReceiveNotification(Notification notification)
    {
        Notifications.Add(notification);
    }

    public void DismissNotification(object e)
    {
        if (e is not Notification notification)
            return;

        Notifications.Remove(notification);
    }
}