/// <summary>
/// This class acts as a central coordinator or "wrapper" for the application's core logic.
/// </summary>
/// <remarks>
/// The Initializer class serves as a middleman, orchestrating complex, multi-step workflows.
/// It calls methods from other specialized classes (like WebScraper, StorageManager, DownloadManager, and InstallationManager)
/// in a specific sequence to perform tasks such as version discovery, downloading, and installation.
/// This design simplifies the main application logic and improves code maintainability.
/// </remarks>
public class Initializer
{
    public async static Task<List<VersionInfo>> InitWebScrapping()
    {
        // Step 1: Scrape versions from web
        var webVersions = await WebScraper.ScrapeHtmlAsync("https://hansbrender.com/all-onedrive-versions-windows/", Config.LastYearToBEIncluded);

        return webVersions;
    }

    public static void SaveToJsonFile(List<VersionInfo> AllVersions)
    {
        StorageManager.SaveVersionsOverwrite(AllVersions);
    }

    public static List<VersionInfo> LocalStorageScrapping()
    {
        //StorageManager.SaveVersionsOverwrite(webVersions);
        return StorageManager.GetStoredVersions(Config.VersionFile);
    }

    public async static Task<List<VersionInfo>> HiddenInitWebScrapping(List<VersionInfo> AllVersions)
    {
        List<VersionInfo>? NewHiddenVersions = await WebScraper.GetListOfHiddenVersionsParallel(AllVersions);
        return NewHiddenVersions;
    }

    public static List<VersionInfo> CompareAndGetNewVersions(List<VersionInfo> onlineVersoins, List<VersionInfo> localVersions)
    {
        return StorageManager.CompareByItem(onlineVersoins, localVersions);
    }

    //Task to return a combined list of new and hidden versions to be used in downloading and installing new versions
    public static List<VersionInfo> GetCombinedWebAndHiddenVersions(List<VersionInfo> newItems, List<VersionInfo> hiddenItems)
    {
        var combinedList = new List<VersionInfo>();
        if (newItems.Count > 0 || hiddenItems.Count > 0)
        {
            combinedList = StorageManager.CombineTwoLists(hiddenItems, newItems);
        }
        return combinedList;
    }


    //Task to download new and hidden versions and save them to the download folder
    public async static Task<List<VersionInfo>> DownloadAllNewversions(List<VersionInfo> NewItems)
    {
        var downloadedVersionsList = await DownloadManager.DownloadNewVersions(NewItems);
        return downloadedVersionsList;
    }

    //Task to install the new downloaded versions ... 
    public async static Task<List<VersionInfo>> InstallNewDownloadedVersions(List<VersionInfo> installerPaths)
    {
        var ListOfInstalledVersions = await InstallationManager.InstallAndUninstallVersions(installerPaths);
        return ListOfInstalledVersions;
    }

    //Task to add new versions and hidden versions to the local json file
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

    //Cleanup and exit TO:
    //Delete all files thats has been downloaded 
    public static void Cleanup()
    {
        try
        {
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



}