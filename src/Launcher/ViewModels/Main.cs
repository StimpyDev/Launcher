using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Models;
using Launcher.Services;
using NuGet.Versioning;
using System.Collections.Specialized;
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

    public AvaloniaList<Server> Servers { get; set; } = new AvaloniaList<Server>();
    public AvaloniaList<Notification> Notifications { get; set; } = new AvaloniaList<Notification>();

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

        Settings.Instance.DiscordActivityChanged += (s, e) => UpdateDiscordActivity();
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
    public Task CheckForUpdates() => App.CheckForUpdatesAsync();

    [RelayCommand]
    public void ShowSettings() => App.ShowSettings();

    [RelayCommand]
    public Task AddServer() => App.ShowPopupAsync(new AddServer());

    [RelayCommand]
    public async Task DeleteServer()
    {
        if (ActiveServer is null)
            return;

        await App.ShowPopupAsync(new DeleteServer(ActiveServer.Info));
    }

    public async Task OnReceiveNotification(Notification notification)
    {
        if (Notifications.Count == 1)
            return;

        Notifications.Add(notification);

        // Automatically dismisses notification after 5 seconds.
        await Task.Delay(5000);
        Notifications.Remove(notification);
    }
}