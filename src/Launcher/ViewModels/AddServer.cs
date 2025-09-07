using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Extensions;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Launcher.ViewModels;

public partial class AddServer : Popup
{
    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AddServer), nameof(ValidateServerUrl))]
    private string serverUrl = string.Empty;

    public IAsyncRelayCommand AddServerCommand { get; }
    public ICommand CancelAddServerCommand { get; }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public AddServer()
    {
        AddServerCommand = new AsyncRelayCommand(OnAddServer);
        CancelAddServerCommand = new RelayCommand(OnAddServerCancel);

        View = new Views.AddServer()
        {
            DataContext = this
        };
    }

    private async Task OnAddServer()
    {
        await App.ProcessPopupAsync();
    }
    private void OnAddServerCancel()
    {
        App.CancelPopup();
    }

    public static ValidationResult? ValidateServerUrl(string serverUrl, ValidationContext context)
    {
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverUri))
            return new ValidationResult(App.GetText("Text.Add_Server.InvalidServerUrl1", serverUrl));

        if (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps)
            return new ValidationResult(App.GetText("Text.Add_Server.InvalidServerUrl2", serverUrl));

        return ValidationResult.Success;
    }

    public override Task<bool> ProcessAsync()
    {
        ProgressDescription = App.GetText("Text.Add_Server.Loading");

        return Task.Run(OnAddServerAsync);
    }

    private async Task<bool> OnAddServerAsync()
    {
        try
        {
            ServerUrl = ServerUrl.Trim();

            var result = await HttpHelper.GetServerManifestAsync(ServerUrl).ConfigureAwait(false);

            if (!result.Success || result.ServerManifest is null)
            {
                await App.AddNotification(result.Error, true);
                return false;
            }

            var serverManifest = result.ServerManifest;

            if (string.IsNullOrEmpty(serverManifest.Name))
            {
                await App.AddNotification("Server name is missing in manifest.", true);
                return false;
            }

            if (!TryCreateSavePath(serverManifest.Name, out var savePath))
            {
                await App.AddNotification("Failed to create a save path for server.", true);
                return false;
            }

            var serverInfo = new ServerInfo
            {
                Url = ServerUrl,
                Name = serverManifest.Name,
                Description = serverManifest.Description,
                LoginServer = serverManifest.LoginServer,
                LoginApiUrl = serverManifest.LoginApiUrl,
                SavePath = savePath
            };

            Settings.Instance.ServerInfoList.Add(serverInfo);
            Settings.Save();

            return true;
        }
        catch (Exception ex)
        {
            await App.AddNotification($"An exception occurred: {ex.Message}", true);

            _logger.Error(ex);

            return false;
        }
    }

    private bool TryCreateSavePath(string name, out string path)
    {
        path = string.Empty;
        try
        {
            var validName = name.ToValidDirectoryName();

            var current = validName;
            int i = 1;

            var basePath = Path.Combine(Constants.SavePath, Constants.ServersDirectory);
            Directory.CreateDirectory(basePath);

            string candidatePath;

            do
            {
                candidatePath = Path.Combine(basePath, current);
                if (!Directory.Exists(candidatePath))
                {
                    Directory.CreateDirectory(candidatePath);
                    path = candidatePath;
                    return true;
                }
                current = $"{validName}_{i++}";
            } while (true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create save path");
            return false;
        }
    }
}