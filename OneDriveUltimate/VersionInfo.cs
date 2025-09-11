using System.Text.Json.Serialization;
 
/// <summary>
/// this is the main type that is serialized to json and stored in the OneDriveUltimate folder
/// It wil act as a container for the version information
/// </summary>
public class VersionInfo
{
    // version number property
    public string Version { get; set; } = string.Empty;
 
    // version date property
    public string VersionDate { get; set; } = string.Empty;
 
    // list of installer paths when the exe is installed to a custom path it will be saved here to be used for installation and uninstallation
    // json ignore so this  data will not be saved to the json file
    [JsonIgnore]
    public List<string> InstallerStoredPaths { get; set; } = new List<string>();
 
    // property to indicate if the install uninstall cycle was successful to indicate if the version was installed and uninstalled successfully
    [JsonIgnore]
    public bool InstallUnInstallCycleSuccess { get; set; } = false;
 
    // override equals to compare version and date used when comparing objects in a list
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
 
        if (obj is VersionInfo other)
        {
            // compare version and date only for now
            return this.VersionDate == other.VersionDate && this.Version == other.Version;
        }
        return false;
    }
 
    // override gethashcode to use in hashset and dictionary to get a unique hash value based on version and date combined
    public override int GetHashCode()
    {
        return HashCode.Combine(VersionDate, Version);
    }
}