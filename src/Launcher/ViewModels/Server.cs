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

namespace Launcher.ViewModels
{
    public partial class Server : ObservableObject
    {
        private readonly Main _main = null!;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public string FilesDownloadStatus => $"{FilesDownloaded} / {TotalFilesToDownload} Files Downloaded";

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
        public IBrush? serverStatusFill;

        [ObservableProperty]
        private double fileProgress;

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
            IsRefreshing = true;
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
                }).ConfigureAwait(false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task PlayAsync()
        {
            if (Process != null)
            {
                await App.AddNotification("Unable to launch, the game is already open.", true).ConfigureAwait(false);
                _logger.Warn("Unable to launch, the game is already open.");
                return;
            }

            var clientManifest = await GetClientManifestAsync().ConfigureAwait(false);
            if (clientManifest is null)
                return;

            StatusMessage = App.GetText("Text.Server.VerifyClientFiles");

            if (!await VerifyClientFilesAsync(clientManifest).ConfigureAwait(false))
            {
                await App.AddNotification("Failed to verify client files, please try again", true).ConfigureAwait(false);
                _logger.Warn("Failed to verify client files");
                return;
            }

            if (!IsOnline)
            {
                StatusMessage = string.Empty;
                await App.AddNotification("Cannot login: The server is offline.", true).ConfigureAwait(false);
                return;
            }

            await UIThreadHelper.InvokeAsync(async () =>
            {
                StatusMessage = string.Empty;
                await App.ShowPopupAsync(new Login(this)).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [RelayCommand]
        public async Task OpenClientFolder()
        {
            try
            {
                string folderPath = Path.Combine(Constants.SavePath, Info.SavePath);
                string osPlatform = App.GetOSPlatform();

                switch (osPlatform)
                {
                    case "Windows":
                        Process.Start(new ProcessStartInfo
                        {
                            Verb = "open",
                            UseShellExecute = true,
                            FileName = folderPath
                        });
                        break;
                    case "OSX":
                        Process.Start("open", folderPath);
                        break;
                    case "Linux":
                        Process.Start("xdg-open", folderPath);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
                }
            }
            catch (Exception ex)
            {
                await UIThreadHelper.InvokeAsync(async () =>
                {
                    await App.AddNotification($"An exception was thrown while opening the client folder. Execption: {ex}", true).ConfigureAwait(false);
                    _logger.Error(ex.ToString());
                }).ConfigureAwait(false);
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
                        await App.AddNotification(result.Error, true).ConfigureAwait(false);
                        _logger.Error(result.Error);
                    }).ConfigureAwait(false);
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
                    await App.AddNotification($"An exception was thrown while getting server info. Exception: {ex}", true).ConfigureAwait(false);
                    _logger.Error(ex.ToString());
                }).ConfigureAwait(false);
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
                        await App.AddNotification(result.Error, true).ConfigureAwait(false);
                        _logger.Error(result.Error);
                    }).ConfigureAwait(false);
                    return null;
                }
                return result.ClientManifest;
            }
            catch (Exception ex)
            {
                await UIThreadHelper.InvokeAsync(async () =>
                {
                    await App.AddNotification($"An exception was thrown while getting client info. Exception: {ex}", true).ConfigureAwait(false);
                    _logger.Error(ex.ToString());
                }).ConfigureAwait(false);
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
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount * 2),
            };

            if (Settings.Instance.ParallelDownload)
            {
                await Parallel.ForEachAsync(filesToDownload, parallelOptions, async (file, ct) =>
                {
                    try
                    {
                        if (!await DownloadFileAsync(file.Path, file.FileName).ConfigureAwait(false))
                        {
                            Interlocked.Exchange(ref success, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error downloading file {file.FileName}: {ex.Message}");
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var file in filesToDownload)
                {
                    try
                    {
                        if (!await DownloadFileAsync(file.Path, file.FileName).ConfigureAwait(false))
                        {
                            success = 0;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error downloading file {file.FileName}: {ex.Message}");
                    }
                }
            }
            _logger.Info("End - Verify Client Files");
            return success == 1;
        }

        private async Task<bool> DownloadFileAsync(string path, string fileName)
        {
            var downloadFilePath = Path.Combine(path, fileName);
            IsDownloading = true;

            try
            {
                var clientFileUri = UriHelper.JoinUriPaths(Info.Url, "client", path, fileName);

                using var downloadService = new DownloadService(new DownloadConfiguration
                {
                    RequestConfiguration = { UserAgent = $"{App.GetText("Text.Title")} v{App.CurrentVersion}" }
                });

                downloadService.DownloadStarted += (s, e) =>
                {
                    StatusMessage = App.GetText("Text.Server.DownloadingFile", Path.Combine(path, fileName));
                };

                var fileDirectory = Path.Combine(Constants.SavePath, Info.SavePath, "Client", path);
                var filePath = Path.Combine(fileDirectory, fileName);

                if (!Directory.Exists(fileDirectory))
                    Directory.CreateDirectory(fileDirectory);

                await using var fileStream = await downloadService.DownloadFileTaskAsync(clientFileUri).ConfigureAwait(false);
                if (fileStream is null)
                {
                    await UIThreadHelper.InvokeAsync(async () =>
                    {
                        await App.AddNotification($"Failed to get client file: {downloadFilePath}", true).ConfigureAwait(false);
                        _logger.Error("Failed to get client file {path} {filename}", path, fileName);
                    }).ConfigureAwait(false);
                    return false;
                }

                await using var writeStream = new FileStream(filePath, new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    Options = FileOptions.SequentialScan
                });

                await fileStream.CopyToAsync(writeStream).ConfigureAwait(false);
                await writeStream.FlushAsync().ConfigureAwait(false);

                await UIThreadHelper.InvokeAsync(() =>
                {
                    FilesDownloaded++;
                    OnPropertyChanged(nameof(FilesDownloadStatus));
                    return Task.CompletedTask;
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error downloading {path} {filename}", path, fileName);
                return false;
            }
            finally
            {
                IsDownloading = false;
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
                    using var readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
            _main?.UpdateDiscordActivity();
        }
    }
}