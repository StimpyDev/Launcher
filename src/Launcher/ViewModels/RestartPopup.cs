using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Launcher.ViewModels
{
    public partial class RestartPopup : Popup
    {
        public RestartPopup()
        {
            View = new Views.RestartPopup()
            {
                DataContext = this
            };
        }

        [RelayCommand]
        public async Task RestartLauncherAsync()
        {
            const string exeName = "Launcher";
            var launcherPath = Directory.GetCurrentDirectory();

            try
            {
                foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName)))
                {
                    process.Kill();
                }

                await Task.Delay(500);

                var startInfo = new ProcessStartInfo
                {
                    FileName = exeName,
                    WorkingDirectory = launcherPath
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting launcher: {ex.Message}");
            }
        }
    }
}