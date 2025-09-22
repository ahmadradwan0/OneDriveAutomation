/// <summary>
/// This class acts as a central coordinator or "wrapper" for the application's core logic.
/// </summary>
/// <remarks>
/// The Initializer class serves as a middleman for multi step workflows and if more logic needed
/// It calls methods from other specialized classes (like WebScraper, StorageManager, DownloadManager, and InstallationManager)
/// This design simplifies the main application logic and improves code readability and to have more control over the output.
/// </remarks>
public class Initializer
{

    /// <summary>
    /// a methods calling the (ScrapeHtmlAsync) method from (WebScraper) with a year variable and will return a list of versions from website only
    /// in future if needed to change or call another methods from webscraper it can be done here and no need to change anywhere else 
    /// </summary>
    public async static Task<List<VersionInfo>> InitWebScrapping()
    {
        // Step 1: Scrape versions from web
        var webVersions = await WebScraper.ScrapeHtmlAsync("https://hansbrender.com/all-onedrive-versions-windows/", Config.LastYearToBEIncluded);

        return webVersions;
    }

    /// <summary>
    /// calling (SaveVersionsOverwrite) that will save and overwright data in json file from (StorageManager) class revices it as a list to be saved .
    /// </summary>
    public static void SaveToJsonFile(List<VersionInfo> AllVersions)
    {
        StorageManager.SaveVersionsOverwrite(AllVersions);
    }

    /// <summary>
    /// a method to get the versions stored in local json file (GetStoredVersions) from (StorageManager) class returns it as a list of versioninfo type 
    /// </summary>
    public static List<VersionInfo> LocalStorageScrapping()
    {
        //StorageManager.SaveVersionsOverwrite(webVersions);
        return StorageManager.GetStoredVersions(Config.VersionFile);
    }

    /// <summary>
    /// a method to get all the hidden versions in betwen the version that they were found from the website 
    /// </summary>
    public async static Task<List<VersionInfo>> HiddenInitWebScrapping(List<VersionInfo> AllVersions)
    {
        List<VersionInfo>? NewHiddenVersions = await WebScraper.GetListOfHiddenVersionsParallel(AllVersions);
        return NewHiddenVersions;
    }

    /// <summary>
    /// a methosd will compare 2 lists by item and it will return only the new items not found in theg otehr list
    /// </summary>
    public static List<VersionInfo> CompareAndGetNewVersions(List<VersionInfo> onlineVersoins, List<VersionInfo> localVersions)
    {
        return StorageManager.CompareByItem(onlineVersoins, localVersions);
    }

    /// <summary>
    /// Task to return a combined list of new and hidden versions to be used in downloading and installing new versions
    /// </summary>
    public static List<VersionInfo> GetCombinedWebAndHiddenVersions(List<VersionInfo> newItems, List<VersionInfo> hiddenItems)
    {
        var combinedList = new List<VersionInfo>();
        if (newItems.Count > 0 || hiddenItems.Count > 0)
        {
            combinedList = StorageManager.CombineTwoLists(hiddenItems, newItems);
        }
        return combinedList;
    }


    /// <summary>
    /// Task to download new and hidden versions and save them to the download folder
    /// </summary>
    public async static Task<List<VersionInfo>> DownloadAllNewversions(List<VersionInfo> NewItems)
    {
        var downloadedVersionsList = await DownloadManager.DownloadNewVersions(NewItems);
        return downloadedVersionsList;
    }

    /// <summary>
    /// Task to install the new downloaded versions ... 
    /// </summary>
    public async static Task<List<VersionInfo>> InstallNewDownloadedVersions(List<VersionInfo> installerPaths, List<VersionInfo> LocalJsonList)
    {
        var ListOfInstalledVersions = await InstallationManager.InstallAndUninstallVersions(installerPaths, LocalJsonList);
        return ListOfInstalledVersions;
    }

    /// <summary>
    /// Task to add new versions and hidden versions to the local json file
    /// </summary>
    public static void AddNewVersionsToJsonFile(List<VersionInfo> NewItemsList, List<VersionInfo> JsonList)
    {
        if (NewItemsList.Count > 0)
        {

            var combinedList = StorageManager.CombineTwoLists(NewItemsList, JsonList);

            StorageManager.SaveVersionsOverwrite(combinedList);
            Utils.Log("JSON File Updated With New versions ");
        }
        else
        {
            Utils.Log("No new items found. Exiting...");
            return;
        }
    }

    /// <summary>
    /// Delete all files thats has been downloaded 
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            // it will check if the directory exists before trying to delete it to avoid exceptions
            // Config.DownloadPath is the path to the download directory specified in the config class 
            if (Directory.Exists(Config.DownloadPath))
            {
                Directory.Delete(Config.DownloadPath, true);
                Utils.Log("Deleted download directory and its contents.");
            }
            else
            {
                Utils.Log("Download directory does not exist, nothing to clean up.");
            }
        }
        catch (Exception ex)
        {
            Utils.Log($"Error during cleanup: {ex.Message}", "ERROR");
        }
    }
    
    public async static Task<List<VersionInfo>> EperimentalHiddenVersionsCheck(VersionInfo LastVersionReleased)
    {
        List<VersionInfo>? NewEperimentalHiddenVersions = await WebScraper.ConstructNewVersionsOnTheFlyParallel(LastVersionReleased);
        return NewEperimentalHiddenVersions;
    } 



}