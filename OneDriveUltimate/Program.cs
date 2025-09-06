// See https://aka.ms/new-console-template for more information

class Program
{
    

    /// <summary>
    /// This method executes the full end-to-end production workflow to discover, download, and install new versions of the application.
    /// </summary>
    /// <remarks>
    /// This is the primary function for the automated version management pipeline. It performs a sequential, multi-step process to ensure a complete and reliable operation:
    /// <br/>
    /// 1.  **Initial Cleanup:** Clears the environment of any leftover files from previous sessions to ensure a clean slate.
    /// 2.  **Version Scrapping:** Retrieves all existing versions from the local JSON file.
    /// 3.  **Public Discovery:** Scrapes and loads all publicly listed versions from the website's table.
    /// 4.  **Hidden Discovery:** Searches for any hidden or unlisted subversions by checking the publicly found versions.
    /// 5.  **List Consolidation:** Combines the publicly discovered versions with the hidden versions, ensuring no duplicates exist in the final list of new versions.
    /// 6.  **Comparison:** Compares the consolidated list of new versions against the local list to identify only the ones that have not yet been processed.
    /// 7.  **Download:** Downloads the identified new versions to the designated local directory.
    /// 8.  **Installation :** Installs the downloaded versions, runs an installation verification, and then uninstalls them to test the integrity of each installer.
    /// 9.  **Data Persistence:** Adds the successfully tested and installed versions to the local JSON file for future reference.
    /// 10. **Final Cleanup:** Deletes all downloaded installer files to prevent clutter and save disk space.
    /// </remarks>
    public async static Task Production2_0()
    {

        Utils.Log("Starting Production 2.0 ...");

        //First thing is To clean UP the evniroment make sure old files left over 
        Initializer.Cleanup();
        Utils.Log("Cleanup is Done");

        // Second Step is to get all the version we have saved in the local json file and load them in a list
        var JsonList = Initializer.LocalStorageScrapping();
        Utils.Log("Number Of versions in Local Json File :::    " + JsonList.Count.ToString());

        //3rd Step is To get all the versions from the website TABLE and load them in a list
        var websiteList = await Initializer.InitWebScrapping();
        Utils.Log("Number Of versions From Website Table: " + websiteList.Count.ToString());

        //4th Step is to Get a List of Hidden Versions by checking subversions of the new versions found from the website table
        var hiddenVersionsList = await Initializer.HiddenInitWebScrapping(websiteList);
        Utils.Log("Number Of Hidden Versions Found: " + hiddenVersionsList.Count.ToString());

        //5th Step is to Combine the two lists of new versions from website table and hidden versions found by checking subversions (no duplicates)
        var combinedNewAndHiddenList = Initializer.GetCombinedWebAndHiddenVersions(websiteList, hiddenVersionsList);
        Utils.Log("Total Number Of All Versions From Website Table and Hidden Versions Combined : " + combinedNewAndHiddenList.Count.ToString());

        // 6th step is to compare the Combined List Of all discovered versions against the local Json List and get only the new versions;
        var NewVersionsToBeDownloaded = Initializer.CompareAndGetNewVersions(combinedNewAndHiddenList, JsonList);
        Utils.Log("Number Of New Versions To Be Downloaded: " + NewVersionsToBeDownloaded.Count.ToString());

        // 7th Step is to Download the new versions found from the previous step and get a list of downloaded file paths
        var ListOfDownloadedVersions = await Initializer.DownloadAllNewversions(NewVersionsToBeDownloaded);
        Utils.Log("Number Of New Versions That has Been Successfully Downloaded: " + ListOfDownloadedVersions.Count.ToString());

        // 8tth Step is to Install the new downloaded versions and get a list of installed versions
        var ListOfInstalledVersions = await Initializer.InstallNewDownloadedVersions(ListOfDownloadedVersions);
        Utils.Log("Number Of New Versions That has Been Successfully Installed: " + ListOfInstalledVersions.Count.ToString());

        // 9th Step is to add the successfully installed versions To oue Local Json File 
        Initializer.AddNewVersionsToJsonFile(ListOfInstalledVersions, JsonList);

        //10th step is to clean up the environment by deleting all downloaded files
        Initializer.Cleanup();


    }


    /// <summary>
    /// Test case to fill the local JSON file with all versions from the website table only, without downloading or installing them.
    /// This is only used for testing and demonstration purposes.
    /// </summary>
    /// <remarks>
    /// This method will perfdorm 3 main steps :
    /// 1. Load existing versions from the local JSON file as a List
    /// 2. Scrape all versions from the website table as a List
    /// 3. Combine both lists and save the result back to the local JSON file, avoiding duplicates.
    /// 
    /// </remarks>
    public async static Task TestCase_FillJsonWithWebSiteVersionsOnly()
    {
        Utils.Log("TestCase :::: Starting Test Case to fill json with all versions from website table...");

        // Second Step is to get all the version we have saved in the local json file and load them in a list
        var JsonList = Initializer.LocalStorageScrapping();
        Utils.Log("Number Of versions in Local Json File :::    " + JsonList.Count.ToString());

        //3rd Step is To get all the versions from the website TABLE and load them in a list
        var websiteList = await Initializer.InitWebScrapping();
        Utils.Log("Number Of versions From Website Table: " + websiteList.Count.ToString());

        // 9th Step is to add the successfully installed versions To oue Local Json File 
        Initializer.SaveToJsonFile(websiteList);
        Utils.Log("Number of versions in Local Json File after update :::    " + Initializer.LocalStorageScrapping().Count.ToString());

    }


    /// <summary>
    /// This test case scrapes all available versions from a website, finds any hidden subversions,
    /// and then updates the local JSON file with all the newly discovered versions.
    /// </summary>
    /// <remarks>
    /// The method ensures that the web-scraping and JSON-handling logic is robust and comprehensive.
    /// It performs a multi-step workflow:
    /// 1. Loads existing versions from the local JSON file.
    /// 2. Scrapes publicly listed versions from the website's table.
    /// 3. Searches for "hidden" subversions related to the publicly listed ones.
    /// 4. Combines the public and hidden version lists, removing duplicates.
    /// 5. Compares the combined list to the local list to identify only the truly new versions.
    /// 6. Appends these new versions to the local JSON file for future reference.
    /// </remarks>
    public async static Task TestCase_FillJsonWithWebSiteAndHiddenVersion()
    {
        Utils.Log("Test Case ::::");

        // Second Step is to get all the version we have saved in the local json file and load them in a list
        var JsonList = Initializer.LocalStorageScrapping();
        Utils.Log("Number Of versions in Local Json File :::    " + JsonList.Count.ToString());

        //3rd Step is To get all the versions from the website TABLE and load them in a list
        var websiteList = await Initializer.InitWebScrapping();
        Utils.Log("Number Of versions From Website Table: " + websiteList.Count.ToString());

        //4th Step is to Get a List of Hidden Versions by checking subversions of the new versions found from the website table
        var hiddenVersionsList = await Initializer.HiddenInitWebScrapping(websiteList);
        Utils.Log("Number Of Hidden Versions Found: " + hiddenVersionsList.Count.ToString());

        //5th Step is to Combine the two lists of new versions from website table and hidden versions found by checking subversions (no duplicates)
        var combinedNewAndHiddenList = Initializer.GetCombinedWebAndHiddenVersions(websiteList, hiddenVersionsList);
        Utils.Log("Total Number Of All Versions From Website Table and Hidden Versions Combined : " + combinedNewAndHiddenList.Count.ToString());

        // 6th step is to compare the Combined List Of all discovered versions against the local Json List and get only the new versions;
        var NewVersionsToBeDownloaded = Initializer.CompareAndGetNewVersions(combinedNewAndHiddenList, JsonList);
        Utils.Log("Number Of New Versions Found :  " + NewVersionsToBeDownloaded.Count.ToString());

        // 9th Step is to add the successfully installed versions To oue Local Json File 
        Initializer.AddNewVersionsToJsonFile(NewVersionsToBeDownloaded, JsonList);
        Utils.Log("Number of versions in Local Json File after update :::    " + Initializer.LocalStorageScrapping().Count.ToString());

    }

    /// <summary>
    /// This test case verifies the functionality of the Download Manager.
    /// </summary>
    /// <remarks>
    /// The test simulates a version discovery and download workflow to ensure that the download components are working as expected. This process includes:
    /// <br/>
    /// 1.  **Version Retrieval:** Loads existing versions from the local JSON file.
    /// 2.  **Web Scraping:** Retrieves all publicly listed versions from the website's table.
    /// 3.  **Version Comparison:** Compares the web-scraped list against the local list to identify only the new versions that need to be downloaded.
    /// 4.  **Download Execution:** Attempts to download the new versions and tracks which ones are successfully downloaded.
    /// </remarks>
    public async static Task TestCase_CheckIfDownloadManagerWorks()
    {
        Utils.Log("TestCase :::: Starting Test Case to check if Download Manager works...");

        // Second Step is to get all the version we have saved in the local json file and load them in a list
        var JsonList = Initializer.LocalStorageScrapping();
        Utils.Log("Number Of versions in Local Json File :::    " + JsonList.Count.ToString());

        //3rd Step is To get all the versions from the website TABLE and load them in a list
        var websiteList = await Initializer.InitWebScrapping();
        Utils.Log("Number Of versions From Website Table: " + websiteList.Count.ToString());

        // 6th step is to compare the Combined List Of all discovered versions against the local Json List and get only the new versions;
        var NewVersionsToBeDownloaded = Initializer.CompareAndGetNewVersions(websiteList, JsonList);
        Utils.Log("Number Of New Versions To Be Downloaded: " + NewVersionsToBeDownloaded.Count.ToString());

        // 7th Step is to Download the new versions found from the previous step and get a list of downloaded file paths
        var ListOfDownloadedVersions = await Initializer.DownloadAllNewversions(NewVersionsToBeDownloaded);
        Utils.Log("Number Of New Versions That has Been Successfully Downloaded: " + ListOfDownloadedVersions.Count.ToString());

    }



    public static async Task Main(string[] args)
    {

        Utils.Log($"=== Task started at {DateTime.Now} ===");

        try
        {
            // # uncomment the line below to fill json with all versions from website table only (testing only)
            //await TestCase_FillJsonWithWebSiteVersionsOnly();

            // # uncomment the line below to fill json with all versions from website table and hidden versions included (testing only)
            //await TestCase_FillJsonWithWebSiteAndHiddenVersion();

            // # uncomment the line below to test if download manager works (testing only)
            //await TestCase_CheckIfDownloadManagerWorks();

            await Production2_0();

            Utils.Log($"=== Task completed successfully at {DateTime.Now} ===");
        }
        catch (Exception ex)
        {
            Utils.Log($"=== Task failed at {DateTime.Now}: {ex.Message} ===", "ERROR");
            Environment.Exit(1); // Exit with error code
        }

        // Don't wait for user input in scheduled mode
        if (args.Length == 0 || args[0] != "--interactive")
        {
            Environment.Exit(0);
        }




    }
}