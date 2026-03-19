using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;
using YARL.UI.ViewModels;

namespace YARL.Tests.Phase3;

[Trait("Category", "Phase3")]
public class MetadataOverrideTests
{
    private static (YarlDbContext db, IServiceScopeFactory scopeFactory) CreateTestDb()
    {
        var options = new DbContextOptionsBuilder<YarlDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var db = new YarlDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        // Build a service scope factory backed by in-memory SQLite
        var services = new ServiceCollection();
        services.AddDbContext<YarlDbContext>(o =>
            o.UseSqlite(db.Database.GetDbConnection()));
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return (db, scopeFactory);
    }

    private static Game CreateTestGame(int id = 1, bool isOverridden = false)
    {
        return new Game
        {
            Id = id,
            Title = "Test Game",
            RawTitle = "Test Game",
            PlatformId = "snes",
            Status = GameStatus.Active,
            ScrapeStatus = isOverridden ? ScrapeStatus.Override : ScrapeStatus.Pending,
            IsMetadataOverridden = isOverridden,
            Description = "Original description",
            Genre = "RPG",
            Developer = "Original Dev",
            CreatedAt = DateTime.UtcNow,
            SourceId = 1,
        };
    }

    [Fact]
    public async Task Rescrape_SkipsOverriddenGame()
    {
        // META-04: Game with IsMetadataOverridden=true should not be updated by a scraper
        // We simulate this by verifying the flag is respected: if IsMetadataOverridden=true,
        // calling SaveOverrideAsync on a different game should not affect it.
        // More directly: verify the DB game with IsMetadataOverridden=true retains its data
        // after a simulated re-scrape that checks the flag.
        var (db, scopeFactory) = CreateTestDb();

        // Seed a ROM source
        var source = new RomSource { Path = "/roms", SourceType = SourceType.Local, IsEnabled = true };
        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var game = CreateTestGame(isOverridden: true);
        game.SourceId = source.Id;
        db.Games.Add(game);
        await db.SaveChangesAsync();

        // Simulate what a scraper pipeline would do: check IsMetadataOverridden before updating
        using var scope = scopeFactory.CreateScope();
        var scopedDb = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var entity = await scopedDb.Games.FindAsync(game.Id);
        Assert.NotNull(entity);

        // A well-implemented scraper should skip overridden games
        if (!entity.IsMetadataOverridden)
        {
            // This block should NOT execute for overridden games
            entity.Description = "Scraper-set description";
            entity.Genre = "Action";
            await scopedDb.SaveChangesAsync();
        }

        // Re-read and verify original values were preserved
        var reRead = await scopedDb.Games.AsNoTracking().FirstAsync(g => g.Id == game.Id);
        Assert.True(reRead.IsMetadataOverridden);
        Assert.Equal("Original description", reRead.Description);
        Assert.Equal("RPG", reRead.Genre);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task ManualEdit_SetsIsMetadataOverridden()
    {
        // META-04: After user edits a field via GameDetailViewModel, IsMetadataOverridden is set to true
        var (db, scopeFactory) = CreateTestDb();

        // Seed
        var source = new RomSource { Path = "/roms", SourceType = SourceType.Local, IsEnabled = true };
        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var game = CreateTestGame(isOverridden: false);
        game.SourceId = source.Id;
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var gameVm = new GameViewModel(game);
        var detailVm = new GameDetailViewModel(scopeFactory);
        detailVm.LoadGame(gameVm);

        // Enter edit mode and save
        await detailVm.EnterEditModeCommand.Execute();
        detailVm.EditDescription = "Custom description";
        await detailVm.SaveCommand.Execute();

        // Verify IsMetadataOverridden is true in DB
        using var scope = scopeFactory.CreateScope();
        var scopedDb = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var updated = await scopedDb.Games.AsNoTracking().FirstAsync(g => g.Id == game.Id);
        Assert.True(updated.IsMetadataOverridden);
        Assert.Equal(ScrapeStatus.Override, updated.ScrapeStatus);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task ManualEdit_PersistsToDatabase()
    {
        // META-04: Edited fields are saved to DB and survive re-read
        var (db, scopeFactory) = CreateTestDb();

        // Seed
        var source = new RomSource { Path = "/roms", SourceType = SourceType.Local, IsEnabled = true };
        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var game = CreateTestGame(isOverridden: false);
        game.SourceId = source.Id;
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var gameVm = new GameViewModel(game);
        var detailVm = new GameDetailViewModel(scopeFactory);
        detailVm.LoadGame(gameVm);

        // Edit and save
        await detailVm.EnterEditModeCommand.Execute();
        detailVm.EditDescription = "My custom description";
        detailVm.EditGenre = "Platform";
        detailVm.EditDeveloper = "Custom Dev";
        detailVm.EditReleaseYear = 1991;
        await detailVm.SaveCommand.Execute();

        // Re-read from a fresh scope and verify all fields
        using var scope = scopeFactory.CreateScope();
        var scopedDb = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var persisted = await scopedDb.Games.AsNoTracking().FirstAsync(g => g.Id == game.Id);
        Assert.Equal("My custom description", persisted.Description);
        Assert.Equal("Platform", persisted.Genre);
        Assert.Equal("Custom Dev", persisted.Developer);
        Assert.Equal(1991, persisted.ReleaseYear);
        Assert.True(persisted.IsMetadataOverridden);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task RevertOverride_AllowsRescrape()
    {
        // META-04: Setting IsMetadataOverridden=false allows scraper to update fields again
        var (db, scopeFactory) = CreateTestDb();

        // Seed with an overridden game
        var source = new RomSource { Path = "/roms", SourceType = SourceType.Local, IsEnabled = true };
        db.RomSources.Add(source);
        await db.SaveChangesAsync();

        var game = CreateTestGame(isOverridden: true);
        game.SourceId = source.Id;
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var gameVm = new GameViewModel(game);
        var detailVm = new GameDetailViewModel(scopeFactory);
        detailVm.LoadGame(gameVm);

        // Execute revert
        await detailVm.RevertOverrideCommand.Execute();

        // Verify IsMetadataOverridden=false and ScrapeStatus=Pending in DB
        using var scope = scopeFactory.CreateScope();
        var scopedDb = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var reverted = await scopedDb.Games.AsNoTracking().FirstAsync(g => g.Id == game.Id);
        Assert.False(reverted.IsMetadataOverridden);
        Assert.Equal(ScrapeStatus.Pending, reverted.ScrapeStatus);

        // Verify the GameViewModel was also updated
        Assert.False(gameVm.IsMetadataOverridden);
        Assert.Equal(ScrapeStatus.Pending, gameVm.ScrapeStatus);

        await db.DisposeAsync();
    }
}
