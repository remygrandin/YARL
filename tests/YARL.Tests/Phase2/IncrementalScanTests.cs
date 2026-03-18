using Microsoft.EntityFrameworkCore;
using YARL.Domain.Enums;
using YARL.Domain.Interfaces;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Providers;
using YARL.Infrastructure.Scanning;

namespace YARL.Tests.Phase2;

[Trait("Category", "Phase2")]
public class IncrementalScanTests
{
    private static YarlDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new YarlDbContext(options);
        db.Database.OpenConnection();
        db.Database.MigrateAsync().GetAwaiter().GetResult();
        return db;
    }

    private static PlatformRegistry BuildRegistry()
    {
        var platforms = new List<PlatformDefinition>
        {
            new()
            {
                Id = "snes",
                Name = "Super Nintendo",
                Extensions = [".smc", ".sfc"],
                Aliases = ["SNES", "Super Famicom"]
            }
        };
        return new PlatformRegistry(platforms);
    }

    private static string CreateTempRomDirectory(params string[] relativeFilePaths)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        foreach (var rel in relativeFilePaths)
        {
            var full = Path.Combine(tempDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "ROM_DATA");
        }
        return tempDir;
    }

    [Fact]
    public async Task New_files_are_added_as_Active_games()
    {
        var tempDir = CreateTempRomDirectory(
            "SNES/Game1.smc",
            "SNES/Game2.sfc",
            "SNES/Game3.smc");

        try
        {
            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            var registry = BuildRegistry();
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            await scanner.ScanAllAsync(ct: CancellationToken.None);

            var games = await db.Games.ToListAsync();
            Assert.Equal(3, games.Count);
            Assert.All(games, g => Assert.Equal(GameStatus.Active, g.Status));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Removed_files_are_marked_Missing()
    {
        var tempDir = CreateTempRomDirectory(
            "SNES/Game1.smc",
            "SNES/Game2.sfc");

        try
        {
            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            var registry = BuildRegistry();
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            // First scan: add 2 games
            await scanner.ScanAllAsync(ct: CancellationToken.None);
            Assert.Equal(2, await db.Games.CountAsync());

            // Remove one file from disk then rescan
            File.Delete(Path.Combine(tempDir, "SNES", "Game2.sfc"));
            await scanner.ScanAllAsync(ct: CancellationToken.None);

            var missing = await db.Games.Where(g => g.Status == GameStatus.Missing).ToListAsync();
            Assert.Single(missing);
            Assert.Equal("Game2", missing[0].RawTitle);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Reappeared_files_are_restored_to_Active()
    {
        var tempDir = CreateTempRomDirectory("SNES/Game1.smc");

        try
        {
            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            var registry = BuildRegistry();
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            // First scan: add game
            await scanner.ScanAllAsync(ct: CancellationToken.None);

            // Remove file (marks Missing)
            File.Delete(Path.Combine(tempDir, "SNES", "Game1.smc"));
            await scanner.ScanAllAsync(ct: CancellationToken.None);
            Assert.Equal(GameStatus.Missing, (await db.Games.FirstAsync()).Status);

            // Re-create file (should restore to Active)
            File.WriteAllText(Path.Combine(tempDir, "SNES", "Game1.smc"), "ROM_DATA");
            await scanner.ScanAllAsync(ct: CancellationToken.None);

            Assert.Equal(GameStatus.Active, (await db.Games.FirstAsync()).Status);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Existing_files_are_not_modified()
    {
        var tempDir = CreateTempRomDirectory("SNES/Game1.smc");

        try
        {
            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            var registry = BuildRegistry();
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            // First scan
            await scanner.ScanAllAsync(ct: CancellationToken.None);

            // Set IsFavorite manually
            var game = await db.Games.FirstAsync();
            game.IsFavorite = true;
            await db.SaveChangesAsync();

            // Rescan same files
            await scanner.ScanAllAsync(ct: CancellationToken.None);

            // IsFavorite should still be true (game not re-created)
            var gameAfter = await db.Games.FirstAsync();
            Assert.True(gameAfter.IsFavorite);
            Assert.Equal(1, await db.Games.CountAsync()); // No duplicate
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Duplicate_file_path_rejected_by_unique_index()
    {
        using var db = CreateDb();
        var source = new RomSource { Path = "/roms", SourceType = SourceType.Local, IsEnabled = true };
        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var game = new Game
        {
            Title = "Test Game",
            RawTitle = "Test Game",
            PlatformId = "snes",
            SourceId = source.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var romFile1 = new RomFile
        {
            GameId = game.Id,
            FilePath = "/roms/SNES/Test Game.smc",
            FileName = "Test Game.smc",
            FileSize = 100,
            SourceId = source.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.RomFiles.Add(romFile1);
        await db.SaveChangesAsync();

        // Attempt to insert a second RomFile with the same FilePath
        var romFile2 = new RomFile
        {
            GameId = game.Id,
            FilePath = "/roms/SNES/Test Game.smc", // Duplicate
            FileName = "Test Game.smc",
            FileSize = 100,
            SourceId = source.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.RomFiles.Add(romFile2);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
