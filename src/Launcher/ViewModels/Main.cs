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
    private bool isRefreshing;

    [ObservableProperty]
    private SemanticVersion version = App.CurrentVersion;

    public AvaloniaList<Server> Servers { get; } = [];
    public AvaloniaList<Notification> Notifications { get; } = [];

    public Main()
    {
#if DESIGNMODE
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            Servers.Clear();
            var demoServer = new Server();
            Servers.Add(demoServer);
            ActiveServer = demoServer;
        }
#endif

        Settings.Instance.ServerInfoList.CollectionChanged += ServerInfoList_CollectionChanged;
        Settings.Instance.DiscordActivityChanged += (_, _) => UpdateDiscordActivity();
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
            Servers.Add(new Server(serverInfo, this));
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
            DiscordService.UpdateActivity(App.GetText("Text.Discord.Playing"), string.Join(", ", serversPlaying));
        }
        else
        {
            DiscordService.UpdateActivity(App.GetText("Text.Discord.Idle"), string.Empty);
        }
    }

    [RelayCommand]
    public static Task CheckForUpdates() => App.CheckForUpdatesAsync();

    [RelayCommand]
    public static void ShowSettings() => App.ShowSettings();

    [RelayCommand]
    public static Task AddServer() => App.ShowPopupAsync(new AddServer());

    [RelayCommand]
    public async Task DeleteServer()
    {
        if (ActiveServer == null)
            return;

        await App.ShowPopupAsync(new DeleteServer(ActiveServer.Info)).ConfigureAwait(false);
    }

    public async Task OnReceiveNotification(Notification notification)
    {
        if (Notifications.Count >= 3)
            return;

        Notifications.Add(notification);

        await Task.Delay(1000).ConfigureAwait(false);
        Notifications.Remove(notification);
    }
}