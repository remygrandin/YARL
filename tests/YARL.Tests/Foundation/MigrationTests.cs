using Microsoft.EntityFrameworkCore;
using YARL.Infrastructure.Persistence;

namespace YARL.Tests.Foundation;

/// <summary>
/// Verifies EF Core migration applies cleanly on a fresh database.
/// Uses in-memory SQLite to avoid migration lock issues (RESEARCH.md Pitfall 3).
/// </summary>
public class MigrationTests
{
    [Fact]
    public async Task InitialMigrationAppliesCleanly()
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new YarlDbContext(options);
        db.Database.OpenConnection();

        // MigrateAsync applies all pending migrations
        await db.Database.MigrateAsync();

        // Verify the RomSources table was created
        var conn = db.Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='RomSources'";
        var result = cmd.ExecuteScalar();
        Assert.Equal("RomSources", result);
    }

    [Fact]
    public async Task MigrationHistoryTableExists()
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new YarlDbContext(options);
        db.Database.OpenConnection();
        await db.Database.MigrateAsync();

        var conn = db.Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        var result = cmd.ExecuteScalar();
        Assert.Equal("__EFMigrationsHistory", result);
    }
}
