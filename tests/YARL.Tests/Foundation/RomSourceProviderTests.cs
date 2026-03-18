using Microsoft.EntityFrameworkCore;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;

namespace YARL.Tests.Foundation;

/// <summary>
/// Tests LIB-08: User can tag a path as "remote" (OS-mounted network share).
/// Verifies that RomSource with SourceType.OsMounted can be stored and retrieved.
/// </summary>
public class RomSourceProviderTests
{
    private static YarlDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var context = new YarlDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CanPersistLocalRomSource()
    {
        using var db = CreateInMemoryContext();

        var source = new RomSource
        {
            Path = "/home/user/roms/nes",
            SourceType = SourceType.Local,
            Label = "NES ROMs",
            IsEnabled = true
        };

        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var retrieved = await db.RomSources.FirstAsync(s => s.Path == "/home/user/roms/nes");
        Assert.Equal(SourceType.Local, retrieved.SourceType);
        Assert.Equal("NES ROMs", retrieved.Label);
        Assert.True(retrieved.IsEnabled);
    }

    [Fact]
    public async Task CanPersistOsMountedRomSource_LIB08()
    {
        using var db = CreateInMemoryContext();

        var source = new RomSource
        {
            Path = "/mnt/nas/roms",
            SourceType = SourceType.OsMounted,
            Label = "NAS ROM Share",
            IsEnabled = true
        };

        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var retrieved = await db.RomSources.FirstAsync(s => s.SourceType == SourceType.OsMounted);
        Assert.Equal("/mnt/nas/roms", retrieved.Path);
        Assert.Equal(SourceType.OsMounted, retrieved.SourceType);
        Assert.Equal("NAS ROM Share", retrieved.Label);
    }

    [Fact]
    public async Task SourceTypeStoredAsString()
    {
        using var db = CreateInMemoryContext();

        db.RomSources.Add(new RomSource
        {
            Path = "/mnt/share",
            SourceType = SourceType.OsMounted
        });
        await db.SaveChangesAsync();

        // Verify the value is stored as a string in SQLite (not an int)
        var conn = db.Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SourceType FROM RomSources LIMIT 1";
        var result = cmd.ExecuteScalar();
        Assert.Equal("OsMounted", result);
    }

    [Fact]
    public async Task CanFilterBySourceType()
    {
        using var db = CreateInMemoryContext();

        db.RomSources.AddRange(
            new RomSource { Path = "/local/roms", SourceType = SourceType.Local },
            new RomSource { Path = "/mnt/nas1", SourceType = SourceType.OsMounted },
            new RomSource { Path = "/mnt/nas2", SourceType = SourceType.OsMounted }
        );
        await db.SaveChangesAsync();

        var mounted = await db.RomSources
            .Where(s => s.SourceType == SourceType.OsMounted)
            .ToListAsync();

        Assert.Equal(2, mounted.Count);
        Assert.All(mounted, s => Assert.Equal(SourceType.OsMounted, s.SourceType));
    }
}
