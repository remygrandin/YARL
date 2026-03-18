using Microsoft.EntityFrameworkCore;
using YARL.Domain.Enums;
using YARL.Domain.Interfaces;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Providers;
using YARL.Infrastructure.Scanning;

namespace YARL.Tests.Phase2;

[Trait("Category", "Phase2")]
public class ScannerTests
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
    public async Task Scanner_discovers_roms_from_source()
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

            var report = await scanner.ScanAllAsync(ct: CancellationToken.None);

            Assert.Equal(2, report.GamesAdded);
            Assert.Equal(2, await db.Games.CountAsync());
            Assert.Equal(2, await db.RomFiles.CountAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scanner_reports_progress_via_IProgress()
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

            var updates = new List<ScanUpdate>();
            var progress = new Progress<ScanUpdate>(u => updates.Add(u));

            await scanner.ScanAllAsync(progress, CancellationToken.None);

            Assert.NotEmpty(updates);
            Assert.Contains(updates, u => u.PlatformName == "Super Nintendo");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scanner_respects_cancellation_token()
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

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Should return without throwing
            var report = await scanner.ScanAllAsync(ct: cts.Token);

            Assert.Equal(0, report.GamesAdded);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scanner_skips_unrecognized_extensions()
    {
        var tempDir = CreateTempRomDirectory(
            "SNES/NotARom.txt",
            "SNES/AlsoNotARom.doc");

        try
        {
            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            var registry = BuildRegistry();
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            var report = await scanner.ScanAllAsync(ct: CancellationToken.None);

            Assert.Equal(0, report.GamesAdded);
            Assert.Equal(0, await db.Games.CountAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scanner_handles_m3u_multi_disc_games()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "SNES"));

        try
        {
            // Create disc files
            File.WriteAllText(Path.Combine(tempDir, "SNES", "Disc1.smc"), "DISC1_DATA");
            File.WriteAllText(Path.Combine(tempDir, "SNES", "Disc2.smc"), "DISC2_DATA");

            // Create .m3u playlist referencing both discs (relative paths)
            var m3uPath = Path.Combine(tempDir, "SNES", "Game.m3u");
            File.WriteAllLines(m3uPath, ["Disc1.smc", "Disc2.smc"]);

            using var db = CreateDb();
            var source = new RomSource { Path = tempDir, SourceType = SourceType.Local, IsEnabled = true };
            db.RomSources.Add(source);
            await db.SaveChangesAsync();

            // Registry needs to include .m3u in extensions — scanner allows .m3u regardless
            var platforms = new List<PlatformDefinition>
            {
                new()
                {
                    Id = "snes",
                    Name = "Super Nintendo",
                    Extensions = [".smc", ".sfc"],
                    Aliases = ["SNES"]
                }
            };
            var registry = new PlatformRegistry(platforms);
            IRomSourceProvider[] providers = [new LocalRomSourceProvider()];
            var scanner = new RomScannerService(db, registry, providers);

            await scanner.ScanAllAsync(ct: CancellationToken.None);

            // Expect: 1 game for Game.m3u, 2 games for Disc1.smc and Disc2.smc (scanned as normal roms too)
            // Plus the m3u creates additional RomFiles (disc1 + disc2) under the same game
            // The m3u game has: 1 m3u RomFile + 2 disc RomFiles = 3 RomFiles for that game
            var games = await db.Games.Include(g => g.RomFiles).ToListAsync();
            var m3uGame = games.FirstOrDefault(g => g.RomFiles.Any(r => r.IsM3uPlaylist));
            Assert.NotNull(m3uGame);

            var m3uRomFiles = m3uGame.RomFiles;
            // 1 m3u file + 2 disc references = 3 total
            Assert.Equal(3, m3uRomFiles.Count);
            Assert.Single(m3uRomFiles, r => r.IsM3uPlaylist);
            Assert.Equal(2, m3uRomFiles.Count(r => r.DiscNumber.HasValue));
            Assert.Contains(m3uRomFiles, r => r.DiscNumber == 1);
            Assert.Contains(m3uRomFiles, r => r.DiscNumber == 2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
