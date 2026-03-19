using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Scanning;

namespace YARL.UI.ViewModels;

/// <summary>
/// Shared ViewModel consumed by both DesktopShell and FullscreenShell.
/// Acts as the live data hub: SourceCache receives scanner results,
/// derived collections update reactively.
/// </summary>
public partial class LibraryViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly SourceCache<GameViewModel, int> _gamesSource = new(g => g.Id);
    private readonly IScheduler _mainThreadScheduler;

    // Bound to UI
    private ReadOnlyObservableCollection<GameViewModel> _allGames = new([]);
    public ReadOnlyObservableCollection<GameViewModel> AllGames => _allGames;

    private ReadOnlyObservableCollection<PlatformViewModel> _platforms = new([]);
    public ReadOnlyObservableCollection<PlatformViewModel> Platforms => _platforms;

    private ReadOnlyObservableCollection<GameViewModel> _recentlyPlayed = new([]);
    public ReadOnlyObservableCollection<GameViewModel> RecentlyPlayed => _recentlyPlayed;

    private ReadOnlyObservableCollection<GameViewModel> _favorites = new([]);
    public ReadOnlyObservableCollection<GameViewModel> Favorites => _favorites;

    // Scan state
    [Reactive] private string _statusMessage = "YARL is ready. Add ROM folders to get started.";
    [Reactive] private bool _isScanning;
    [Reactive] private string _scanProgressText = "";

    // Selected state
    [Reactive] private PlatformViewModel? _selectedPlatform;

    // Game list filter state
    [Reactive] private bool _showFavoritesOnly;

    // Computed visibility helpers for LibraryView (reactive backing fields)
    [Reactive] private bool _isEmptyState = true;
    [Reactive] private bool _isScanningEmpty;
    [Reactive] private bool _hasPlatforms;
    [Reactive] private bool _hasRecentlyPlayed;
    [Reactive] private bool _hasFavorites;

    // Filtered games for the currently selected platform (GameListView binding)
    private ReadOnlyObservableCollection<GameViewModel> _filteredGames = new([]);
    public ReadOnlyObservableCollection<GameViewModel> FilteredGames => _filteredGames;

    // Commands
    public ReactiveCommand<Unit, Unit> RescanCommand { get; }
    public ReactiveCommand<GameViewModel, Unit> ToggleFavoriteCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelScanCommand { get; }

    // Dependencies for rescan
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly PlatformRegistry? _platformRegistry;
    private CancellationTokenSource? _scanCts;

    public LibraryViewModel(
        PlatformRegistry? platformRegistry = null,
        IServiceScopeFactory? scopeFactory = null,
        IScheduler? mainThreadScheduler = null)
    {
        _platformRegistry = platformRegistry;
        _scopeFactory = scopeFactory;
        _mainThreadScheduler = mainThreadScheduler ?? CurrentThreadScheduler.Instance;

        // AllGames pipeline
        _disposables.Add(
            _gamesSource.Connect()
                .ObserveOn(_mainThreadScheduler)
                .Bind(out _allGames)
                .Subscribe());

        // Platforms pipeline: group by PlatformId.
        // DynamicData removes empty groups automatically when all items leave a group.
        // Transform passes (group, groupKey) so we can access the platformId.
        _disposables.Add(
            _gamesSource.Connect()
                .Group<GameViewModel, int, string>(g => g.PlatformId)
                .Transform<PlatformViewModel, IGroup<GameViewModel, int, string>, string>((group, groupKey) =>
                {
                    var platformName = _platformRegistry?.Resolve(groupKey)?.Name ?? groupKey;
                    var vm = new PlatformViewModel(groupKey, platformName);

                    // Track game count reactively from the group's inner cache
                    _disposables.Add(
                        group.Cache.CountChanged
                            .ObserveOn(_mainThreadScheduler)
                            .Subscribe(count => vm.GameCount = count));

                    return vm;
                })
                .SortBy(vm => vm.Name)
                .ObserveOn(_mainThreadScheduler)
                .Bind(out _platforms)
                .Subscribe());

        // RecentlyPlayed pipeline: top 15 by LastPlayedAt descending, excluding nulls
        var lastPlayedComparer = SortExpressionComparer<GameViewModel>
            .Descending(g => g.LastPlayedAt ?? DateTime.MinValue);

        _disposables.Add(
            _gamesSource.Connect()
                .AutoRefresh(g => g.LastPlayedAt)
                .Filter(g => g.LastPlayedAt != null)
                .Top(lastPlayedComparer, 15)
                .ObserveOn(_mainThreadScheduler)
                .Bind(out _recentlyPlayed)
                .Subscribe());

        // Favorites pipeline
        _disposables.Add(
            _gamesSource.Connect()
                .AutoRefresh(g => g.IsFavorite)
                .Filter(g => g.IsFavorite)
                .ObserveOn(_mainThreadScheduler)
                .Bind(out _favorites)
                .Subscribe());

        // FilteredGames pipeline: games for selected platform, optionally filtered by favorites
        var platformFilter = this.WhenAnyValue(x => x.SelectedPlatform, x => x.ShowFavoritesOnly)
            .Select(t => BuildPlatformFilter(t.Item1, t.Item2));

        _disposables.Add(
            _gamesSource.Connect()
                .AutoRefresh(g => g.IsFavorite)
                .Filter(platformFilter)
                .SortBy(g => g.Title)
                .ObserveOn(_mainThreadScheduler)
                .Bind(out _filteredGames)
                .Subscribe());

        // Commands
        var canRescan = this.WhenAnyValue(x => x.IsScanning).Select(s => !s);
        RescanCommand = ReactiveCommand.CreateFromTask(RescanAsync, canRescan);

        ToggleFavoriteCommand = ReactiveCommand.Create<GameViewModel>(gvm =>
        {
            gvm.IsFavorite = !gvm.IsFavorite;
        });

        var canCancel = this.WhenAnyValue(x => x.IsScanning);
        CancelScanCommand = ReactiveCommand.Create(() => _scanCts?.Cancel(), canCancel);

        // Wire visibility computed properties from collection count changes + scanning state
        _disposables.Add(
            _gamesSource.Connect()
                .ObserveOn(_mainThreadScheduler)
                .Subscribe(_ =>
                {
                    var pc = Platforms.Count;
                    var scanning = IsScanning;
                    HasPlatforms = pc > 0;
                    IsEmptyState = pc == 0 && !scanning;
                    IsScanningEmpty = pc == 0 && scanning;
                    HasRecentlyPlayed = RecentlyPlayed.Count > 0;
                    HasFavorites = Favorites.Count > 0;
                }));

        _disposables.Add(
            this.WhenAnyValue(x => x.IsScanning)
                .ObserveOn(_mainThreadScheduler)
                .Subscribe(scanning =>
                {
                    var pc = Platforms.Count;
                    IsEmptyState = pc == 0 && !scanning;
                    IsScanningEmpty = pc == 0 && scanning;
                }));
    }

    private async Task RescanAsync()
    {
        if (_scopeFactory is null)
            return;

        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        ScanProgressText = "Scanning ROM library...";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<RomScannerService>();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

            var progress = new Progress<ScanUpdate>(update =>
            {
                if (update.IsComplete)
                    ScanProgressText = $"Scan complete: {update.GamesFound} games found.";
                else
                    ScanProgressText = $"Scanning {update.PlatformName}: {update.TotalProcessed} files processed...";
            });

            var report = await scanner.ScanAllAsync(progress, _scanCts.Token);

            await LoadGamesFromDbAsync(db);

            StatusMessage = $"Library ready. {report.GamesAdded} games discovered.";
            ScanProgressText = $"Scan complete. {report.GamesAdded} games added, {report.GamesRemoved} removed.";
        }
        catch (OperationCanceledException)
        {
            ScanProgressText = "Scan cancelled.";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    /// <summary>
    /// Load all Active games from DB into the SourceCache.
    /// Called by RomScanHostedService after initial scan completes.
    /// </summary>
    public async Task LoadGamesFromDbAsync(YarlDbContext db)
    {
        var games = await db.Games
            .Where(g => g.Status == GameStatus.Active)
            .ToListAsync();

        var viewModels = games.Select(g => new GameViewModel(g)).ToList();
        _gamesSource.AddOrUpdate(viewModels);
    }

    /// <summary>
    /// Public accessor for SourceCache AddOrUpdate — used by scanner to push live updates.
    /// </summary>
    public void AddOrUpdateGame(GameViewModel gvm)
    {
        _gamesSource.AddOrUpdate(gvm);
    }

    /// <summary>
    /// Remove a game from the SourceCache (e.g., when marked Missing/Hidden).
    /// </summary>
    public void RemoveGame(int gameId)
    {
        _gamesSource.Remove(gameId);
    }

    /// <summary>
    /// Run EF Core migrations using the injected scope factory.
    /// Called once at app startup from App.axaml.cs.
    /// Avoids relying on Splat to resolve framework-internal IServiceScopeFactory.
    /// </summary>
    internal void RunMigration()
    {
        if (_scopeFactory is null) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        db.Database.Migrate();
    }

    /// <summary>
    /// Persist a new ROM source to the database.
    /// Returns false if the scope factory is unavailable.
    /// Called by AddRomSourceDialog via delegate — avoids Splat resolving IServiceScopeFactory.
    /// </summary>
    public async Task<bool> AddRomSourceAsync(string path, SourceType sourceType)
    {
        if (_scopeFactory is null) return false;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        db.RomSources.Add(new RomSource { Path = path, SourceType = sourceType, IsEnabled = true });
        await db.SaveChangesAsync();
        return true;
    }

    private static Func<GameViewModel, bool> BuildPlatformFilter(PlatformViewModel? platform, bool favoritesOnly)
    {
        return g =>
        {
            if (platform is null) return false;
            if (g.PlatformId != platform.Id) return false;
            if (favoritesOnly && !g.IsFavorite) return false;
            return true;
        };
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _scanCts?.Dispose();
        _gamesSource.Dispose();
    }
}
