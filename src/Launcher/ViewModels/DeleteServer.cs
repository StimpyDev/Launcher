using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Launcher.ViewModels;
public partial class DeleteServer : Popup
{
    [ObservableProperty]
    private ServerInfo info;

    public IAsyncRelayCommand DeleteServerCommand { get; }
    public ICommand CancelDeleteServerCommand { get; }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public DeleteServer(ServerInfo info)
    {
        Info = info;

        DeleteServerCommand = new AsyncRelayCommand(OnDeleteServer);
        CancelDeleteServerCommand = new RelayCommand(OnDeleteServerCancel);

        View = new Views.DeleteServer
        {
            DataContext = this
        };
    }

    private async Task OnDeleteServer()
    {
        await App.ProcessPopupAsync();
    }

    private void OnDeleteServerCancel()
    {
        App.CancelPopup();
    }

    public override async Task<bool> ProcessAsync()
    {
        ProgressDescription = App.GetText("Text.Delete_Server.Loading");
        return await OnDeleteServerAsync().ConfigureAwait(false);
    }

    private async Task<bool> OnDeleteServerAsync()
    {
        try
        {
            await ForceDeleteDirectoryAsync(Info.SavePath);
        }
        catch (Exception ex)
        {
            await UIThreadHelper.InvokeAsync(async () =>
            {
                try
                {
                    await App.AddNotification($"Failed to delete server directory: {ex.Message}", true);
                }
                catch (Exception notifyEx)
                {
                    _logger.Error(notifyEx, "Error showing notification");
                }
                _logger.Error(ex, "Error deleting server directory");
            });
            return false;
        }

        await UIThreadHelper.InvokeAsync(() =>
        {
            try
            {
                Settings.Instance.ServerInfoList.Remove(Info);
                Settings.Save();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error removing server info or saving settings");
            }
            return Task.CompletedTask;
        });

        return true;
    }
    private async Task ForceDeleteDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
            return;

        await Task.Run(() =>
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path)
                {
                    Attributes = FileAttributes.Normal
                };

                foreach (var info in directoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                directoryInfo.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to delete directory: {path}");
                throw;
            }
        });
    }
}