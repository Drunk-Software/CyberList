using CyberList.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CyberList.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ListItem> Items => Set<ListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListItem>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
