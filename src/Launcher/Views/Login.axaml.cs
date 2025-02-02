using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Launcher.Views;

public partial class Login : UserControl
{
    public Login()
    {
        InitializeComponent();
    }

    private static void Login_Button(object sender, RoutedEventArgs e)
    {
        Task task = App.ProcessPopupAsync();
        e.Handled = true;
    }

    private static void Login_Button_Cancel(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}