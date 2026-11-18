namespace CyberList.Data;

public static class DbPaths
{
    /// <summary>%LOCALAPPDATA%/CyberList/cyberlist.db (or the platform equivalent).</summary>
    public static string DefaultDatabasePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CyberList");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "cyberlist.db");
        }
    }

    public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";
}
