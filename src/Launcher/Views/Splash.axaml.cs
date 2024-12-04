using Avalonia.Controls;

namespace Launcher.Views;

public partial class Splash : Window
{
    public readonly ViewModels.Splash ViewModel = new ViewModels.Splash();

    public Splash()
    {
        DataContext = ViewModel;

        InitializeComponent();
    }
}