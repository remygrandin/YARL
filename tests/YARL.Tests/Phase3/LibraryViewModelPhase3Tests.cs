using Microsoft.Reactive.Testing;
using System.Reactive.Concurrency;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.UI.ViewModels;

namespace YARL.Tests.Phase3;

[Trait("Category", "Phase3")]
public class LibraryViewModelPhase3Tests
{
    private static Game MakeGameEntity(
        int id,
        string title,
        string platformId = "snes",
        string? genre = null,
        int? releaseYear = null,
        string? developer = null,
        List<string>? aliases = null)
    {
        var game = new Game
        {
            Id = id,
            Title = title,
            RawTitle = title,
            PlatformId = platformId,
            Status = GameStatus.Active,
            Genre = genre,
            ReleaseYear = releaseYear,
            Developer = developer,
            CreatedAt = DateTime.UtcNow,
        };

        if (aliases != null)
        {
            game.Versions = aliases.Select((alias, i) => new GameVersion
            {
                Id = id * 100 + i,
                GameId = id,
                Region = "japan",
                LocalizedTitle = alias,
                Game = game,
            }).ToList();
        }

        return game;
    }

    private static (LibraryViewModel vm, TestScheduler scheduler) CreateVm()
    {
        var scheduler = new TestScheduler();
        var vm = new LibraryViewModel(
            platformRegistry: null,
            scopeFactory: null,
            mainThreadScheduler: scheduler);
        return (vm, scheduler);
    }

    private static void PopulateGames(LibraryViewModel vm)
    {
        // 4 test games as specified in plan:
        // "Super Mario World" (Platform, 1990, Nintendo)
        // "Final Fantasy VI" (RPG, 1994, Square)
        // "Chrono Trigger" (RPG, 1995, Square)
        // "Kirby" with alias "Hoshi no Kirby" (Platform, 1992, HAL)
        var mario = MakeGameEntity(1, "Super Mario World", genre: "Platform", releaseYear: 1990, developer: "Nintendo");
        var ff6 = MakeGameEntity(2, "Final Fantasy VI", genre: "RPG", releaseYear: 1994, developer: "Square");
        var ct = MakeGameEntity(3, "Chrono Trigger", genre: "RPG", releaseYear: 1995, developer: "Square");
        var kirby = MakeGameEntity(4, "Kirby", genre: "Platform", releaseYear: 1992, developer: "HAL",
            aliases: ["Hoshi no Kirby"]);

        vm.AddOrUpdateGame(new GameViewModel(mario));
        vm.AddOrUpdateGame(new GameViewModel(ff6));
        vm.AddOrUpdateGame(new GameViewModel(ct));
        vm.AddOrUpdateGame(new GameViewModel(kirby));
    }

    [Fact]
    public void SearchText_FiltersFilteredGames_ByTitle()
    {
        // LIB-04: Setting SearchText to "Mario" filters FilteredGames to only games containing "Mario" in Title
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.SearchText = "Mario";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

        Assert.Single(vm.FilteredGames);
        Assert.Equal("Super Mario World", vm.FilteredGames[0].Title);

        vm.Dispose();
    }

    [Fact]
    public void SearchText_MatchesAliases()
    {
        // LIB-04: Setting SearchText to "Hoshi" matches a game with Japanese alias containing "Hoshi"
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.SearchText = "Hoshi";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

        Assert.Single(vm.FilteredGames);
        Assert.Equal("Kirby", vm.FilteredGames[0].Title);

        vm.Dispose();
    }

    [Fact]
    public void SearchText_Debounces250ms()
    {
        // LIB-04: Using TestScheduler, verify filter is NOT applied until 250ms after last keystroke
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.SearchText = "Mario";

        // Advance only 200ms — filter should NOT have applied yet
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);
        Assert.Equal(4, vm.FilteredGames.Count);

        // Advance 100 more ms (total 300ms > 250ms debounce) — filter should now apply
        // Extra advance flushes ObserveOn scheduled work items on the TestScheduler
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
        Assert.Single(vm.FilteredGames);

        vm.Dispose();
    }

    [Fact]
    public void GenreFilter_FiltersGamesByGenre()
    {
        // LIB-05: Setting ActiveGenreFilter to "RPG" keeps only RPG games in FilteredGames
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.ActiveGenreFilter = "RPG";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

        Assert.Equal(2, vm.FilteredGames.Count);
        Assert.All(vm.FilteredGames, g => Assert.Equal("RPG", g.Genre));

        vm.Dispose();
    }

    [Fact]
    public void YearFilter_FiltersGamesByYear()
    {
        // LIB-05: Setting year range 1990-1992 keeps only games with ReleaseYear in that range
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.YearFilterMin = 1990;
        vm.YearFilterMax = 1992;
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

        Assert.Equal(2, vm.FilteredGames.Count);
        Assert.All(vm.FilteredGames, g =>
        {
            Assert.True(g.ReleaseYear >= 1990);
            Assert.True(g.ReleaseYear <= 1992);
        });

        vm.Dispose();
    }

    [Fact]
    public void DeveloperFilter_FiltersGamesByDeveloper()
    {
        // LIB-05: Setting ActiveDeveloperFilter to "Nintendo" keeps only Nintendo-developed games
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.ActiveDeveloperFilter = "Nintendo";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

        Assert.Single(vm.FilteredGames);
        Assert.Equal("Super Mario World", vm.FilteredGames[0].Title);

        vm.Dispose();
    }

    [Fact]
    public void MultipleFilters_CombineWithAndLogic()
    {
        // LIB-05: Genre="RPG" + Developer="Square" returns only games matching BOTH criteria
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.ActiveGenreFilter = "RPG";
        vm.ActiveDeveloperFilter = "Square";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

        Assert.Equal(2, vm.FilteredGames.Count);
        Assert.All(vm.FilteredGames, g =>
        {
            Assert.Equal("RPG", g.Genre);
            Assert.Equal("Square", g.Developer);
        });

        vm.Dispose();
    }

    [Fact]
    public void ClearFilters_RestoresFullPlatformList()
    {
        // LIB-05: After setting genre + year, clearing all filters returns all games
        var (vm, scheduler) = CreateVm();
        vm.ShowAllGames = true;
        PopulateGames(vm);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        vm.ActiveGenreFilter = "RPG";
        vm.YearFilterMin = 1994;
        vm.ActiveDeveloperFilter = "Square";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(10).Ticks);

        // Should be filtered to 2 games
        Assert.True(vm.FilteredGames.Count < 4);

        // Clear all filters
        vm.ClearFiltersCommand.Execute().Subscribe();
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

        // All 4 games should be back
        Assert.Equal(4, vm.FilteredGames.Count);

        vm.Dispose();
    }
}
