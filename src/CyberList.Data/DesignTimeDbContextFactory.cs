using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CyberList.Data;

/// <summary>
/// Used only by the `dotnet ef` tooling so migrations can be created
/// from the Data project without booting the Avalonia app.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DbPaths.DefaultConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}
