
using Microsoft.Extensions.Configuration;

public static class Config
{
    private static readonly IConfiguration config;
    
    // this is the path to download the installers to (default is user downloads folder + OneDriveUpdaterVersions)
    public static string DownloadPath { get; set; }

    // this is the path to the json file that will hold all the version information (as of now its just besides the exe)
    public static string VersionFile { get; set; }

    // this is the path to the log file (default is beside the exe)
    public static string LogFile { get; set; }

    // this is used to switch between architectures to download and install
    // x86, x64, arm64, Both
    public static string Architecture { get; set; }

    // this will represent the max sub version or hidden version search to do when looking for new versions
    // Ex : 10 means from 23.3432.1232.0001 to 23.3432.1232.0010 will be searched
    public static int MaxSubVersionCheck { get; set; }

    // this will represent the last year to be included when searching for new versions
    // Ex : 23 will include the search for new versions the year 2023 or technically from any version starts with 23.xxxx.xxxx.xxxx
    public static int LastYearToBEIncluded { get; set; }
    static Config()
    {
        // to grap the json file configs
        var appSettingsGrapper = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("config.json", optional: false, reloadOnChange: true);

        config = appSettingsGrapper.Build();

        //initialize them 
        // data will be imported from the config.json file that will reside besids the exe
        DownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\" + "Downloads" + "\\" + "OneDriveUpdaterVersions";
        VersionFile = config["AppSettings:VersionFile"];
        LogFile = config["AppSettings:LogFile"];
        Architecture = config["AppSettings:Architecture"];
        MaxSubVersionCheck = int.Parse(config["AppSettings:MaxSubVersionCheck"]);
        LastYearToBEIncluded = int.Parse(config["AppSettings:LastYearToBEIncluded"]);

    }
}