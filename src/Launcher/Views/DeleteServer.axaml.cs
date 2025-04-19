using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.Views;

public partial class DeleteServer : UserControl
{
    public DeleteServer()
    {
        InitializeComponent();
    }

    private static async void DeleteServer_Button_Yes(object sender, RoutedEventArgs e)
    {
        await App.ProcessPopupAsync();
        e.Handled = true;
    }

    private static void DeleteServer_Button_No(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}