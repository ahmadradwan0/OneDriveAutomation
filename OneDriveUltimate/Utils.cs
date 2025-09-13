/// <summary>
/// class used for nethods run Globally across all app classes 
/// </summary>
public static class Utils
{
    /// <summary>
    /// method to log all the changes on the console and in a log file saved besids the EXE file 
    /// </summary>

    public static void Log(string message, string level = "INFO")
    {
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Console.WriteLine(logEntry);
        //File.AppendAllText("onedrive_updater.log", logEntry + Environment.NewLine);
        // Log to a file
        try
        {
            File.AppendAllText(Config.LogFile, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Fallback: log an error to the console if file logging fails
            Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
        }
    }
}