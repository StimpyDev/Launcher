using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.Models;

public partial class ProgressInfo : ObservableObject
{
    public required string FilePath { get; set; }

    [ObservableProperty]
    private double percentage;
}