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
        public static async Task RestartLauncher()
        {
            const string exeName = "Launcher.exe";
            var launcherPath = Directory.GetCurrentDirectory();
       

            var procces = new Process();
            await Task.Delay(500);

            procces.StartInfo.FileName = exeName;
            procces.StartInfo.WorkingDirectory = launcherPath;
            procces.Start();

            foreach (Process process in Process.GetProcessesByName("Launcher"))
            {
                process.Kill();
            }
        }
    }
}
