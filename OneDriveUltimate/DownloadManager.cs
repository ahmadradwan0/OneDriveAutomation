using System.Text.Json;

/// <summary>
/// it is responsible for downloading OneDrive installers based on specified versions and architectures.
/// it has 3 functions the main one is DownloadNewVersions which takes a list of VersionInfo objects and downloads the corresponding installers
/// and the other two private functions are helper functions to download files from a URL and to handle architecture-specific downloads so we dontt repeat code
/// </summary>
public static class DownloadManager
{

    // helper function to download a file from a url and save it to a specified path
    // takes 3 : 1: HttpClient client to make the request and it is passed from the main function to reuse the same client
    // 2: string url the url to download from
    // 3: string filePath the path to save the file to
    private static async Task DownloadFileFromURL(HttpClient client, string url, string filePath)
    {
        Utils.Log($"Downloading {url}...");

        // Send GET request to the URL and get the response headers 
        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            // Ensure the request was successful wiith 200 status code
            response.EnsureSuccessStatusCode();

            // streamToReadFrom is the stream we read from the response its will contain the file data
            // streamToWriteTo is the stream we write to the file we created at filePath
            // we use CopyToAsync to copy the data from the response stream to the file stream (basically saving the file)
            using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
            using (var streamToWriteTo = File.Create(filePath))
            {
                await streamToReadFrom.CopyToAsync(streamToWriteTo);
            }
        }

        Utils.Log($"Saved to {filePath}");

    }

    /// helper method just to actually call Download From url function and handle the version info object and saving the path
    /// it checks if the file already exists and if it does it skips the download and just
    /// it take 4 parameters
    /// 1: HttpClient client to make the request and it is passed from the main function to reuse the same client
    /// 2: VersionInfo : is an object of type VersionInfo to store the path of the downloaded file in it 
    /// 3: string path : the path to save the file to
    /// 4: string url : the url to download from
    private static async Task DownloadFileArchSensitive(HttpClient client, VersionInfo versionInfo, string path, string url)
    {
        //string path = Path.Combine(downloadPath, fileName);
        if (!File.Exists(path))
        {
            try
            {
                // Call the helper function to download the file from the url and save it to the path
                // arguments required : 1: HttpClient client to make the request and it is passed from the main function to reuse the same client
                // 2: string url the url to download from
                // 3: string filePath the path to save the file to
                await DownloadFileFromURL(client, url, path);

                // add the path to the list of paths in the version info object
                versionInfo.InstallerStoredPaths.Add(path); 
            }
            catch (Exception ex)
            {
                Utils.Log($"Failed to download {path}: {ex.Message}", "ERROR");
            }
        }
        else
        {
            Utils.Log($"{path} already exists at {path}, skipping download.");
            // if the file already exists we still add it to the list of paths in the version info object
            versionInfo.InstallerStoredPaths.Add(path); 
        }


    }


    //##  ##  ## ##  # # # # ################
    /// <summary>
    /// This is the main function to download versions of the installers based on the architecture specified in the config
    /// it takes a list of VersionInfo objects and downloads the corresponding installers based on the architecture
    /// it returns a list of VersionInfo objects that were successfully downloaded
    /// it creates a folder for each version inside the download path specified in the config and saves the installers there
    /// </summary>
    public async static Task<List<VersionInfo>> DownloadNewVersions(List<VersionInfo> versions)
    {
        // empty list to store the versions that were successfully downloaded
        var downloadedVersions = new List<VersionInfo>();

        // using a single HttpClient instance that responsible for making the HTTP requests to download the files
        using (var client = new HttpClient())
        {
            // loop through each version in the list of versions to download
            foreach (var version in versions)
            {
                // two urls for the 64-bit and 32-bit versions of the installer and placeholders for the version number
                string version64Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/amd64/OneDriveSetup.exe";
                string version32Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/OneDriveSetup.exe";
                string version64ArmUrl = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/arm64/OneDriveSetup.exe";


                // basically compines the download path where already been specified in the config with the version number 
                // to create a unique folder for each version that where the installers will be saved
                // Directory.CreateDirectory is used to create the directory if it does not exist with the path we combined
                string downloadPath = Path.Combine(Config.DownloadPath, version.Version);
                Directory.CreateDirectory(downloadPath);

                // a variable as a tracker to know how many files we expect to download for this version based on the architecture
                int expectedDownloadsFilesPerVersion = 0;

                // switch case to handle the architecture specified in the config
                switch (Config.Architecture)
                {
                    case "x64":
                        // the path64 here is the path of the installer it self inside the version folder we created above

                        string path64 = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");

                        try
                        {
                            // here is the main calling for the helper function that downloads the file and handles the version info object
                            await DownloadFileArchSensitive(client, version, path64, version64Url);

                            // we expect to download 1 file for this version se we increase the counter by 1 
                            expectedDownloadsFilesPerVersion = 1;
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");
                            // Continue to next file/version
                        }

                        break;

                    case "x86":
                            // path for the 32-bit version of the installer
                        string path32 = Path.Combine(downloadPath, "OneDriveSetup_x86.exe");

                        try
                        {
                            // here is the main calling for the helper function that downloads the file and handles the version info object
                            await DownloadFileArchSensitive(client, version, path32, version32Url);
                            expectedDownloadsFilesPerVersion = 1;
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to download 32-bit version {version.Version}: {ex.Message}", "ERROR");
                            // Continue to next file/version
                        }

                        break;

                    case "Both":
                        // same concept as above but here we have to download both versions so we have two paths and we call the download function twice
                        string bothPath64 = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");
                        string bothPath32 = Path.Combine(downloadPath, "OneDriveSetup_x86.exe");

                        // a counter to track how many downloads were successful
                        int successful = 0;

                        try
                        {
                            // calling the download function for the 64-bit version
                            await DownloadFileArchSensitive(client, version, bothPath64, version64Url);
                            successful++;

                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");

                        }

                        try
                        {
                            // calling the download function for the 32-bit version
                            await DownloadFileArchSensitive(client, version, bothPath32, version32Url);
                            successful++;
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to download 32-bit version {version.Version}: {ex.Message}", "ERROR");
                        }


                        expectedDownloadsFilesPerVersion = successful;
                        break;


                    case "ARM64":
                                        // the path64 here is the path of the installer it self inside the version folder we created above

                        string path64Arm = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");

                        try
                        {
                            // here is the main calling for the helper function that downloads the file and handles the version info object
                            await DownloadFileArchSensitive(client, version, path64Arm, version64ArmUrl);

                            // we expect to download 1 file for this version se we increase the counter by 1 
                            expectedDownloadsFilesPerVersion = 1;
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");
                            // Continue to next file/version
                        }

                        break;
                    default:
                        Utils.Log("Unsupported or unknown architecture specified.", "WARNING");
                        break;
                }

                // Check if we downloaded the expected number of files for this version
                // so if the number of paths in that specific version equal to the expected number of versions that in actually 
                //  being incremented every successfull download then add it to our list that will be returned 
                if (version.InstallerStoredPaths.Count == expectedDownloadsFilesPerVersion)
                {
                    downloadedVersions.Add(version);
                    Utils.Log($"Successfully prepared version {version.Version} with {version.InstallerStoredPaths.Count} installer(s).");

                }
                // this condition when we have less number of the expected installers like 1 out of 2 installer successfull
                // we will still add it for now (during testing for 600 installers only happeded once if it did accure again we might add more logic here)
                else if (version.InstallerStoredPaths.Count > 0 && version.InstallerStoredPaths.Count < expectedDownloadsFilesPerVersion)
                {
                    downloadedVersions.Add(version);
                    Utils.Log($"Failed to download any installers for version {version.Version}.", "WARNING");
                }
                // last else when no installers or if the number is higher for some glitch we will ignore that installer
                else
                {
                    Utils.Log($"Version {version.Version} has {version.InstallerStoredPaths.Count} installer(s), expected {expectedDownloadsFilesPerVersion}. It may be incomplete.", "WARNING");
                }



            }
        }

        return downloadedVersions;
    }

}