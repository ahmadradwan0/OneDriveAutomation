using System.Collections.Generic;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Net;

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
    public static async Task<List<VersionInfo>> ScrapeHtmlAsync(string url, int? LastYearIncluded = 17)
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

/// <summary>
/// Asynchronously checks for "hidden" OneDrive versions by generating potential version numbers
/// THIS METHOD IS NOT USED CURRENTLY .. (Does not search in parallel we have it just in case we need it in future)
    public static async Task<List<VersionInfo>> GetListOfHiddenVersions()
    {
        Utils.Log("function");
        //var AllVersions = StorageManager.GetStoredVersions();
        //test list
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
    }

public static async Task<List<VersionInfo>> GetListOfHiddenVersionsParallel(List<VersionInfo> AllVersions)
{

    //var AllVersions = StorageManager.GetStoredVersions(Config.VersionFile);
    int hiddenItemsSearchLimit = Config.MaxSubVersionCheck;

    var hiddenVersions = new ConcurrentBag<VersionInfo>(); // thread-safe

    using var httpClient = new HttpClient();

    foreach (var version in AllVersions)
    {
        Utils.Log($"Checking subVersions in base version: {version.Version}");

        string versionNumberStr = version.Version;

        int lastDotIndex = versionNumberStr.LastIndexOf('.');
        if (lastDotIndex == -1) continue;

        string prefix = versionNumberStr.Substring(0, lastDotIndex + 1);
        string lastPart = versionNumberStr.Substring(lastDotIndex + 1);

        if (!int.TryParse(lastPart, out int baseSubVersion))
            continue;

        // Generate candidate versions
        var candidateTasks = Enumerable.Range(1, hiddenItemsSearchLimit)
            .Select(async i =>
            {
                string candidateSubVersion = i.ToString("D4");
                string candidateVersion = prefix + candidateSubVersion;

                if (AllVersions.Any(v => v.Version == candidateVersion))
                    return; // skip existing

                string url64 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/amd64/OneDriveSetup.exe";
                string url32 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/OneDriveSetup.exe";

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

}