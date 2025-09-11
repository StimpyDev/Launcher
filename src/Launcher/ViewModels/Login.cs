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

        if (RememberUsername && !string.IsNullOrEmpty(_server.Info.Username))
        {
            Username = _server.Info.Username;
        }

        if (RememberPassword && !string.IsNullOrEmpty(_server.Info.Password))
        {
            Password = _server.Info.Password;
        }

        LoginCommand = new AsyncRelayCommand(OnLogin);
        LoginCancelCommand = new RelayCommand(OnLoginCancel);

        View = new Views.Login()
        {
            DataContext = this
        };
    }

    private async Task OnLogin()
    {
        await App.ProcessPopupAsync().ConfigureAwait(false);
    }

    private void OnLoginCancel()
    {
        App.CancelPopup();
    }

    private void AddSecureWarning()
    {
        if (Uri.TryCreate(_server.Info.LoginApiUrl, UriKind.Absolute, out var loginApiUrl))
        {
            if (loginApiUrl.Scheme != Uri.UriSchemeHttps)
            {
                Warning = App.GetText("Text.Login.SecureApiWarning");
            }
        }
    }

    partial void OnRememberUsernameChanged(bool value)
    {
        _server.Info.RememberUsername = value;
        if (!value)
        {
            _server.Info.Username = null;
        }
        SaveSettings();
    }

    partial void OnRememberPasswordChanged(bool value)
    {
        _server.Info.RememberPassword = value;
        if (!value)
        {
            _server.Info.Password = null;
        }
        SaveSettings();
    }

    private static void SaveSettings() => Settings.Save();

    public override async Task<bool> ProcessAsync()
    {
        if (RememberUsername)
        {
            _server.Info.Username = Username;
            SaveSettings();
        }

        if (RememberPassword)
        {
            _server.Info.Password = Password;
            SaveSettings();
        }

        try
        {
            using var httpClient = HttpHelper.CreateHttpClient();

            var loginRequest = new LoginRequest
            {
                Username = Username,
                Password = Password
            };

            ProgressDescription = App.GetText("Text.Login.Loading");

            var httpResponse = await httpClient.PostAsJsonAsync(_server.Info.LoginApiUrl, loginRequest).ConfigureAwait(false);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                await App.AddNotification(App.GetText("Text.Login.Unauthorized"), true).ConfigureAwait(false);
                Password = string.Empty;
                return false;
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                await App.AddNotification($"""
                    Failed to login. Http Error: {httpResponse.ReasonPhrase}
                    """, true).ConfigureAwait(false);
                return false;
            }

            var loginResponse = await httpResponse.Content.ReadFromJsonAsync<LoginResponse>().ConfigureAwait(false);
            if (string.IsNullOrEmpty(loginResponse?.SessionId))
            {
                await App.AddNotification("Invalid login api response.", true).ConfigureAwait(false);
                Password = string.Empty;
                return false;
            }

            await LaunchClientAsync(loginResponse.SessionId, loginResponse.LaunchArguments).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await App.AddNotification($"""
                An exception was thrown while logging in.
                Exception: {ex}
                """, true).ConfigureAwait(false);
            _logger.Error(ex.ToString());
            return false;
        }
    }

    private async Task LaunchClientAsync(string sessionId, string? serverArguments)
    {
        bool dx9Available = D3D9.IsAvailable();

        // Checks if dx9 is installed on the system.
        if (!dx9Available)
        {
            await App.AddNotification("DirectX 9 is not available. Cannot launch the client.", true);
            await Task.Delay(500);
            Process.Start(new ProcessStartInfo()
            {
                Verb = "open",
                UseShellExecute = true,
                FileName = "https://www.microsoft.com/en-us/download/details.aspx?id=8109"
            });
            return;
        }

        const string FileName = "FreeRealms.exe";

        var launcherArguments = new List<string>
        {
            $"Server={_server.Info.LoginServer}",
            $"SessionId={sessionId}",
            $"Internationalization:Locale={Settings.Instance.Locale}"
        };

        if (!string.IsNullOrEmpty(serverArguments))
        {
            launcherArguments.Add(serverArguments);
        }

        var arguments = string.Join(' ', launcherArguments);
        var workingDirectory = Path.Combine(Constants.SavePath, _server.Info.SavePath, "Client");

        var executablePath = Path.Combine(workingDirectory, FileName);
        if (!File.Exists(executablePath))
        {
            await App.AddNotification($"Client executable not found: {executablePath}", true);
            return;
        }

        _server.Process = new Process();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _server.Process.StartInfo.FileName = "wine";
            _server.Process.StartInfo.Arguments = $"{FileName} {arguments}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _server.Process.StartInfo.FileName = FileName;
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
            await App.AddNotification($"Failed to start the client: {ex.Message}", true);
            _logger.Error(ex.ToString());
        }
    }
}