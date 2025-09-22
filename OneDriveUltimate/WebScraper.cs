using System.Collections.Generic;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Net;
using System.Globalization;

public static class WebScraper
{
    /// <summary>
    /// Asynchronously scrapes version information from an HTML table on a given URL.
    /// </summary>
    /// <param name="url">The URL of the webpage to scrape.</param>
    /// <param name="LastYearIncluded">The last two-digit year (e.g., 20 for 2020) to include in the scrape. Defaults to 17.</param>
    /// <returns>A list of <see cref="VersionInfo"/> objects found on the webpage.</returns>
    /// <remarks>
    /// This method fetches the HTML content, parses it using <see cref="HtmlAgilityPack.HtmlDocument"/>, and
    /// extracts version numbers and dates from an HTML table. It stops scraping once it
    /// encounters a version from a year older than the specified <paramref name="LastYearIncluded"/>,
    /// optimizing the process by not fetching irrelevant data.
    /// </remarks>
    public static async Task<List<VersionInfo>> ScrapeHtmlAsync(string url, int? LastYearIncluded = 25)
    {
        Utils.Log("Fetching versions from website table...");

        //empty list to store the versions from website
        var versions = new List<VersionInfo>();


        //client is responsible for fetching the HTML content and save it to html variable as a string
        using HttpClient client = new HttpClient();
        var html = await client.GetStringAsync(url);

        // the doc variable is responsible for parsing the html string and allow us to navigate through the HTML elements (DOM)
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Select rows from the table and extract version info
        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows != null)
        {
            foreach (var row in rows.Skip(1)) // Skip header row cause its not a version just a header ..
            {
                // cell index 0 = date , cell index 1 = version number (cell basiaclly means column in the table)
                var cells = row.SelectNodes("td");
                if (cells != null && cells.Count > 1)
                {
                    // Extract and filter by year it will check the first two digits of the version number and compare it
                    // to the LastYearIncluded parameter that we specify in Config.cs
                    string versionNumber = cells[1].InnerText.Trim();
                    if (LastYearIncluded != null && int.Parse(versionNumber.Substring(0, 2)) >= LastYearIncluded)
                    {
                        versions.Add(new VersionInfo
                        {
                            VersionDate = cells[0].InnerText.Trim(),
                            Version = versionNumber

                        });
                    }
                    else
                    {
                        Utils.Log("All versions fetched since The year :    " + LastYearIncluded.ToString());
                        break; // Exit the loop if we reach a year less than LastYearIncluded
                    }
                    // End the scrapping if we reach the Startyear and its included 

                }
            }
        }
        return versions;
    }

    /// <summary>THIS METHOD IS NOT USED CURRENTLY .. (Does not search in parallel we have it just in case we need it in future)
    /// Asynchronously checks for "hidden" OneDrive versions by generating potential version numbers
    /// uncomment it if needed
    /// </summary>

    /*public static async Task<List<VersionInfo>> GetListOfHiddenVersions()
    {
        Utils.Log("function");
        var AllVersions = StorageManager.GetStoredVersions(Config.VersionFile);
        int hiddenItemsSearchLimit = Config.MaxSubVersionCheck;

        var hiddenVersions = new List<VersionInfo>();

        using var httpClient = new HttpClient();
        foreach (var version in AllVersions)
        {
            Utils.Log("looping each");
            string versionNumberStr = version.Version; // e.g. "19.002.0107.0008"

            // split version into prefix + subversion
            int lastDotIndex = versionNumberStr.LastIndexOf('.');
            if (lastDotIndex == -1) continue;

            string prefix = versionNumberStr.Substring(0, lastDotIndex + 1); // "19.002.0107."
            string lastPart = versionNumberStr.Substring(lastDotIndex + 1);   // "0008"

            if (!int.TryParse(lastPart, out int baseSubVersion))
                continue;

            for (int i = 1; i <= hiddenItemsSearchLimit; i++)
            {
                Utils.Log("looping versions");
                string candidateSubVersion = i.ToString("D4"); // pad with leading zeros (0001, 0002...)
                string candidateVersion = prefix + candidateSubVersion;

                // Skip if already exists in our stored versions
                if (AllVersions.Any(v => v.Version == candidateVersion))
                    continue;

                // Build both URLs
                string url64 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/amd64/OneDriveSetup.exe";
                string url32 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/OneDriveSetup.exe";

                // Check availability
                if (await UrlExistsAsync(httpClient, url64) || await UrlExistsAsync(httpClient, url32))
                {
                    hiddenVersions.Add(new VersionInfo
                    {
                        Version = candidateVersion,
                        VersionDate = version.VersionDate, // you might need to decide how to handle date

                    });
                    Utils.Log("versoin added");
                }
            }
        }
        Utils.Log("returning all hidden done");

        return hiddenVersions;
    }*/


    /// <summary>
    /// Scans for “hidden” sub-versions of OneDrive that are not present in the provided version list.
    /// Performs concurrent HTTP checks for candidate installer URLs (both x64 and x86) using parallel tasks.
    /// </summary>
    public static async Task<List<VersionInfo>> GetListOfHiddenVersionsParallel(List<VersionInfo> AllVersions)
    {

        //var AllVersions = StorageManager.GetStoredVersions(Config.VersionFile);
        int hiddenItemsSearchLimit = Config.MaxSubVersionCheck;

        // a data type act as bag and thread will grab from it at the same time
        var hiddenVersions = new ConcurrentBag<VersionInfo>(); // thread-safe

        // the agent that resposible to initiate the requests
        using var httpClient = new HttpClient();

        foreach (var version in AllVersions)
        {
            Utils.Log($"Checking subVersions in base version: {version.Version}");

            // var to save the version number as a string
            string versionNumberStr = version.Version;

            // get the index of the last dot (will be used to split the string)
            int lastDotIndex = versionNumberStr.LastIndexOf('.');
            if (lastDotIndex == -1) continue;

            //get a substring of thenumbers before the last dot 23.345.2234.
            string prefix = versionNumberStr.Substring(0, lastDotIndex + 1);
            //get a substring of thenumbers after the last dot .0001
            string lastPart = versionNumberStr.Substring(lastDotIndex + 1);


            if (!int.TryParse(lastPart, out int baseSubVersion))
                continue;

            // Generate candidate versions
            var candidateTasks = Enumerable.Range(1, hiddenItemsSearchLimit)
                .Select(async i =>
                {
                    //make sure the section is from 4 digits 0000
                    string candidateSubVersion = i.ToString("D4");
                    // combin the first part of string to the sub nuber that will be tested like 0007
                    string candidateVersion = prefix + candidateSubVersion;

                    if (AllVersions.Any(v => v.Version == candidateVersion))
                        return; // skip existing

                    // now the veersion we  have will be placed in a test url to check if its vaild or not 
                    string url64 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/amd64/OneDriveSetup.exe";
                    string url32 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/OneDriveSetup.exe";

                    //if the function UrlExistsAsync returnds trueit will add the version to out hiddenlist of versions to be returned 
                    if (await UrlExistsAsync(httpClient, url64) || await UrlExistsAsync(httpClient, url32))
                    {
                        hiddenVersions.Add(new VersionInfo
                        {
                            Version = candidateVersion,
                            VersionDate = version.VersionDate
                        });
                        Utils.Log($"Hidden version found: {candidateVersion}");
                    }
                });

            // Run all candidate checks for this version concurrently
            await Task.WhenAll(candidateTasks);
        }

        Utils.Log("returning all hidden done");
        return hiddenVersions.ToList();
    }


    /// <summary>
    ///  checks if a URL exists by sending a HEAD request and if it code 200 means success if will return True .
    /// </summary>
    private static async Task<bool> UrlExistsAsync(HttpClient client, string url)
    {
        try
        {
            // it will send a get request and get back the reading of the request header and check for the response read if its 404 or 200 
            using var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url),
                HttpCompletionOption.ResponseHeadersRead);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }




    /// <summary>
    /// Still Under testing Can be used as a fall back method if the website stopped working and if we decided to change how we obtain the new versions 
    /// </summary>
    /// <param name="MostRecentVersion"></param>
    /// <returns></returns>
    public static async Task<List<VersionInfo>> ConstructNewVersionsOnTheFlyNormal(VersionInfo MostRecentVersion)
    {
        // empty list to store the new successfully created versions 
        var CreatedVersions = new List<VersionInfo>();

        string CurrentDate = DateTime.Now.ToString("MM/dd/yy");

        string[] CurrentDateParts = CurrentDate.Split('/'); // [0] = 09 [1] = 14 [2] = 25
        Console.WriteLine(string.Join(",", CurrentDateParts));

        Dictionary<string, string> CurrentDateDictionary = new Dictionary<string, string>
        {
            {"month" , CurrentDateParts[0]},
            {"day" , CurrentDateParts[1]},
            {"year" , CurrentDateParts[2]}
        };


        string[] MostRecentversionParts = MostRecentVersion.Version.Split('.'); // [0] = 25
        Console.WriteLine(string.Join(",", MostRecentversionParts));

        Dictionary<string, string> MostRecentversionDictionary = new Dictionary<string, string>
        {
            { "year",MostRecentversionParts[0]},
            { "versionCounter",MostRecentversionParts[1]},
            { "dateCounter",MostRecentversionParts[2]},
            { "subVersions",MostRecentversionParts[3]}
        };


        string[] NewConstructedVersionParts = { };

        Dictionary<string, string> NewConstructedVersionDictionary = new Dictionary<string, string>();

        for (int vc = 1; vc <= 10; vc++)
        {
            for (int dc = int.Parse(MostRecentversionDictionary["dateCounter"]);
            dc <= int.Parse(MostRecentversionDictionary["dateCounter"]) + 10;
            dc++)
            {

                NewConstructedVersionDictionary["year"] = CurrentDateDictionary["year"];
                NewConstructedVersionDictionary["versionCounter"] = (int.Parse(MostRecentversionDictionary["versionCounter"]) + vc).ToString();
                NewConstructedVersionDictionary["dateCounter"] = dc.ToString("D4");
                NewConstructedVersionDictionary["subVersions"] = "0001";

                string newVersionStringToTest = $"{NewConstructedVersionDictionary["year"]}.{NewConstructedVersionDictionary["versionCounter"]}.{NewConstructedVersionDictionary["dateCounter"]}.{NewConstructedVersionDictionary["subVersions"]}";

                string testURL = $"https://oneclient.sfx.ms/Win/Installers/{newVersionStringToTest}/amd64/OneDriveSetup.exe";
                Console.WriteLine(newVersionStringToTest);

                if (await UrlExistsAsync(new HttpClient(), testURL))
                {
                    CreatedVersions.Add(new VersionInfo
                    {
                        Version = newVersionStringToTest,
                        VersionDate = "00/00/00"
                        //VersionDate = $"{CurrentDateDictionary["month"]}/{CurrentDateDictionary["day"]}/{CurrentDateDictionary["year"]}"
                    });
                }
            }
        }

        return CreatedVersions;
    }
        
    
    /// <summary>
    /// Creating new versions on the fly by testing them in parallel to speed up the process
    /// how it works based on the moist recent published version it will start testing new versions by increasing the version counter and date counter
    /// version 20.188.0927/0001 : 20 = year , 188 = version counter , 0927 = date counter , 0001 = sub version
    /// it will increase the version counter by 1 and date counter by 1 and will test the new version if its valid or not
    /// if its valid it will be added to the list of new versions to be returned to be used later for downloading and installing
    /// </summary>
    /// <param name="MostRecentVersion"></param>
    /// <returns></returns>
    public static async Task<List<VersionInfo>> ConstructNewVersionsOnTheFlyParallel(VersionInfo MostRecentVersion)
    {
        // empty list to store the new successfully created versions used a concurrent bag to allow multiple threads to add to it at the same time
        var CreatedVersions = new ConcurrentBag<VersionInfo>();

        // the agent that resposible to initiate the requests to send if the version is valid or not
        using var httpClient = new HttpClient();

        //get the current date and split it to be used later in creating new versions
        string CurrentDate = DateTime.Now.ToString("MM/dd/yy");
        string[] CurrentDateParts = CurrentDate.Split('/');
        
        //dictionary to store the current date parts so we can use it later in creating new versions easier thatn using array (key, values)
        var CurrentDateDictionary = new Dictionary<string, string>
        {
            {"month" , CurrentDateParts[0]},
            {"day" , CurrentDateParts[1]},
            {"year" , CurrentDateParts[2]}
        };

        //array to store the most recent version parts after splitting it by dot
        //so array index 0 = year , index 1 = version counter , index 2 = date counter , index 3 = sub version
        string[] MostRecentversionParts = MostRecentVersion.Version.Split('.');
        
        //dictionary to store the most recent version parts so we can use it later in creating new versions easier thatn using array (key, values)
        var MostRecentversionDictionary = new Dictionary<string, string>
        {
            { "year", MostRecentversionParts[0]},
            { "versionCounter", MostRecentversionParts[1]},
            { "dateCounter", MostRecentversionParts[2]},
            { "subVersions", MostRecentversionParts[3]}
        };

        //list to store all the tasks that will be created to test the new versions in parallel
        // basically each task will test a new version if its valid or not
        var tasks = new List<Task>();

        // Limit to 5 parallel requests at a time (adjust as needed)
        //semaphore is used to limit the number of parallel requests to avoid crashing the system or getting blocked by microsoft
        var semaphore = new SemaphoreSlim(5);


        // 2 nested loops ...
        // first loop to increase the version counter by 1 up to 20 times
        // second loop to increase the date counter by 1 up to 20 times for each version counter
        // for example if the most recent version is 20.188.0927.0001
        // the first loop will increase the version counter to 20.189 
        // and the second loop will increase the date counter to 0928, 0929, 0930, 0931, 0932 ... etc
        for (int vc = 1; vc <= 20; vc++)
        {
            // Parse the starting date from the most recent version
            string startYear = CurrentDateDictionary["year"];
            string startDateCounter = MostRecentversionDictionary["dateCounter"]; // e.g. "0927"

            DateTime startDate = DateTime.ParseExact(
                $"{startYear}{startDateCounter}",
                "yyMMdd",
                CultureInfo.InvariantCulture);

            for (int offset = 0; offset < 20; offset++)
            {
                // adding one day to the start date in each iteration of the loop
                DateTime currentDate = startDate.AddDays(offset);

                /// Putting all pieces together to create the new version to be tested
                // year will be the current year
                string year = CurrentDateDictionary["year"];
                // version counter will be the most recent version counter + the value of the first loop (vc)
                string versionCounter = (int.Parse(MostRecentversionDictionary["versionCounter"]) + vc).ToString();
                // date counter will be the current date in the loop in MMDD format
                string dateCounter = currentDate.ToString("MMdd");
                // subversion only testing 0001 for now as its the main one used by microsoft
                string subVersions = "0001";

                //combin all the pieces of the version to be tested
                string newVersionStringToTest = $"{year}.{versionCounter}.{dateCounter}.{subVersions}";
                string testURL = $"https://oneclient.sfx.ms/Win/Installers/{newVersionStringToTest}/amd64/OneDriveSetup.exe";

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(); // wait for a slot
                    try
                    {
                        Utils.Log($"Testing version: {newVersionStringToTest}");
                        if (await UrlExistsAsync(httpClient, testURL))
                        {
                            CreatedVersions.Add(new VersionInfo
                            {
                                Version = newVersionStringToTest,
                                VersionDate = "00/00/00"
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release(); // free the slot
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        return CreatedVersions.ToList();
    }
}