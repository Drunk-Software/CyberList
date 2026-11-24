using CyberList.Data;
using CyberList.Data.Entities;
using CyberList.Interfaces;
using CyberList.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CyberList.Tests;

/// <summary>Real SQLite files in a temp folder: moving, swapping and importing are all about files.</summary>
public sealed class DatabaseServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "CyberList.Tests", Guid.NewGuid().ToString("N"));
    private readonly JsonSettingsService _settings;
    private readonly DatabaseService _service;

    private string DefaultFolder => Path.Combine(_root, "default");
    private string BackupFolder => Path.Combine(_root, "backups");
    private string Folder(string name) => Path.Combine(_root, name);

    public DatabaseServiceTests()
    {
        Directory.CreateDirectory(DefaultFolder);
        _settings = new JsonSettingsService(Path.Combine(_root, "settings.json"));
        _service = NewService();
        Migrate(_service);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }

    private DatabaseService NewService() =>
        new(_settings, Path.Combine(DefaultFolder, "test.db"), BackupFolder);

    private static void Migrate(IDbContextFactory<AppDbContext> factory)
    {
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }

    private static void AddItem(IDbContextFactory<AppDbContext> factory, string title)
    {
        using var db = factory.CreateDbContext();
        db.Items.Add(new ListItem { Title = title });
        db.SaveChanges();
    }

    private static List<string> Titles(IDbContextFactory<AppDbContext> factory)
    {
        using var db = factory.CreateDbContext();
        return db.Items.OrderBy(i => i.Id).Select(i => i.Title).ToList();
    }

    /// <summary>A separate database file made through its own service, as if by another install.</summary>
    private DatabaseService OtherDatabase(string folderName, params string[] titles)
    {
        var otherSettings = new JsonSettingsService(Path.Combine(_root, folderName + ".json"));
        var other = new DatabaseService(otherSettings, Path.Combine(Folder(folderName), "test.db"), BackupFolder);
        Directory.CreateDirectory(Folder(folderName));
        Migrate(other);
        foreach (var title in titles)
            AddItem(other, title);
        return other;
    }

    // ===== Location ===========================================================

    [Fact]
    public void Starts_on_the_default_path_with_no_warning()
    {
        Assert.True(_service.IsUsingDefault);
        Assert.Null(_service.StartupWarning);
    }

    [Fact]
    public async Task MoveTo_moves_the_file_keeps_the_data_and_saves_the_location()
    {
        AddItem(_service, "a");
        var oldPath = _service.CurrentPath;
        var changed = 0;
        _service.DatabaseChanged += () => changed++;

        await _service.MoveToAsync(Folder("moved"), overwrite: false);

        Assert.Equal(Path.Combine(Folder("moved"), "test.db"), _service.CurrentPath);
        Assert.False(File.Exists(oldPath));
        Assert.Equal(["a"], Titles(_service));
        Assert.Equal(_service.CurrentPath, _settings.Current.Database.Path);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task MoveTo_refuses_to_overwrite_unless_asked()
    {
        OtherDatabase("taken", "theirs");

        await Assert.ThrowsAsync<IOException>(() => _service.MoveToAsync(Folder("taken"), overwrite: false));
        Assert.True(_service.IsUsingDefault);
    }

    [Fact]
    public async Task MoveTo_with_overwrite_backs_up_the_replaced_database()
    {
        OtherDatabase("taken", "theirs");
        AddItem(_service, "mine");

        await _service.MoveToAsync(Folder("taken"), overwrite: true);

        Assert.Equal(["mine"], Titles(_service));
        Assert.Single(Directory.GetFiles(BackupFolder));
    }

    [Fact]
    public async Task Moving_back_to_the_default_clears_the_saved_location()
    {
        await _service.MoveToAsync(Folder("moved"), overwrite: false);

        await _service.MoveToAsync(DefaultFolder, overwrite: false);

        Assert.True(_service.IsUsingDefault);
        Assert.Null(_settings.Current.Database.Path);
    }

    [Fact]
    public async Task UseExisting_switches_to_that_database_and_leaves_the_old_one()
    {
        OtherDatabase("other", "theirs");
        AddItem(_service, "mine");
        var oldPath = _service.CurrentPath;

        await _service.UseExistingAsync(Folder("other"));

        Assert.Equal(["theirs"], Titles(_service));
        Assert.True(File.Exists(oldPath));
    }

    [Fact]
    public async Task UseExisting_rejects_a_file_that_is_not_a_database()
    {
        Directory.CreateDirectory(Folder("junk"));
        await File.WriteAllTextAsync(Path.Combine(Folder("junk"), "test.db"), "hello");

        await Assert.ThrowsAsync<InvalidDatabaseException>(() => _service.UseExistingAsync(Folder("junk")));
        Assert.True(_service.IsUsingDefault);
    }

    [Fact]
    public async Task A_missing_saved_location_falls_back_to_default_with_a_warning()
    {
        await _service.MoveToAsync(Folder("usb"), overwrite: false);
        SqliteConnection.ClearAllPools();
        Directory.Delete(Folder("usb"), recursive: true); // drive unplugged

        var restarted = NewService();

        Assert.True(restarted.IsUsingDefault);
        Assert.NotNull(restarted.StartupWarning);
        Assert.NotNull(_settings.Current.Database.Path); // kept, so Retry can still work
    }

    [Fact]
    public async Task TryRestoreConfiguredLocation_switches_back_once_the_file_returns()
    {
        AddItem(_service, "on usb");
        await _service.MoveToAsync(Folder("usb"), overwrite: false);
        SqliteConnection.ClearAllPools();
        Directory.Move(Folder("usb"), Folder("usb-away"));
        var restarted = NewService();
        Migrate(restarted);

        Assert.False(restarted.TryRestoreConfiguredLocation());
        Directory.Move(Folder("usb-away"), Folder("usb"));
        Assert.True(restarted.TryRestoreConfiguredLocation());

        Assert.Null(restarted.StartupWarning);
        Assert.Equal(["on usb"], Titles(restarted));
    }

    // ===== Export / import ====================================================

    [Fact]
    public async Task Export_then_import_restores_the_data_and_backs_up_first()
    {
        AddItem(_service, "keep me");
        var export = Path.Combine(_root, "export.db");
        await _service.ExportAsync(export);
        AddItem(_service, "added later");

        var backup = await _service.ImportAsync(export);

        Assert.Equal(["keep me"], Titles(_service));
        Assert.True(File.Exists(backup));
        Assert.True(File.Exists(export)); // the user's file is left alone
    }

    [Fact]
    public async Task Export_overwrites_an_existing_file()
    {
        var export = Path.Combine(_root, "export.db");
        await File.WriteAllTextAsync(export, "old");

        await _service.ExportAsync(export);

        Assert.True(new FileInfo(export).Length > 3);
    }

    [Fact]
    public async Task Import_rejects_a_non_sqlite_file_and_keeps_the_current_data()
    {
        AddItem(_service, "mine");
        var junk = Path.Combine(_root, "junk.db");
        await File.WriteAllTextAsync(junk, "definitely not sqlite");

        await Assert.ThrowsAsync<InvalidDatabaseException>(() => _service.ImportAsync(junk));

        Assert.Equal(["mine"], Titles(_service));
    }

    [Fact]
    public async Task Import_rejects_a_sqlite_file_without_the_app_tables()
    {
        var foreign = Path.Combine(_root, "foreign.db");
        await using (var connection = new SqliteConnection($"Data Source={foreign};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Stuff (Id INTEGER)";
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDatabaseException>(() => _service.ImportAsync(foreign));
    }

    [Fact]
    public async Task Import_rejects_a_database_from_a_newer_version()
    {
        var other = OtherDatabase("newer");
        using (var db = other.CreateDbContext())
        {
            db.Database.ExecuteSqlRaw(
                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29991231000000_FromTheFuture', '99.0')");
        }
        SqliteConnection.ClearAllPools();

        var error = await Assert.ThrowsAsync<InvalidDatabaseException>(() => _service.ImportAsync(other.CurrentPath));
        Assert.Contains("NEWER", error.Message);
    }

    [Fact]
    public async Task Import_migrates_a_database_from_an_older_version()
    {
        var oldFile = Path.Combine(_root, "old.db");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={oldFile};Pooling=False").Options;
        string firstMigration;
        await using (var db = new AppDbContext(options))
        {
            firstMigration = db.Database.GetMigrations().First();
            db.GetService<IMigrator>().Migrate(firstMigration);
        }

        await _service.ImportAsync(oldFile);

        await using var current = _service.CreateDbContext();
        Assert.Empty(await current.Database.GetPendingMigrationsAsync());
    }
}
