using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Helpers;
using Launcher.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Launcher.ViewModels;

public partial class Login : Popup
{
    private readonly Server _server;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private string? warning;

    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string username = string.Empty;

    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string password = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool rememberUsername;

    [ObservableProperty]
    private bool rememberPassword;

    public bool AutoFocusUsername => string.IsNullOrEmpty(Username);
    public bool AutoFocusPassword => !string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password);

    public IAsyncRelayCommand LoginCommand { get; }
    public ICommand LoginCancelCommand { get; }

    public Login(Server server)
    {
        _server = server;

        AddSecureWarning();

        RememberUsername = _server.Info.RememberUsername;
        RememberPassword = _server.Info.RememberPassword;
        Username = RememberUsername ? _server.Info.Username ?? string.Empty : string.Empty;
        Password = RememberPassword ? _server.Info.Password ?? string.Empty : string.Empty;

        LoginCommand = new AsyncRelayCommand(OnLogin);
        LoginCancelCommand = new RelayCommand(OnLoginCancel);

        View = new Views.Login { DataContext = this };
    }

    private async Task OnLogin()
    {
        await App.ProcessPopupAsync();
    }

    private void OnLoginCancel()
    {
        App.CancelPopup();
    }

    private void AddSecureWarning()
    {
        if (Uri.TryCreate(_server.Info.LoginApiUrl, UriKind.Absolute, out var loginApiUrl)
            && loginApiUrl.Scheme != Uri.UriSchemeHttps)
        {
            Warning = App.GetText("Text.Login.SecureApiWarning");
        }
    }

    partial void OnRememberUsernameChanged(bool value)
    {
        _server.Info.RememberUsername = value;
        if (!value)
            _server.Info.Username = null;

        Settings.Instance.Save();
    }

    partial void OnRememberPasswordChanged(bool value)
    {
        _server.Info.RememberPassword = value;
        if (!value)
            _server.Info.Password = null;

        Settings.Instance.Save();
    }

    public override async Task<bool> ProcessAsync()
    {
        SaveRememberedCredentials();

        try
        {
            using var httpClient = HttpHelper.CreateHttpClient();
            var loginRequest = new LoginRequest { Username = Username, Password = Password };
            ProgressDescription = App.GetText("Text.Login.Loading");

            var httpResponse = await httpClient.PostAsJsonAsync(_server.Info.LoginApiUrl, loginRequest).ConfigureAwait(false);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
                return false;
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                await App.AddNotification($"Failed to login. Http Error: {httpResponse.ReasonPhrase}", true).ConfigureAwait(false);
                return false;
            }

            var loginResponse = await httpResponse.Content.ReadFromJsonAsync<LoginResponse>().ConfigureAwait(false);
            if (string.IsNullOrEmpty(loginResponse?.SessionId))
            {
                await App.AddNotification("Invalid login API response.", true).ConfigureAwait(false);
                Password = string.Empty;
                return false;
            }

            await LaunchClientAsync(loginResponse.SessionId, loginResponse.LaunchArguments).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await App.AddNotification($"An exception was thrown while logging in {ex}", true).ConfigureAwait(false);
            _logger.Error(ex);
            return false;
        }
    }

    private void SaveRememberedCredentials()
    {
        if (RememberUsername)
        {
            _server.Info.Username = Username;
        }
        if (RememberPassword)
        {
            _server.Info.Password = Password;
        }
        Settings.Instance.Save();
    }

    private async Task HandleUnauthorizedAsync()
    {
        await App.AddNotification(App.GetText("Text.Login.Unauthorized"), true).ConfigureAwait(false);
        Password = string.Empty;
    }

    private async Task LaunchClientAsync(string sessionId, string? serverArguments)
    {
        if (!D3D9.IsAvailable())
        {
            await NotifyDirectX9MissingAsync().ConfigureAwait(false);
            return;
        }

        var launcherArguments = new List<string>
        {
            $"Server={_server.Info.LoginServer}",
            $"SessionId={sessionId}",
            $"Internationalization:Locale={Settings.Instance.Locale}"
        };

        if (!string.IsNullOrEmpty(serverArguments))
            launcherArguments.Add(serverArguments);

        var arguments = string.Join(' ', launcherArguments);
        var workingDirectory = Path.Combine(Constants.SavePath, _server.Info.SavePath, "Client");
        var executablePath = Path.Combine(workingDirectory, Constants.ClientExecutableName);

        if (!File.Exists(executablePath))
        {
            await App.AddNotification($"Client executable not found: {executablePath}", true).ConfigureAwait(false);
            return;
        }

        _server.Process = new Process();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _server.Process.StartInfo.FileName = "wine";
            _server.Process.StartInfo.Arguments = $"{Constants.ClientExecutableName} {arguments}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _server.Process.StartInfo.FileName = Constants.ClientExecutableName;
            _server.Process.StartInfo.Arguments = arguments;
        }

        _server.Process.StartInfo.UseShellExecute = true;
        _server.Process.StartInfo.WorkingDirectory = workingDirectory;
        _server.Process.EnableRaisingEvents = true;

        _server.Process.Exited += _server.ClientProcessExited;

        try
        {
            _server.Process.Start();
        }
        catch (Exception ex)
        {
            await App.AddNotification($"Failed to start the client: {ex.Message}", true).ConfigureAwait(false);
            _logger.Error(ex);
        }
    }
    private async Task NotifyDirectX9MissingAsync()
    {
        await App.AddNotification("DirectX 9 is not available. Cannot launch the client.", true).ConfigureAwait(false);
        await Task.Delay(500).ConfigureAwait(false);

        string url = Constants.DirectXDownloadUrl;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    Verb = "open",
                    UseShellExecute = true,
                    FileName = url
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            await App.AddNotification("Failed to open the DirectX download page. Please open this URL manually: " + url, true).ConfigureAwait(false);
        }
    }
}