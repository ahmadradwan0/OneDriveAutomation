using System.Text.Json;

/// <summary>
/// Handles storage and retrieval of OneDrive version information using JSON files.
/// Provides methods to read, write, compare, and combine version lists.
/// </summary>
public static class StorageManager
{
    
    // <summary>
    /// Reads a JSON file from the given path and deserializes it into a list of VersionInfo objects List
    /// </summary>
    public static List<VersionInfo> GetStoredVersions(string StorePath)
    {
        var json = File.ReadAllText(StorePath);
        return JsonSerializer.Deserialize<List<VersionInfo>>(json) ?? new List<VersionInfo>();
    }

    /// <summary>
    /// Serializes the provided list of VersionInfo objects and overwrites the configured JSON file.
    /// </summary>
    public static void SaveVersionsOverwrite(List<VersionInfo> versions)
    {

        var json = JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Config.VersionFile, json);
        Utils.Log("Stored versions updated (overwrite).");
    }

    // This methos Not used currently ..  
    public static void SaveVersionsCompareByItem(List<VersionInfo> versions)
    {
        var existingVersionsList = GetStoredVersions(Config.VersionFile);

        //convert existing list to hash set just for searching faster 
        HashSet<VersionInfo> existingVersionsListHASHSET = new HashSet<VersionInfo>(existingVersionsList);

        foreach (var item in versions)
        {
            if (!existingVersionsListHASHSET.Contains(item))
            {
                existingVersionsListHASHSET.Add(item);
                existingVersionsList.Add(item);
                // Keep the set in sync
            }
        }

        SaveVersionsOverwrite(existingVersionsList);

    }

    /// <summary>
    /// Compares two lists of VersionInfo objects and returns the items that are present in the first list but not in the second.
    /// it is used to compare versions online vs the local list
    /// </summary>
    public static List<VersionInfo> CompareByItem(List<VersionInfo> versions, List<VersionInfo> versions2)
    {
        var existingVersionsList = versions2;
        var NewVersions = new List<VersionInfo>();
        //convert existing list to hash set just for searching faster 
        HashSet<VersionInfo> existingVersionsListHASHSET = new HashSet<VersionInfo>(existingVersionsList);

        foreach (var item in versions)
        {
            if (!existingVersionsListHASHSET.Contains(item))
            {
                existingVersionsListHASHSET.Add(item);
                NewVersions.Add(item);
                // Keep the set in sync
            }

        }

        return NewVersions;

    }

    /// <summary>
    /// Combines two lists of VersionInfo objects and removes duplicates.
    /// </summary>
    public static List<VersionInfo> CombineTwoLists(List<VersionInfo> list1, List<VersionInfo> list2)
    {
        var combined = list1.Concat(list2).Distinct().ToList();
        return combined;
    }




}