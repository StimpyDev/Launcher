using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Extensions;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Launcher.ViewModels;

public partial class AddServer : Popup
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AddServer), nameof(ValidateServerUrl))]
    private string serverUrl = string.Empty;

    public IAsyncRelayCommand AddServerCommand { get; }
    public ICommand CancelAddServerCommand { get; }

    public AddServer()
    {
        AddServerCommand = new AsyncRelayCommand(OnAddServer);
        CancelAddServerCommand = new RelayCommand(OnAddServerCancel);

        View = new Views.AddServer
        {
            DataContext = this
        };
    }

    private async Task OnAddServer()
    {
        await App.ProcessPopupAsync().ConfigureAwait(false);
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

    public override async Task<bool> ProcessAsync()
    {
        ProgressDescription = App.GetText("Text.Add_Server.Loading");
        return await OnAddServerAsync().ConfigureAwait(false);
    }

    private async Task<bool> OnAddServerAsync()
    {
        try
        {
            ServerUrl = ServerUrl.Trim();

            var result = await HttpHelper.GetServerManifestAsync(ServerUrl).ConfigureAwait(false);

            if (!result.Success || result.ServerManifest is null)
            {
                await App.AddNotification(result.Error, true).ConfigureAwait(false);
                return false;
            }

            var serverManifest = result.ServerManifest;

            if (string.IsNullOrEmpty(serverManifest.Name))
                return await NotifyAndReturnFalse("Server name is missing in manifest.");

            if (!TryCreateSavePath(serverManifest.Name, out var savePath))
                return await NotifyAndReturnFalse("Failed to create a save path for server.");

            if (Settings.Instance.ServerInfoList.Any(s =>
                string.Equals(s.Name, serverManifest.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return await NotifyAndReturnFalse(
                    App.GetText("Text.Add_Server.ServerAlreadyExists", ServerUrl));
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
            _logger.Error(ex);
            await App.AddNotification($"An exception occurred: {ex.Message}", true).ConfigureAwait(false);
            return false;
        }
    }

    private static async Task<bool> NotifyAndReturnFalse(string message)
    {
        await App.AddNotification(message, true).ConfigureAwait(false);
        return false;
    }

    private static bool TryCreateSavePath(string name, out string path)
    {
        path = string.Empty;
        try
        {
            var validName = name.ToValidDirectoryName();
            var basePath = Path.Combine(Constants.SavePath, Constants.ServersDirectory);
            Directory.CreateDirectory(basePath);

            int counter = 1;
            string currentName = validName;

            while (true)
            {
                var candidatePath = Path.Combine(basePath, currentName);
                if (!Directory.Exists(candidatePath))
                {
                    Directory.CreateDirectory(candidatePath);
                    path = candidatePath;
                    return true;
                }
                currentName = $"{validName}_{counter++}";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create save path");
            return false;
        }
    }
}