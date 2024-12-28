using System;
using System.IO;

namespace Launcher.Helpers;

public static class Constants
{
    public const string SettingsFile = "Launcher.xml";

    public const string SaveDirectory = "OSFRLauncher";
    public const string ServersDirectory = "Servers";
    
    public static readonly string SavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                SaveDirectory);
}