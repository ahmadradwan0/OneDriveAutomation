/// <summary>
/// Configuration class to hold application settings
/// This class contains static properties that can be adjusted to configure the behavior of the application.
/// Changing these setings wil not break any thing in the application and you only need to change it here to apply the changes
/// </summary>
public static class Config
{
    // this is the path to the json file that will hold all the version information (as of now its just besides the exe)
    public static string VersionFile { get; set; } = "AllVersions.json";

    // this is the path to download the installers to (default is user downloads folder + OneDriveUpdaterVersions)
    public static string DownloadPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\" + "Downloads" + "\\" + "OneDriveUpdaterVersions";

    // this is the path to the log file (default is beside the exe)
    public static string LogFile { get; set; } = "onedrive_updater.log";

    // this is used to switch between architectures to download and install
    public static string Architecture { get; set; } = "Both"; // x86, x64, arm64, Both

    // this will represent the max sub version or hidden version search to do when looking for new versions
    // Ex : 10 means from 23.3432.1232.0001 to 23.3432.1232.0010 will be searched 
    public static int MaxSubVersionCheck { get; set; } = 10;

    // this will represent the last year to be included when searching for new versions
    // Ex : 23 will include the search for new versions the year 2023 or technically from any version starts with 23.xxxx.xxxx.xxxx 
    public static int LastYearToBEIncluded { get; set; } = 25;
}
