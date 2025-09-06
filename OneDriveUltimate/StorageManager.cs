using System.Text.Json;

public static class StorageManager
{
    //private static string StorePath = "AllVersions.json";
    //private static string testingPath = "testJson.json";

    public static List<VersionInfo> GetStoredVersions(string StorePath)
    {
        var json = File.ReadAllText(StorePath);
        return JsonSerializer.Deserialize<List<VersionInfo>>(json) ?? new List<VersionInfo>();
    }

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

    // compare to our builtin json
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

    public static List<VersionInfo> CombineTwoLists(List<VersionInfo> list1, List<VersionInfo> list2)
    {
        var combined = list1.Concat(list2).Distinct().ToList();
        return combined;
    }

    


    }