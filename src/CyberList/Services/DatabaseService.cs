using CyberList.Data;
using CyberList.Data.Entities;
using CyberList.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CyberList.Services;

/// <summary>
/// The app's <see cref="IDbContextFactory{AppDbContext}"/>: every context it creates points at
/// <see cref="CurrentPath"/>, so a location change applies to the very next query.
/// </summary>
public sealed class DatabaseService : IDatabaseService, IDbContextFactory<AppDbContext>
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Files SQLite may keep next to the database. -wal and -journal can hold data that isn't in the
    /// main file yet (after a crash), so they move with it. -shm is only an index that SQLite rebuilds.
    /// </summary>
    private static readonly string[] DataSideFiles = ["-wal", "-journal"];
    private static readonly string[] AllSideFiles = ["-wal", "-journal", "-shm"];

    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    private readonly ISettingsService _settings;
    private readonly string _backupFolder;
    private DbContextOptions<AppDbContext> _options = null!;

    public string CurrentPath { get; private set; } = null!;
    public string DefaultPath { get; }
    public bool IsUsingDefault => SamePath(CurrentPath, DefaultPath);
    public string? StartupWarning { get; private set; }

    public event Action? DatabaseChanged;

    public DatabaseService(ISettingsService settings, string defaultPath, string backupFolder)
    {
        _settings = settings;
        DefaultPath = Path.GetFullPath(defaultPath);
        _backupFolder = backupFolder;

        var configured = settings.Current.Database.Path;
        if (configured is not null && !File.Exists(configured))
        {
            // Don't touch the settings: the drive may just be unplugged, and Retry should still work.
            StartupWarning = $"DATABASE NOT FOUND AT {configured}. USING THE DEFAULT LOCATION FOR NOW.";
            configured = null;
        }
        SetPath(configured ?? DefaultPath);
    }

    public AppDbContext CreateDbContext() => new(_options);

    public bool TryRestoreConfiguredLocation()
    {
        var configured = _settings.Current.Database.Path;
        if (StartupWarning is null || configured is null || !File.Exists(configured))
            return false;

        SetPath(configured);
        StartupWarning = null;
        using (var db = CreateDbContext())
            db.Database.Migrate();
        DatabaseChanged?.Invoke();
        return true;
    }

    public void ForgetConfiguredLocation()
    {
        StartupWarning = null;
        _settings.Update(s => s.Database.Path = null);
    }

    public string DatabasePathIn(string folder) =>
        Path.GetFullPath(Path.Combine(folder, Path.GetFileName(DefaultPath)));

    public bool HasDatabaseIn(string folder) => File.Exists(DatabasePathIn(folder));

    public async Task MoveToAsync(string folder, bool overwrite)
    {
        var source = CurrentPath;
        var target = DatabasePathIn(folder);
        if (SamePath(source, target))
            return;

        if (File.Exists(target) && !overwrite)
            throw new IOException($"A DATABASE ALREADY EXISTS AT {target}.");

        ReleaseFileLocks();
        await Task.Run(() =>
        {
            Directory.CreateDirectory(folder);
            if (File.Exists(target))
            {
                File.Copy(target, NewBackupPath(target));
                DeleteWithSideFiles(target);
            }
            // Copy first, delete last: if anything fails halfway, the original is still intact.
            File.Copy(source, target);
            foreach (var suffix in DataSideFiles)
            {
                if (File.Exists(source + suffix))
                    File.Copy(source + suffix, target + suffix);
            }
        });

        SwitchTo(target);
        TryDeleteWithSideFiles(source);
        DatabaseChanged?.Invoke();
    }

    public async Task UseExistingAsync(string folder)
    {
        var target = DatabasePathIn(folder);
        await ValidateAsync(target);

        SwitchTo(target);
        await MigrateAsync();
        DatabaseChanged?.Invoke();
    }

    public async Task ExportAsync(string filePath)
    {
        // VACUUM INTO refuses to overwrite; the save dialog already asked the user.
        File.Delete(filePath);
        await using var db = CreateDbContext();
        // Unlike a file copy, this is consistent even while other connections are open.
        await db.Database.ExecuteSqlRawAsync("VACUUM INTO {0}", Path.GetFullPath(filePath));
    }

    public async Task<string> ImportAsync(string filePath)
    {
        // Work on a private copy: the user's file is never opened for writing, and can't
        // change between validating and swapping it in.
        var staged = CurrentPath + ".import";
        File.Copy(filePath, staged, overwrite: true);
        try
        {
            await ValidateAsync(staged);
            var backup = await BackupCurrentAsync();

            ReleaseFileLocks();
            DeleteWithSideFiles(CurrentPath);
            File.Move(staged, CurrentPath);

            await MigrateAsync();
            DatabaseChanged?.Invoke();
            return backup;
        }
        finally
        {
            File.Delete(staged);
        }
    }

    /// <summary>Checks that the file is SQLite, holds the app's tables, and has no migrations this version doesn't know.</summary>
    private static async Task ValidateAsync(string path)
    {
        if (!File.Exists(path))
            throw new InvalidDatabaseException($"NO DATABASE AT {path}.");

        var header = new byte[SqliteHeader.Length];
        // ReadWrite share: the file may still be open in SQLite (a pooled connection), which is fine for a peek.
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            if (read < header.Length || !header.AsSpan().SequenceEqual(SqliteHeader))
                throw new InvalidDatabaseException("NOT A SQLITE DATABASE.");
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false, // don't keep the file locked after the check
        }.ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options;
        await using var db = new AppDbContext(options);

        var itemsTable = db.Model.FindEntityType(typeof(ListItem))!.GetTableName()!;
        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();
        if (!tables.Contains("__EFMigrationsHistory") || !tables.Contains(itemsTable))
            throw new InvalidDatabaseException("NOT A CYBERLIST DATABASE.");

        var known = db.Database.GetMigrations().ToHashSet();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        if (applied.Any(m => !known.Contains(m)))
            throw new InvalidDatabaseException("THIS DATABASE IS FROM A NEWER VERSION OF CYBERLIST. UPDATE THE APP TO OPEN IT.");
    }

    private async Task MigrateAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>A consistent copy of the database in use, via VACUUM INTO. Returns its path.</summary>
    private async Task<string> BackupCurrentAsync()
    {
        var backup = NewBackupPath(CurrentPath);
        await ExportAsync(backup);
        return backup;
    }

    private string NewBackupPath(string databasePath)
    {
        Directory.CreateDirectory(_backupFolder);
        var name = $"{Path.GetFileNameWithoutExtension(databasePath)}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.db";
        return Path.Combine(_backupFolder, name);
    }

    private void SwitchTo(string path)
    {
        SetPath(path);
        StartupWarning = null;
        _settings.Update(s => s.Database.Path = IsUsingDefault ? null : CurrentPath);
    }

    private void SetPath(string path)
    {
        CurrentPath = Path.GetFullPath(path);
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = CurrentPath }.ToString())
            .Options;
    }

    /// <summary>
    /// Microsoft.Data.Sqlite pools connections, and a pooled connection keeps the file open
    /// (and locked, on Windows) after its context is disposed.
    /// </summary>
    private static void ReleaseFileLocks() => SqliteConnection.ClearAllPools();

    private static void DeleteWithSideFiles(string path)
    {
        File.Delete(path);
        foreach (var suffix in AllSideFiles)
            File.Delete(path + suffix);
    }

    /// <summary>The move already succeeded; a leftover old file isn't worth failing it over.</summary>
    private static void TryDeleteWithSideFiles(string path)
    {
        try
        {
            DeleteWithSideFiles(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool SamePath(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), PathComparison);
}
