namespace CyberList.Models;

/// <summary>
/// Everything stored in settings.json. One nested class per section of the Settings page.
/// New properties need a sensible default: older settings files won't contain them.
/// </summary>
public sealed class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
}

public sealed class DatabaseSettings
{
    /// <summary>Full path of the SQLite file. Null means ConfigurationHelper.DefaultDatabasePath.</summary>
    public string? Path { get; set; }
}
