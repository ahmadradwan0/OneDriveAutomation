using System.Text.Json.Serialization;

public class VersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string VersionDate { get; set; } = string.Empty;

    [JsonIgnore]
    public List<string> InstallerStoredPaths { get; set; } = new List<string>();


    [JsonIgnore]
    public bool InstallUnInstallCycleSuccess { get; set; } = false;

    public override bool Equals(object? obj)
    {
         if (obj is null)
        {
            return false;
        }
        
        if (obj is VersionInfo other)
        {
            return this.VersionDate == other.VersionDate && this.Version == other.Version;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VersionDate, Version);
    }
}