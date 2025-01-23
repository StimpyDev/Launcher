using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.Views;

public partial class AddServer : UserControl
{
    public AddServer()
    {
        InitializeComponent();
    }

    private void AddServer_Button_Add(object sender, RoutedEventArgs e)
    {
        Task task = App.ProcessPopupAsync();
        e.Handled = true;
    }

    private void AddServer_Button_Cancel(object sender, RoutedEventArgs e)
    {
        App.CancelPopup();
        e.Handled = true;
    }
}