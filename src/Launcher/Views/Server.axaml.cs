using Avalonia.Controls;

namespace Launcher.Views;

public partial class Server : UserControl
{
    public Server()
    {
        InitializeComponent();
    }

    protected override async void OnDataContextBeginUpdate()
    {
        if (DataContext is not ViewModels.Server server)
            return;

        var success = await server.OnShowAsync();

        if (!success)
            App.ClearServerSelection();
    }
}