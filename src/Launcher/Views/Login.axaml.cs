using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.Views;

public partial class Login : UserControl
{
    public Login()
    {
        InitializeComponent();
    }

    private static async void Login_Button(object sender, RoutedEventArgs e)
    {
        await App.ProcessPopupAsync();
        e.Handled = true;
    }

    private static void Login_Button_Cancel(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}