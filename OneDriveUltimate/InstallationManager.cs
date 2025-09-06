using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public static class InstallationManager
{

    public static async Task<bool> UninstallVersion(string installerPath)
    { 
        try
        {
            var IsInstalled =  VerifyInstallation();
            // Check if actually installed first
            if (!IsInstalled)
            {
                Utils.Log("UninstallVersion :::: OneDrive is not installed. Nothing to uninstall.");
                return true; // Consider this success since nothing to uninstall
            }

            Utils.Log("UninstallVersion :::: Uninstalling...");
            bool uninstallSuccess = await RunInstaller(installerPath, "/uninstall /silent");
            
            //Fall back method incase if the uninstall via the installer exe fails
            if (!uninstallSuccess)
            {
                Utils.Log($"UninstallVersion :::: Uninstallation Terminal Process failed Trying the Fall Back method ::::: for {installerPath}", "ERROR");
                uninstallSuccess = await UninstallCurrentVersion();
            }
            
            // if it still fail return false
            if (!uninstallSuccess)
            {
                Utils.Log($"UninstallVersion :::: Uninstallation Terminal Process failed for {installerPath}", "ERROR");
                return false;
            }
            
            // Verify uninstallation
            Utils.Log("UninstallVersion :::: Verifying uninstallation...");
            await Task.Delay(TimeSpan.FromSeconds(60)); 
            IsInstalled =  VerifyInstallation();
            
            if (IsInstalled)
            {
                Utils.Log("UninstallVersion :::: Uninstallation verification failed - OneDrive still detected", "ERROR");
                return false;
            }
            
            Utils.Log("UninstallVersion :::: Uninstallation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Utils.Log($"UninstallVersion :::: Error during uninstallation: {ex.Message}", "ERROR");
            return false;
        }     

    }

    public static async Task<bool> UninstallCurrentVersion(){ 
        try
        {
            // Try to find OneDrive installer in common locations to uninstall
            string[] possibleInstallerPaths = 
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive", "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive", "OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "OneDrive", "OneDriveSetup.exe")
            };
            
            foreach (var installerPath in possibleInstallerPaths)
            {
                if (File.Exists(installerPath))
                {
                    Utils.Log($"Current :::: Found installer for uninstallation: {installerPath}");
                    return await RunInstaller(installerPath, "/uninstall /silent");
                }
            }
            
            Utils.Log("Current ::::  No OneDrive installer found for uninstallation", "WARNING");
            return false;
        }
        catch (Exception ex)
        {
            Utils.Log($"Current ::::  Error uninstalling current version: {ex.Message}", "ERROR");
            return false;
        }
    }
    public static async Task<bool> InstallVersion(string installerPath)
    {
        try
        {
            // check if we have a version already installed so we have to uninstall that version first before we install the new version
            var IsThereAnyVersionInstalled =  VerifyInstallation();
            if (IsThereAnyVersionInstalled)
            {
                Utils.Log($"InstallVersion :::: OneDrive is already installed. Uninstalling current version first...");
                var uninstallCurrentVersionSuccess = await UninstallCurrentVersion();
                if (!uninstallCurrentVersionSuccess)
                {
                    Utils.Log("InstallVersion :::: Failed to uninstall current version. Aborting installation.", "ERROR");
                    return false;
                }
            }

            Utils.Log($"InstallVersion :::: Installing {Path.GetFileName(installerPath)}...");
            bool installSuccess = await RunInstaller(installerPath, "/silent");

            if (!installSuccess)
            {
                Utils.Log($"InstallVersion :::: Installation failed for {installerPath}", "ERROR");
                return false;
            }

            // Verify installation
            Utils.Log("InstallVersion :::: Verifying installation...");
            bool isInstalled =  VerifyInstallation();

            if (!isInstalled)
            {
                Utils.Log("InstallVersion :::: Installation verification failed", "ERROR");
                return false;
            }

            Utils.Log("InstallVersion :::: Installation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Utils.Log($"InstallVersion :::: Error during installation: {ex.Message}", "ERROR");
            return false;
        }
    }

    public static async Task<List<VersionInfo>> InstallAndUninstallVersions(List<VersionInfo> downloadedVersions)
    {
        var installedVersions = new List<VersionInfo>();
        foreach (var DVersion in downloadedVersions)
        {
            int expectedInstalledFilesPerVersion = 0;
            foreach (var StoredPath in DVersion.InstallerStoredPaths)
            {

                try
                {

                    // Install Uninstall cycle for each installer ... 
                    Utils.Log($"Cycle :::: Installing {Path.GetFileName(StoredPath)}...");
                    bool installSuccess = await InstallVersion(StoredPath);

                    if (!installSuccess)
                    {
                        Utils.Log($"Cycle :::: Installation failed for {StoredPath}", "ERROR");
                        continue;
                    }

                    // Step 2: Verify installation
                    Utils.Log("Cycle :::: Verifying installation...");
                    bool isInstalled = VerifyInstallation();

                    if (!isInstalled)
                    {
                        Utils.Log("Cycle :::: Installation verification failed", "ERROR");
                        continue;
                    }

                    // Step 3: Wait before uninstalling
                    Utils.Log("Cycle :::: Waiting before uninstallation 60 Seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(60)); // Wait 60 seconds

                    // Step 4: Uninstall
                    Utils.Log("Cycle :::: Uninstalling...");
                    bool uninstallSuccess = await UninstallVersion(StoredPath);
                    if (!uninstallSuccess)
                    {
                        Utils.Log($"Cycle :::: Uninstallation failed for {StoredPath}", "ERROR");
                    }
                    else
                    {
                        expectedInstalledFilesPerVersion++;
                        Utils.Log("Cycle :::: Successfully completed installation/uninstallation cycle");
                    }
                }
                catch (Exception ex)
                {
                    Utils.Log($"Cycle :::: Error in install/uninstall cycle: {ex.Message}", "ERROR");
                }

                Utils.Log("----------------------------------------------------------");
            }

             if(expectedInstalledFilesPerVersion == DVersion.InstallerStoredPaths.Count)
             {
                 DVersion.InstallUnInstallCycleSuccess = true;
                 installedVersions.Add(DVersion);
                 Utils.Log($"Cycle ::::  {DVersion.Version}  has been added to the installed Versions list");
             }
             else
             {
                 Utils.Log($"Cycle :::: Version {DVersion.Version} installation incomplete. Expected: {DVersion.InstallerStoredPaths.Count}, Successful: {expectedInstalledFilesPerVersion}", "WARNING");
             }

        }
        return installedVersions;
    }
    
    private static async Task<bool> RunInstaller(string installerPath, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas", // Run as admin
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    Utils.Log("Failed to start installer process", "ERROR");
                    return false;
                }
                
                // Wait for exit with timeout (10 minutes)
                bool exited = await Task.Run(() =>process.WaitForExit(600000));
                
                if (!exited)
                {
                    Utils.Log("Installer process timed out", "ERROR");
                    process.Kill();
                    return false;
                }
                
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Utils.Log($"Error running installer: {ex.Message}", "ERROR");
            return false;
        }
    }
    
    private static bool VerifyInstallation()
    {
        // Check if OneDrive is installed by looking for its executable
        // You might need to adjust these paths based on actual installation locations
        string[] possiblePaths = 
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive", "OneDrive.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive", "OneDrive.exe")
        };
        
        // Check if any of the possible paths exist
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Utils.Log($"Verified installation at: {path}");
                return true;
            }
        }
        
        // Additional verification: Check if OneDrive process is running
        try
        {
            var processes = Process.GetProcessesByName("OneDrive");
            if (processes.Length > 0)
            {
                Utils.Log("Verified installation via running process");
                return true;
            }
        }
        catch (Exception ex)
        {
            Utils.Log($"Error checking processes: {ex.Message}", "WARNING");
        }
        
        Utils.Log("Could not verify installation", "WARNING");
        return false;
    }
}