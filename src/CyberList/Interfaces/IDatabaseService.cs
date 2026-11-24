namespace CyberList.Interfaces;

/// <summary>
/// Owns which SQLite file the app uses, and the operations that swap it: move, open an existing
/// one, import, export. Every change takes effect immediately; contexts created afterwards
/// point at the new file.
/// </summary>
public interface IDatabaseService
{
    /// <summary>Full path of the database file in use now.</summary>
    string CurrentPath { get; }

    /// <summary>Where the database lives when the settings don't name a location.</summary>
    string DefaultPath { get; }

    bool IsUsingDefault { get; }

    /// <summary>
    /// Set when the location in the settings wasn't available at startup (unplugged drive, deleted
    /// folder) and the default database is being used instead. Cleared by the next location change.
    /// </summary>
    string? StartupWarning { get; }

    /// <summary>
    /// After a <see cref="StartupWarning"/>: switch to the location in the settings if it's back
    /// (drive plugged in again). Returns false if it's still missing.
    /// </summary>
    bool TryRestoreConfiguredLocation();

    /// <summary>After a <see cref="StartupWarning"/>: keep the default location for good.</summary>
    void ForgetConfiguredLocation();

    /// <summary>Raised after the app switches to a different file or the file's contents are replaced.</summary>
    event Action? DatabaseChanged;

    /// <summary>The path the database file would have in <paramref name="folder"/>.</summary>
    string DatabasePathIn(string folder);

    bool HasDatabaseIn(string folder);

    /// <summary>
    /// Move the current database (and its SQLite side files) into <paramref name="folder"/> and use
    /// it from there. If a database already exists there, it is only replaced when
    /// <paramref name="overwrite"/> is true, and it is backed up first.
    /// </summary>
    Task MoveToAsync(string folder, bool overwrite);

    /// <summary>Switch to the database already in <paramref name="folder"/>, leaving the current one where it is.</summary>
    Task UseExistingAsync(string folder);

    /// <summary>Write a consistent copy of the current database to <paramref name="filePath"/>.</summary>
    Task ExportAsync(string filePath);

    /// <summary>
    /// Validate <paramref name="filePath"/>, back up the current database, then replace it with the
    /// file and migrate it to the current schema. Returns the backup's path.
    /// </summary>
    /// <exception cref="InvalidDatabaseException">The file isn't a CyberList database this version can open.</exception>
    Task<string> ImportAsync(string filePath);
}

/// <summary>A file that isn't a CyberList database, or one made by a newer version of the app.</summary>
public sealed class InvalidDatabaseException(string message) : Exception(message);
