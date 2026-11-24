namespace CyberList.Helpers;

/// <summary>Where the app keeps its local data: the settings file and the default database.</summary>
public static class ConfigurationHelper
{
#if DEBUG
    private const string FolderName = "CyberList-Dev";
    private const string DbName = "cyberlist-dev.db";
#else
    private const string FolderName = "CyberList";
    private const string DbName = "cyberlist.db";
#endif

    /// <summary>
    /// %LOCALAPPDATA%/CyberList (or CyberList-Dev in debug builds): home of all local app data.
    /// Created if missing.
    /// </summary>
    public static string AppDataFolder
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                FolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFilePath => Path.Combine(AppDataFolder, "settings.json");

    /// <summary>Automatic copies made before an import or a move overwrites a database.</summary>
    public static string BackupsFolder => Path.Combine(AppDataFolder, "Backups");

    /// <summary>Used when the settings don't name a database path.</summary>
    public static string DefaultDatabasePath => Path.Combine(AppDataFolder, DbName);
}
