using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CyberList.Data;

/// <summary>
/// Used only by the `dotnet ef` tooling so migrations can be created
/// from the Data project without booting the Avalonia app.
/// The real database path lives in the app's settings, which this project can't see, so this
/// uses an in-memory DB: enough for `migrations add/remove`. The app applies migrations itself
/// at startup, so `dotnet ef database update` is never needed.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AppDbContext(options);
    }
}
