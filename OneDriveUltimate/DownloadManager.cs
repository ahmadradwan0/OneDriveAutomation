using System.Text.Json;

public static class DownloadManager
{
    private static async Task DownloadFileFromURL(HttpClient client,string url, string filePath)
    {
        Utils.Log($"Downloading {url}...");
        
        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            
            using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
            using (var streamToWriteTo = File.Create(filePath))
            {
                await streamToReadFrom.CopyToAsync(streamToWriteTo);
            }
        }
        
        Utils.Log($"Saved to {filePath}");
        
    }


    private static async Task DownloadFileArchSensitive(HttpClient client, VersionInfo versionInfo, string path, string url)
    {
        //string path = Path.Combine(downloadPath, fileName);
        if (!File.Exists(path))
        {
            try
            {
                await DownloadFileFromURL(client, url, path);
                versionInfo.InstallerStoredPaths.Add(path); // Add path to the list
            }
            catch (Exception ex)
            {
                Utils.Log($"Failed to download {path}: {ex.Message}", "ERROR");
            }
        }
        else
        {
            Utils.Log($"{path} already exists at {path}, skipping download.");
            versionInfo.InstallerStoredPaths.Add(path); // Add path to the list
        }

        
    }
    //##  ##  ## ##  # # # # ################
    public async static Task<List<VersionInfo>> DownloadNewVersions(List<VersionInfo> versions)
    {
        var downloadedVersions = new List<VersionInfo>();
        using (var client = new HttpClient())
        {
            foreach (var version in versions)
            {
                string version64Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/amd64/OneDriveSetup.exe";
                string version32Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/OneDriveSetup.exe";

                //string downloadsFolder = Config.DownloadPath; // Use configured download path
                string downloadPath = Path.Combine(Config.DownloadPath, version.Version);
                Directory.CreateDirectory(downloadPath);

                int expectedDownloadsFilesPerVersion = 0;
                switch (Config.Architecture)
                {
                    case "x64":
                        // Download 64-bit version
                        string path64 = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");
                        
                            try
                            {
                                await DownloadFileArchSensitive(client, version, path64, version64Url);
                                expectedDownloadsFilesPerVersion = 1;
                            }
                            catch (Exception ex)
                            {
                                Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");
                                // Continue to next file/version
                            }
                       
                        break;

                    case "x86":
                        // Download 32-bit version
                        string path32 = Path.Combine(downloadPath, "OneDriveSetup_x86.exe");
                        
                            try
                            {
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
                        // Download both versions
                        string bothPath64 = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");
                        string bothPath32 = Path.Combine(downloadPath, "OneDriveSetup_x86.exe");
                        int successful = 0;

                            try
                            {
                                await DownloadFileArchSensitive(client, version, bothPath64, version64Url);
                                 successful++;

                            }
                            catch (Exception ex)
                            {
                                Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");

                            }

                            try
                            {
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
                    default:
                        Utils.Log("Unsupported or unknown architecture specified.", "WARNING");
                        break;
                }

                // Check if we downloaded the expected number of files for this version
                if (version.InstallerStoredPaths.Count == expectedDownloadsFilesPerVersion)
                { 
                    downloadedVersions.Add(version);
                    Utils.Log($"Successfully prepared version {version.Version} with {version.InstallerStoredPaths.Count} installer(s).");
                }
                else
                {
                    Utils.Log($"Version {version.Version} has {version.InstallerStoredPaths.Count} installer(s), expected {expectedDownloadsFilesPerVersion}. It may be incomplete.", "WARNING");
                }



            }
        }

        return downloadedVersions;
    }

}