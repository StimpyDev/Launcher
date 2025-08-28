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

    public override Task<bool> ProcessAsync()
    {
        ProgressDescription = App.GetText("Text.Delete_Server.Loading");

        return Task.Run(OnDeleteServer);
    }

    private bool OnDeleteServer()
    {
        try
        {
            ForceDeleteDirectory(Info.SavePath);
        }
        catch (Exception ex)
        {
            UIThreadHelper.Invoke(async () =>
            {
                await App.AddNotification($"""
                                     An exception was thrown while deleting server.
                                     Exception: {ex}
                                     """, true);

                _logger.Error(ex.ToString());
            });

            return true;
        }

        Settings.Instance.ServerInfoList.Remove(Info);

        Settings.Save();

        return true;
    }

    private void ForceDeleteDirectory(string path)
    {
        var directory = new DirectoryInfo(path)
        {
            Attributes = FileAttributes.Normal
        };

        // Clear all file/directory attributes.
        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            info.Attributes = FileAttributes.Normal;

        directory.Delete(true);
    }
}