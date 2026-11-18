using CyberList.Data;
using CyberList.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CyberList.Tests;

/// <summary>
/// Runs the real migrations against an in-memory SQLite database, so these tests also catch a
/// broken or missing migration. The connection is kept open: an in-memory database lives only
/// as long as its connection.
/// </summary>
public sealed class AppDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void Migrations_are_up_to_date_with_the_model()
    {
        using var db = new AppDbContext(_options);

        Assert.False(db.Database.HasPendingModelChanges(),
            "The model changed without a migration: run 'dotnet ef migrations add <Name>'.");
    }

    [Fact]
    public async Task Deleting_a_parent_deletes_its_whole_subtree()
    {
        await using (var db = new AppDbContext(_options))
        {
            var root = new ListItem { Title = "root" };
            var child = new ListItem { Title = "child", Parent = root };
            db.Items.AddRange(root, child, new ListItem { Title = "grandchild", Parent = child },
                              new ListItem { Title = "other" });
            await db.SaveChangesAsync();
        }

        // Same kind of delete the app does: straight SQL, nothing loaded, so only the
        // database's ON DELETE CASCADE can remove the children.
        await using (var db = new AppDbContext(_options))
            await db.Items.Where(i => i.Title == "root").ExecuteDeleteAsync();

        await using (var db = new AppDbContext(_options))
            Assert.Equal(["other"], await db.Items.Select(i => i.Title).ToListAsync());
    }
}
