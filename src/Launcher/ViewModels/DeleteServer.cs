using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Launcher.ViewModels;

public partial class DeleteServer : Popup
{
    [ObservableProperty]
    private ServerInfo info;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public DeleteServer(ServerInfo info)
    {
        Info = info;
        View = new Views.DeleteServer()
        {
            DataContext = this
        };
    }

    public override async Task<bool> ProcessAsync()
    {
        ProgressDescription = App.GetText("Text.Delete_Server.Loading");
        return await OnDeleteServerAsync();
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
                await App.AddNotification($"Failed to delete server directory: {ex.Message}", true);
                _logger.Error(ex, "Error deleting server directory");
            });
            return false;
        }

        // Remove server info
        Settings.Instance.ServerInfoList.Remove(Info);
        Settings.Save();

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
                var directory = new DirectoryInfo(path)
                {
                    Attributes = FileAttributes.Normal
                };

                foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                directory.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to delete directory: {path}");
                throw;
            }
        });
    }
}