using CommunityToolkit.Mvvm.ComponentModel;

using NuGet.Versioning;

namespace Launcher.ViewModels;

public partial class Splash : ObservableObject
{
    [ObservableProperty]
    private string? message;

    [ObservableProperty]
    private SemanticVersion version = App.CurrentVersion;

    public Splash()
    {
#if DESIGNMODE
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            Message = "Test message";
        }
#endif
    }
}