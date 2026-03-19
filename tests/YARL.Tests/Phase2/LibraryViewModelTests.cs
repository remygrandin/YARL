using System.Reactive.Concurrency;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.UI.ViewModels;

namespace YARL.Tests.Phase2;

[Trait("Category", "Phase2")]
public class LibraryViewModelTests
{
    // Use Scheduler.Immediate so all reactive pipelines run synchronously in tests.
    // (ReactiveUI v23 removed RxApp as a static class; we inject the scheduler directly.)
    private static LibraryViewModel CreateVm() =>
        new LibraryViewModel(
            platformRegistry: null,
            scopeFactory: null,
            mainThreadScheduler: Scheduler.Immediate);

    private static GameViewModel MakeGame(
        int id,
        string platformId = "snes",
        bool isFavorite = false,
        DateTime? lastPlayedAt = null)
    {
        var game = new Game
        {
            Id = id,
            Title = $"Game {id}",
            RawTitle = $"Game {id}",
            PlatformId = platformId,
            Status = GameStatus.Active,
            IsFavorite = isFavorite,
            LastPlayedAt = lastPlayedAt,
            CreatedAt = DateTime.UtcNow
        };
        return new GameViewModel(game);
    }

    [Fact]
    public void Favorites_toggle_updates_IsFavorite()
    {
        var vm = CreateVm();
        var gvm = MakeGame(1, isFavorite: false);

        vm.AddOrUpdateGame(gvm);

        // Initially favorites collection is empty
        Assert.Empty(vm.Favorites);

        // Toggle favorite on
        gvm.IsFavorite = true;

        // DynamicData AutoRefresh + Filter should now include the game
        Assert.Single(vm.Favorites);
        Assert.Equal(1, vm.Favorites[0].Id);

        // Toggle back off
        gvm.IsFavorite = false;
        Assert.Empty(vm.Favorites);

        vm.Dispose();
    }

    [Fact]
    public void RecentlyPlayed_returns_top15_by_LastPlayedAt()
    {
        var vm = CreateVm();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create 20 games: 15 with LastPlayedAt, 5 with null
        for (int i = 1; i <= 15; i++)
        {
            var gvm = MakeGame(i, lastPlayedAt: baseTime.AddHours(i));
            vm.AddOrUpdateGame(gvm);
        }
        for (int i = 16; i <= 20; i++)
        {
            var gvm = MakeGame(i, lastPlayedAt: null);
            vm.AddOrUpdateGame(gvm);
        }

        // Should have at most 15 items
        Assert.True(vm.RecentlyPlayed.Count <= 15);
        Assert.Equal(15, vm.RecentlyPlayed.Count);

        // First item should have the most recent LastPlayedAt
        var first = vm.RecentlyPlayed[0];
        Assert.Equal(baseTime.AddHours(15), first.LastPlayedAt);

        vm.Dispose();
    }

    [Fact]
    public void RecentlyPlayed_excludes_null_LastPlayedAt()
    {
        var vm = CreateVm();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 3 with LastPlayedAt set, 2 with null
        vm.AddOrUpdateGame(MakeGame(1, lastPlayedAt: baseTime.AddHours(1)));
        vm.AddOrUpdateGame(MakeGame(2, lastPlayedAt: baseTime.AddHours(2)));
        vm.AddOrUpdateGame(MakeGame(3, lastPlayedAt: baseTime.AddHours(3)));
        vm.AddOrUpdateGame(MakeGame(4, lastPlayedAt: null));
        vm.AddOrUpdateGame(MakeGame(5, lastPlayedAt: null));

        Assert.Equal(3, vm.RecentlyPlayed.Count);

        vm.Dispose();
    }

    [Fact]
    public void Platform_grid_excludes_empty_platforms()
    {
        var vm = CreateVm();

        // Add games for snes and nes
        vm.AddOrUpdateGame(MakeGame(1, platformId: "snes"));
        vm.AddOrUpdateGame(MakeGame(2, platformId: "snes"));
        vm.AddOrUpdateGame(MakeGame(3, platformId: "nes"));

        // Both platforms should be present
        var platformIds = vm.Platforms.Select(p => p.Id).ToHashSet();
        Assert.Contains("snes", platformIds);
        Assert.Contains("nes", platformIds);

        // Remove the nes game
        vm.RemoveGame(3);

        // nes platform should no longer appear
        var platformIdsAfter = vm.Platforms.Select(p => p.Id).ToHashSet();
        Assert.Contains("snes", platformIdsAfter);
        Assert.DoesNotContain("nes", platformIdsAfter);

        vm.Dispose();
    }

    [Fact]
    public void Scan_state_properties_update_during_scan()
    {
        var vm = CreateVm();

        // Simulate scan start
        vm.IsScanning = true;
        Assert.True(vm.IsScanning);

        vm.ScanProgressText = "Scanning SNES...";
        Assert.Equal("Scanning SNES...", vm.ScanProgressText);

        // Simulate scan end
        vm.IsScanning = false;
        Assert.False(vm.IsScanning);

        vm.Dispose();
    }
}
