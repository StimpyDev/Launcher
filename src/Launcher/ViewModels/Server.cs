using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using HashDepot;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.ViewModels;
public partial class Server : ObservableObject
{
    private readonly Main _main = null!;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private ServerInfo info = null!;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string status = App.GetText("Text.ServerStatus.Offline");

    [ObservableProperty]
    private int onlinePlayers;

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private Server? activeServer;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private Process? process;

    [ObservableProperty]
    private IBrush? serverStatusFill;

    [ObservableProperty]
    private bool isDownloading = false;

    [ObservableProperty]
    private int filesDownloaded;

    [ObservableProperty]
    private int totalFilesToDownload;

    public Server()
    {
#if DESIGNMODE
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            Info = new ServerInfo
            {
                Url = "https://example.com",
                Name = "Name",
                Description = "Description",
                SavePath = "Name",
                LoginServer = "127.0.0.1:20042",
                LoginApiUrl = "https://example.com"
            };
        }
#endif
    }

    public Server(ServerInfo info, Main main)
    {
        Info = info;
        _main = main;
    }
    public async Task<bool> OnShowAsync()
    {
        if (!await RefreshServerInfoAsync().ConfigureAwait(false))
            return false;

        await RefreshServerStatusAsync().ConfigureAwait(false);
        return true;
    }

    public void ClientProcessExited(object? sender, EventArgs e)
    {
        Process?.Dispose();
        Process = null;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task RefreshServerStatusAsync()
    {
        await UIThreadHelper.InvokeAsync(() =>
        {
            Status = "Refreshing";
            ServerStatusFill = new SolidColorBrush(Color.FromRgb(204, 204, 0));
            IsRefreshing = true;
            return Task.CompletedTask;
        });

        try
        {
            var serverStatus = await ServerStatusHelper.GetAsync(Info.LoginServer).ConfigureAwait(false);
            IsOnline = serverStatus.IsOnline;

            await UIThreadHelper.InvokeAsync(() =>
            {
                if (serverStatus.IsOnline)
                {
                    Status = App.GetText(serverStatus.IsLocked
                        ? "Text.ServerStatus.Locked"
                        : "Text.ServerStatus.Online");

                    OnlinePlayers = serverStatus.OnlinePlayers;
                    ServerStatusFill = new SolidColorBrush(
                        serverStatus.IsLocked
                            ? Color.FromRgb(242, 63, 67)
                            : Color.FromRgb(35, 165, 90));
                }
                else
                {
                    Status = App.GetText("Text.ServerStatus.Offline");
                    OnlinePlayers = 0;
                    ServerStatusFill = new SolidColorBrush(Color.FromRgb(242, 63, 67));
                }

                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error refreshing server status.");
            await App.AddNotification($"Failed to refresh server status: {ex.Message}", true);
        }
        finally
        {
            await UIThreadHelper.InvokeAsync(() =>
            {
                IsRefreshing = false;
                return Task.CompletedTask;
            });
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task PlayAsync()
    {
        if (Process != null)
        {
            await App.AddNotification("Unable to launch, the game is already open.", true);
            _logger.Warn("Unable to launch, the game is already open.");
            return;
        }

        var clientManifest = await GetClientManifestAsync().ConfigureAwait(false);
        if (clientManifest is null)
            return;

        StatusMessage = App.GetText("Text.Server.VerifyClientFiles");

        if (!await VerifyClientFilesAsync(clientManifest).ConfigureAwait(false))
        {
            await App.AddNotification("Failed to verify client files, please try again", true);
            _logger.Warn("Failed to verify client files");
            return;
        }

        if (!IsOnline)
        {
            StatusMessage = string.Empty;
            await App.AddNotification("Cannot login: The server is offline.", true);
            return;
        }

        await UIThreadHelper.InvokeAsync(async () =>
        {
            StatusMessage = string.Empty;
            await App.ShowPopupAsync(new Login(this));
        });
    }

    [RelayCommand]
    public async Task OpenClientFolder()
    {
        string folderPath = Path.Combine(Constants.SavePath, Info.SavePath);

        if (!Directory.Exists(folderPath))
        {
            await App.AddNotification($"The client folder does not exist: {folderPath}", true);
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", folderPath);
            }

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error opening client folder directory");
            await App.AddNotification($"Failed to open client folder directory. Error: {ex.Message}", true);
        }
    }
    private async Task<bool> RefreshServerInfoAsync()
    {
        try
        {
            var result = await HttpHelper.GetServerManifestAsync(Info.Url).ConfigureAwait(false);
            if (!result.Success || result.ServerManifest is null)
            {
                await UIThreadHelper.InvokeAsync(async () =>
                {
                    await App.AddNotification(result.Error, true);
                    _logger.Error(result.Error);
                });
                return false;
            }
            var serverManifest = result.ServerManifest;
            Info.Name = serverManifest.Name;
            Info.Description = serverManifest.Description;
            Info.LoginServer = serverManifest.LoginServer;
            Info.LoginApiUrl = serverManifest.LoginApiUrl;
            return true;
        }
        catch (Exception ex)
        {
            await UIThreadHelper.InvokeAsync(async () =>
            {
                await App.AddNotification($"An exception was thrown while getting server info. Exception: {ex.Message}", true);
                _logger.Error(ex.ToString());
            });
        }
        return false;
    }

    private async Task<ClientManifest?> GetClientManifestAsync()
    {
        try
        {
            var result = await HttpHelper.GetClientManifestAsync(Info.Url).ConfigureAwait(false);
            if (!result.Success || result.ClientManifest is null)
            {
                await UIThreadHelper.InvokeAsync(async () =>
                {
                    await App.AddNotification(result.Error, true);
                    _logger.Error(result.Error);
                });
                return null;
            }
            return result.ClientManifest;
        }
        catch (Exception ex)
        {
            await App.AddNotification($"An exception was thrown while getting client info. Exception: {ex.Message}", true);
            _logger.Error(ex.ToString());
        }
        return null;
    }

    private async Task<bool> VerifyClientFilesAsync(ClientManifest clientManifest)
    {
        _logger.Info("Start - Verify Client Files");
        var filesToDownload = GetFilesToDownloadRecursively(clientManifest.RootFolder).ToList();

        TotalFilesToDownload = filesToDownload.Count;
        FilesDownloaded = 0;

        int success = 1;
        var failedFiles = new List<string>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Settings.Instance.DownloadThreads)
        };

        IsDownloading = true;

        try
        {
            var downloadConfig = new DownloadConfiguration
            {
                RequestConfiguration = { UserAgent = $"{App.GetText("Text.Title")} v{App.CurrentVersion}" },
            };

            if (Settings.Instance.ParallelDownload)
            {
                await Parallel.ForEachAsync(filesToDownload, parallelOptions, async (file, ct) =>
                {
                    try
                    {
                        using var downloadService = new DownloadService(downloadConfig);
                        if (!await DownloadFileAsync(downloadService, file.Path, file.FileName, failedFiles).ConfigureAwait(false))
                        {
                            Interlocked.Exchange(ref success, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error downloading file {file.FileName}: {ex.Message}");
                        lock (failedFiles) failedFiles.Add($"{file.Path}/{file.FileName}");
                    }
                });
            }
            else
            {
                using var downloadService = new DownloadService(downloadConfig);
                foreach (var file in filesToDownload)
                {
                    try
                    {
                        if (!await DownloadFileAsync(downloadService, file.Path, file.FileName, failedFiles).ConfigureAwait(false))
                        {
                            success = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error downloading file {file.FileName}: {ex.Message}");
                        failedFiles.Add($"{file.Path}/{file.FileName}");
                    }
                }
            }
        }
        finally
        {
            IsDownloading = false;
        }

        if (failedFiles.Count > 0)
        {
            var errorFiles = string.Join("\n", failedFiles.Take(10));
            var summary = failedFiles.Count > 10 ? $"...and {failedFiles.Count - 10} more." : "";
            await App.AddNotification($"Failed to download {failedFiles.Count} file(s):\n{errorFiles}\n{summary}", true);
        }

        _logger.Info("End - Verify Client Files");
        return success == 1;
    }

    private async Task<bool> DownloadFileAsync(DownloadService downloadService, string path, string fileName, List<string> failedFiles)
    {
        var downloadFilePath = Path.Combine(path, fileName);

        try
        {
            var clientFileUri = UriHelper.JoinUriPaths(Info.Url, "client", path, fileName);

            var fileDirectory = Path.Combine(Constants.SavePath, Info.SavePath, "Client", path);

            if (!Directory.Exists(fileDirectory))
            Directory.CreateDirectory(fileDirectory);

            var filePath = Path.Combine(fileDirectory, fileName);

            await using var fileStream = await downloadService.DownloadFileTaskAsync(clientFileUri).ConfigureAwait(false);

            if (fileStream is null || fileStream.Length == 0)
            {
                _logger.Error($"Failed to download client file or received empty stream: {downloadFilePath}");
                lock (failedFiles) failedFiles.Add($"{path}/{fileName}");
                return false;
            }

            await using (var writeStream = File.Create(filePath))
            {
            await fileStream.CopyToAsync(writeStream).ConfigureAwait(false);
            await writeStream.FlushAsync().ConfigureAwait(false);
            }

            await UIThreadHelper.InvokeAsync(() =>
            {
                FilesDownloaded++;
                StatusMessage = $"Downloading Client Files... ({FilesDownloaded}/{TotalFilesToDownload})";
                return Task.CompletedTask;
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error downloading: {path}/{fileName}");
            lock (failedFiles) failedFiles.Add($"{path}/{fileName}");
            return false;
        }
    }
    private IEnumerable<(string Path, string FileName)> GetFilesToDownloadRecursively(ClientFolder clientFolder, string path = "")
    {
        foreach (var folder in clientFolder.Folders)
        {
            var folderPath = Path.Combine(path, folder.Name);
            foreach (var fileToDownload in GetFilesToDownloadRecursively(folder, folderPath))
                yield return fileToDownload;
        }

        foreach (var file in clientFolder.Files)
        {
            var fileDirectory = Path.Combine(Constants.SavePath, Info.SavePath, "Client", path);
            var filePath = Path.Combine(fileDirectory, file.Name);

            if (File.Exists(filePath))
            {
                using var readStream = File.OpenRead(filePath);
                if (file.Size == readStream.Length)
                {
                    readStream.Position = 0;
                    var hash = XXHash.Hash64(readStream);
                    if (file.Hash == hash)
                        continue;
                }
            }
            yield return (path, file.Name);
        }
    }

    partial void OnProcessChanged(Process? value)
    {
        _main.UpdateDiscordActivity();
    }
}