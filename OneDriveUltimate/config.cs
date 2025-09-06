
public static class Config
{
    public static string VersionFile { get; set; } = "AllVersions.json";
    public static string DownloadPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\" + "Downloads" + "\\" + "OneDriveUpdaterVersions";
    public static string LogFile { get; set; } = "onedrive_updater.log";
    public static int CheckIntervalHours { get; set; } = 2;
    // public static bool AutoInstall { get; set; } = true;
    // public static string PreferredRing { get; set; } = "Production"; // Production, Insider, Deferred
    public static string Architecture { get; set; } = "Both"; // x86, x64, arm64, Both
    public static int MaxSubVersionCheck { get; set; } = 10;
    public static int LastYearToBEIncluded { get; set; } = 25;
}
