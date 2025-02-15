using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Launcher.Views;

public partial class DeleteServer : UserControl
{
    public DeleteServer()
    {
        InitializeComponent();
    }

    private static void DeleteServer_Button_Yes(object sender, RoutedEventArgs e)
    {
        Task task = App.ProcessPopupAsync();
        e.Handled = true;
    }

    private static void DeleteServer_Button_No(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}