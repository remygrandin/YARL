using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Persistence;

namespace YARL.UI.ViewModels;

public partial class SettingsViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly LibraryViewModel? _libraryVm;
    private readonly AppConfig? _appConfig;
    private readonly AppConfigService? _appConfigService;

    public ScrapingStatusViewModel? ScrapingStatus { get; }

    public ObservableCollection<RomSourceViewModel> RomSources { get; } = new();

    [Reactive] private bool _hasSources;
    [Reactive] private bool _isPurgeConfirmVisible;
    [Reactive] private int _missingGamesCount;

    // Scraper credentials
    [Reactive] private string _screenScraperUser = "";
    [Reactive] private string _screenScraperPass = "";
    [Reactive] private string _igdbClientId = "";
    [Reactive] private string _igdbClientSecret = "";

    public ReactiveCommand<RomSourceViewModel, Unit> RemoveSourceCommand { get; }
    public ReactiveCommand<RomSourceViewModel, Unit> ToggleSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowPurgeConfirmCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelPurgeCommand { get; }
    public ReactiveCommand<Unit, Unit> PurgeMissingCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveScraperCredentialsCommand { get; }

    public SettingsViewModel(
        IServiceScopeFactory? scopeFactory = null,
        LibraryViewModel? libraryVm = null,
        ScrapingStatusViewModel? scrapingStatusVm = null,
        AppConfig? appConfig = null,
        AppConfigService? appConfigService = null)
    {
        _scopeFactory = scopeFactory;
        _libraryVm = libraryVm;
        ScrapingStatus = scrapingStatusVm;
        _appConfig = appConfig;
        _appConfigService = appConfigService;

        // Load current credentials from config
        ScreenScraperUser = appConfig?.ScreenScraperUser ?? "";
        ScreenScraperPass = appConfig?.ScreenScraperPass ?? "";
        IgdbClientId = appConfig?.IgdbClientId ?? "";
        IgdbClientSecret = appConfig?.IgdbClientSecret ?? "";

        RomSources.CollectionChanged += (_, _) => HasSources = RomSources.Count > 0;

        RemoveSourceCommand = ReactiveCommand.CreateFromTask<RomSourceViewModel>(RemoveSourceAsync);
        _disposables.Add(RemoveSourceCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "[SettingsViewModel] RemoveSourceCommand threw")));

        ToggleSourceCommand = ReactiveCommand.CreateFromTask<RomSourceViewModel>(ToggleSourceAsync);
        _disposables.Add(ToggleSourceCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "[SettingsViewModel] ToggleSourceCommand threw")));

        ShowPurgeConfirmCommand = ReactiveCommand.CreateFromTask(LoadMissingCountAndShowConfirmAsync);
        _disposables.Add(ShowPurgeConfirmCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "[SettingsViewModel] ShowPurgeConfirmCommand threw")));

        CancelPurgeCommand = ReactiveCommand.Create(() => { IsPurgeConfirmVisible = false; });

        PurgeMissingCommand = ReactiveCommand.CreateFromTask(PurgeMissingGamesAsync);
        _disposables.Add(PurgeMissingCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "[SettingsViewModel] PurgeMissingCommand threw")));

        SaveScraperCredentialsCommand = ReactiveCommand.Create(SaveScraperCredentials);
    }

    public async Task LoadSourcesAsync()
    {
        Log.Debug("[SettingsViewModel] LoadSourcesAsync called");
        if (_scopeFactory is null)
        {
            Log.Warning("[SettingsViewModel] _scopeFactory is null, cannot load sources");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var sources = await db.RomSources.ToListAsync();
        Log.Debug("[SettingsViewModel] Loaded {Count} ROM sources from DB", sources.Count);

        RomSources.Clear();
        foreach (var s in sources)
            RomSources.Add(new RomSourceViewModel(s));
    }

    /// <summary>
    /// Saves a new ROM source to the database and adds it to the local collection.
    /// Called by AddRomSourceDialog via delegate.
    /// Returns false if the scope factory is unavailable.
    /// </summary>
    public async Task<bool> AddSourceAsync(string path, SourceType sourceType)
    {
        Log.Information("[SettingsViewModel] AddSourceAsync path={Path} type={Type}", path, sourceType);
        if (_scopeFactory is null)
        {
            Log.Warning("[SettingsViewModel] _scopeFactory is null, cannot add source");
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var entity = new RomSource { Path = path, SourceType = sourceType, IsEnabled = true };
        db.RomSources.Add(entity);
        await db.SaveChangesAsync();
        RomSources.Add(new RomSourceViewModel(entity));
        Log.Information("[SettingsViewModel] Added ROM source id={Id}", entity.Id);
        return true;
    }

    private async Task RemoveSourceAsync(RomSourceViewModel vm)
    {
        Log.Information("[SettingsViewModel] RemoveSourceAsync id={Id} path={Path}", vm.Id, vm.Path);
        if (_scopeFactory is null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var entity = await db.RomSources.FindAsync(vm.Id);
        if (entity is null)
        {
            Log.Warning("[SettingsViewModel] ROM source id={Id} not found in DB", vm.Id);
            return;
        }

        db.RomSources.Remove(entity);
        await db.SaveChangesAsync();
        RomSources.Remove(vm);
        Log.Information("[SettingsViewModel] Removed ROM source id={Id}", vm.Id);
    }

    private async Task ToggleSourceAsync(RomSourceViewModel vm)
    {
        Log.Information("[SettingsViewModel] ToggleSourceAsync id={Id} currentEnabled={IsEnabled}", vm.Id, vm.IsEnabled);
        if (_scopeFactory is null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var entity = await db.RomSources.FindAsync(vm.Id);
        if (entity is null) return;

        entity.IsEnabled = !entity.IsEnabled;
        await db.SaveChangesAsync();
        vm.IsEnabled = entity.IsEnabled;
        Log.Information("[SettingsViewModel] Toggled ROM source id={Id} enabled={Enabled}", vm.Id, vm.IsEnabled);
    }

    private async Task LoadMissingCountAndShowConfirmAsync()
    {
        if (_scopeFactory is null) { IsPurgeConfirmVisible = true; return; }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        MissingGamesCount = await db.Games
            .CountAsync(g => g.Status == GameStatus.Missing);
        Log.Debug("[SettingsViewModel] Missing games count: {Count}", MissingGamesCount);
        IsPurgeConfirmVisible = true;
    }

    private async Task PurgeMissingGamesAsync()
    {
        Log.Information("[SettingsViewModel] PurgeMissingGamesAsync — purging missing games");
        if (_scopeFactory is null) { IsPurgeConfirmVisible = false; return; }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        var missing = await db.Games
            .Where(g => g.Status == GameStatus.Missing)
            .ToListAsync();

        db.Games.RemoveRange(missing);
        await db.SaveChangesAsync();
        Log.Information("[SettingsViewModel] Purged {Count} missing games", missing.Count);

        // Remove from live SourceCache so UI updates immediately
        if (_libraryVm is not null)
            foreach (var g in missing)
                _libraryVm.RemoveGame(g.Id);

        IsPurgeConfirmVisible = false;
    }

    private void SaveScraperCredentials()
    {
        if (_appConfig is null || _appConfigService is null) return;
        _appConfig.ScreenScraperUser = string.IsNullOrWhiteSpace(ScreenScraperUser) ? null : ScreenScraperUser;
        _appConfig.ScreenScraperPass = string.IsNullOrWhiteSpace(ScreenScraperPass) ? null : ScreenScraperPass;
        _appConfig.IgdbClientId = string.IsNullOrWhiteSpace(IgdbClientId) ? null : IgdbClientId;
        _appConfig.IgdbClientSecret = string.IsNullOrWhiteSpace(IgdbClientSecret) ? null : IgdbClientSecret;
        _appConfigService.Save(_appConfig);
        Log.Information("[SettingsViewModel] Scraper credentials saved");
    }

    public void Dispose() => _disposables.Dispose();
}
