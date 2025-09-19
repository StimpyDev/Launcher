﻿using System;
using System.IO;

namespace Launcher.Helpers;

public static class Constants
{
    public const string LogFile = "Launcher.log";
    public const string SettingsFile = "Settings.xml";

    public const string SaveDirectory = "OSFRLauncher";
    public const string ServersDirectory = "Servers";

    public const string ClientExecutableName = "FreeRealms.exe";
    public const string DirectXDownloadUrl = "https://www.microsoft.com/en-us/download/details.aspx?id=8109";


    public static readonly string LogsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

    public static readonly string SavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                SaveDirectory);
}