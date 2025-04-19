using CommunityToolkit.Mvvm.Input;
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
        public void RestartLauncher()
        {
            Path.Combine("Appdata", "Local", "OSFR Launcher", "current");

            Task.Delay(1000);

            Process.Start("Launcher.exe");

            foreach (Process process in Process.GetProcessesByName("Launcher"))
            {
                process.Kill();
            }
        }
    }
}
