using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.Views;

public partial class Login : UserControl
{
    public Login()
    {
        InitializeComponent();
    }

    private void Login_Button(object sender, RoutedEventArgs e)
    {
        App.ProcessPopup();
        e.Handled = true;
    }

    private void Login_Button_Cancel(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}