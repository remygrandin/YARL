using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using YARL.Domain.Models;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Launch;
using YARL.Infrastructure.Persistence;

namespace YARL.Tests.Phase4;

[Trait("Category", "Phase4")]
[Trait("Class", "GameLaunchServiceTests")]
public class GameLaunchServiceTests
{
    private static GameLaunchService CreateService(AppConfig? config = null)
    {
        config ??= new AppConfig();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        return new GameLaunchService(config, scopeFactory);
    }

    [Fact]
    public void BuildStartInfo_NativeEmulator_SetsFileNameAndArgs()
    {
        // Arrange
        var service = CreateService();
        var config = new EmulatorConfig
        {
            ExePath = "/usr/bin/retroarch",
            Args = "-L core.so {rompath}",
            IsFlatpak = false
        };
        var romPath = "/home/user/roms/game.sfc";

        // Act
        var psi = service.BuildStartInfo(config, romPath);

        // Assert
        Assert.Equal("/usr/bin/retroarch", psi.FileName);
        Assert.Equal("-L core.so \"/home/user/roms/game.sfc\"", psi.Arguments);
        Assert.False(psi.UseShellExecute);
    }

    [Fact]
    public void BuildStartInfo_Flatpak_SetsFileNameToFlatpakAndRunArgs()
    {
        // Arrange
        var service = CreateService();
        var config = new EmulatorConfig
        {
            ExePath = "org.DolphinEmu.dolphin-emu",
            Args = "{rompath}",
            IsFlatpak = true
        };
        var romPath = "/roms/game.iso";

        // Act
        var psi = service.BuildStartInfo(config, romPath);

        // Assert
        Assert.Equal("flatpak", psi.FileName);
        Assert.Equal("run org.DolphinEmu.dolphin-emu \"/roms/game.iso\"", psi.Arguments);
        Assert.False(psi.UseShellExecute);
    }

    [Fact]
    public void BuildStartInfo_RomPathWithSpaces_IsQuoted()
    {
        // Arrange
        var service = CreateService();
        var config = new EmulatorConfig
        {
            ExePath = "/usr/bin/retroarch",
            Args = "{rompath}",
            IsFlatpak = false
        };
        var romPath = "/home/user/my roms/Super Mario World.sfc";

        // Act
        var psi = service.BuildStartInfo(config, romPath);

        // Assert — full path with spaces must be wrapped in double-quotes
        Assert.Contains("\"/home/user/my roms/Super Mario World.sfc\"", psi.Arguments);
    }

    [Fact]
    public async Task UpdatePlayTimeAsync_AddsToExistingTotalPlayTime()
    {
        // Arrange — in-memory SQLite with shared connection
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var ctx = new YarlDbContext(options))
        {
            await ctx.Database.EnsureCreatedAsync();

            // Seed a RomSource (required FK for Game)
            var source = new RomSource { Path = "/roms", Label = "Test" };
            ctx.RomSources.Add(source);
            await ctx.SaveChangesAsync();

            var game = new Game
            {
                Title = "Test Game",
                PlatformId = "snes",
                SourceId = source.Id,
                TotalPlayTime = TimeSpan.FromMinutes(30)
            };
            ctx.Games.Add(game);
            await ctx.SaveChangesAsync();
        }

        // Wire up a real scope factory using the shared connection
        var scopeFactory = CreateScopeFactory(connection);
        var appConfig = new AppConfig();
        var service = new GameLaunchService(appConfig, scopeFactory);

        // Act
        int gameId;
        using (var ctx = new YarlDbContext(options))
        {
            gameId = await ctx.Games.Select(g => g.Id).FirstAsync();
        }

        await service.UpdatePlayTimeAsync(gameId, TimeSpan.FromMinutes(15));

        // Assert
        using (var ctx = new YarlDbContext(options))
        {
            var game = await ctx.Games.FindAsync(gameId);
            Assert.NotNull(game);
            Assert.Equal(TimeSpan.FromMinutes(45), game!.TotalPlayTime);
            Assert.NotNull(game.LastPlayedAt);
            Assert.True((DateTime.UtcNow - game.LastPlayedAt!.Value).TotalSeconds < 5);
        }

        connection.Dispose();
    }

    [Fact(Skip = "Requires process mocking — stub for now")]
    public void OnProcessExited_NonZeroExitCodeWithinGrace_SetsFailed()
    {
        // Stub — requires process mocking infrastructure
        Assert.Fail("Stub");
    }

    private static IServiceScopeFactory CreateScopeFactory(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite(connection)
            .Options;

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(YarlDbContext))
            .Returns(_ => new YarlDbContext(options));

        return scopeFactory;
    }
}
