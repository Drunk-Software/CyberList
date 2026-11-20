namespace CyberList.Data;

public static class DbPaths
{
    /// <summary>%LOCALAPPDATA%/CyberList/cyberlist.db (or the platform equivalent).</summary>
    public static string DefaultDatabasePath
    {
        get
        {
#if DEBUG
            var folderName = "CyberList-Dev";
            var dbName = "cyberlist-dev.db";
#else
            var folderName= "CyberList";
            var dbName = "cyberlist.db";
#endif
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                folderName);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, dbName);
        }
    }

    public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";
}
