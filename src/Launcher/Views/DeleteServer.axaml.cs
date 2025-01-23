using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.Views;

public partial class DeleteServer : UserControl
{
    public DeleteServer()
    {
        InitializeComponent();
    }

    private void DeleteServer_Button_Yes(object sender, RoutedEventArgs e)
    {
        Task task = App.ProcessPopupAsync();
        e.Handled = true;
    }

    private void DeleteServer_Button_No(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}