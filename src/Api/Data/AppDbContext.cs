using Microsoft.EntityFrameworkCore;

namespace RssReader.Api.Data;

/// <summary>
/// Application database context. No domain entities yet — feed/source schema
/// arrives in Phase 1, Item 2. For now this exists to prove the EF Core + SQLite
/// plumbing (connection, migrations) end-to-end.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
