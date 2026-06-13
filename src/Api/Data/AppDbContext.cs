using Microsoft.EntityFrameworkCore;
using RssReader.Api.Models;

namespace RssReader.Api.Data;

/// <summary>
/// Application database context. Holds the feed/source schema introduced in
/// Phase 1, Step 2.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasIndex(s => s.FeedUrl).IsUnique();
            entity.Property(s => s.FeedUrl).IsRequired();
            entity.Property(s => s.Title).IsRequired();

            entity.HasMany(s => s.Items)
                .WithOne(i => i.Source)
                .HasForeignKey(i => i.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(i => i.Url).IsRequired();
            entity.Property(i => i.Title).IsRequired();

            // Supports step-3 dedup lookups (find existing item by source + GUID).
            entity.HasIndex(i => new { i.SourceId, i.Guid });

            // Supports triage-filtered item query (GET /api/items?triage=...).
            entity.HasIndex(i => new { i.TriageState, i.SourceId })
                  .HasDatabaseName("IX_Items_TriageState_SourceId");
        });

        // Single-row settings table; Id=1 row seeded here so migrations create it.
        modelBuilder.Entity<AppSettings>()
            .HasData(new AppSettings { Id = 1, LastOpenedUtc = null });
    }
}
