---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
stopped_at: Completed 04-03-PLAN.md
last_updated: "2026-03-30T20:39:10.402Z"
progress:
  total_phases: 7
  completed_phases: 3
  total_plans: 18
  completed_plans: 17
---

---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
stopped_at: "Checkpoint 03-06 Task 3: human-verify"
last_updated: "2026-03-20T00:21:11.998Z"
progress:
  total_phases: 7
  completed_phases: 3
  total_plans: 13
  completed_plans: 13
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-18)

**Core value:** Users can browse their ROM collection visually and launch any game on any platform with one click, using the right emulator, on both desktop and Steam Deck.
**Current focus:** Phase 04 — emulator-integration-and-launch

## Current Position

Phase: 04 (emulator-integration-and-launch) — EXECUTING
Plan: 1 of 5

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: —
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**

- Last 5 plans: —
- Trend: —

*Updated after each plan completion*
| Phase 01-foundation P01 | 3 min | 2 tasks | 14 files |
| Phase 01-foundation P02 | 12 min | 2 tasks | 14 files |
| Phase 01-foundation P03 | 8 min | 2 tasks | 8 files |
| Phase 02-rom-scanning-and-library P01 | 2 | 2 tasks | 15 files |
| Phase 02-rom-scanning-and-library P02 | 8 | 2 tasks | 15 files |
| Phase 02-rom-scanning-and-library P03 | 8 | 2 tasks | 7 files |
| Phase 02-rom-scanning-and-library P04 | 30 | 2 tasks | 20 files |
| Phase 03-metadata-and-cover-art P01 | 4 | 2 tasks | 10 files |
| Phase 03-metadata-and-cover-art P02 | 9 | 2 tasks | 10 files |
| Phase 03-metadata-and-cover-art P03 | 10 | 2 tasks | 5 files |
| Phase 03-metadata-and-cover-art P04 | 15 | 2 tasks | 6 files |
| Phase 03-metadata-and-cover-art P05 | 15 | 2 tasks | 7 files |
| Phase 03-metadata-and-cover-art P06 | 3 | 2 tasks | 9 files |
| Phase 04-emulator-integration-and-launch P01 | 3 | 2 tasks | 4 files |
| Phase 04-emulator-integration-and-launch P02 | 4 | 2 tasks | 9 files |
| Phase 04-emulator-integration-and-launch P04 | 25 | 2 tasks | 8 files |
| Phase 04-emulator-integration-and-launch P03 | 8 | 2 tasks | 12 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Roadmap]: Two independent view shells (DesktopShell, FullscreenShell) sharing the same ViewModel layer — controller focus management requires this; single adaptive view is not viable
- [Roadmap]: Phase 7 (emulator management + ROM cache) depends on Phase 4, not Phase 6 — can parallelize if needed
- [Roadmap]: Research flags Phase 3 (ScreenScraper API), Phase 4 (Flatpak launch), Phase 6 (Gamescope/Wayland), Phase 7 (cache eviction) for deeper research before implementation
- [Phase 01-foundation]: UseReactiveUIWithMicrosoftDependencyResolver (ReactiveUI.Avalonia.Splat namespace) used in Program.cs; Avalonia.ReactiveUI (old package) intentionally absent
- [Phase 01-foundation]: Program.cs is intentionally a temporary stub — Plan 02 replaces with Generic Host + DI wiring + shell selection
- [Phase 01-foundation]: platforms.json shipped as Content/CopyToOutputDirectory=PreserveNewest (not EmbeddedResource)
- [Phase 01-foundation]: SourceType stored as string in SQLite via HasConversion<string>() for human-readable DB rows
- [Phase 01-foundation P02]: x:DataType=MainViewModel required on shell AXAML files for Avalonia compiled bindings (AvaloniaUseCompiledBindingsByDefault=true)
- [Phase 01-foundation P02]: YarlDbContextFactory (IDesignTimeDbContextFactory) needed because dotnet-ef times out trying to invoke the Avalonia entry point via HostFactoryResolver
- [Phase 01-foundation P02]: LibraryViewModel registered in DI before MainViewModel (constructor dependency order)
- [Phase 01-foundation P03]: In-memory SQLite requires explicit OpenConnection() before EnsureCreated()/MigrateAsync() — connection is per-process lifetime for :memory: databases
- [Phase 01-foundation P03]: ViewModel tests (LibraryViewModel, MainViewModel) require no Avalonia headless infrastructure — ReactiveObject works in plain .NET context
- [Phase 02-rom-scanning-and-library]: GameStatus stored as string via HasConversion<string>() matching SourceType convention for human-readable SQLite rows
- [Phase 02-rom-scanning-and-library]: Unique index on RomFile.FilePath enforces deduplication at DB level; scanner upsert logic relies on this constraint
- [Phase 02-rom-scanning-and-library]: Platform aliases follow EmulationStation/RetroPie folder naming conventions for broad compatibility with existing ROM library structures
- [Phase 02-rom-scanning-and-library]: M3u disc files pre-collected before scan loop to prevent UNIQUE constraint violations on RomFile.FilePath when disc files are both enumerated as standalone ROMs and referenced in playlists
- [Phase 02-rom-scanning-and-library]: ChangeTracker.Clear() required after ExecuteUpdateAsync in RomScannerService — EF Core change tracker retains in-memory state after bulk updates causing stale Active status on subsequent reads
- [Phase 02-rom-scanning-and-library]: CRC32 formatted via GetCurrentHashAsUInt32().ToString(X8) — Convert.ToHexString(GetCurrentHash()) produces wrong endianness with System.IO.Hashing
- [Phase 02-rom-scanning-and-library]: ReactiveUI v23 removed RxApp static class — IScheduler injected as constructor param instead of RxApp.MainThreadScheduler
- [Phase 02-rom-scanning-and-library]: DynamicData Group() used for cache-based grouping (vs GroupOn which operates on list changesets only)
- [Phase 02-rom-scanning-and-library]: RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp() via ModuleInitializer for unit test ReactiveUI bootstrap (ReactiveUI v23 breaking change)
- [Phase 02-rom-scanning-and-library]: ItemsControl+WrapPanel used for grid layouts: ItemsRepeater and UniformGridLayout do not exist in Avalonia 11.3.12
- [Phase 02-rom-scanning-and-library]: MaterialIconKind.GridView does not exist in Material.Icons 3.0.0; ViewGrid is the correct enum value
- [Phase 02-rom-scanning-and-library]: AddRomSourceDialog requires explicit parameterless constructor for AVLN3001 compliance even when instantiated from code-behind
- [Phase 03-metadata-and-cover-art]: ScrapeStatus stored as string via HasConversion<string>() for human-readable SQLite rows, matching GameStatus/SourceType convention
- [Phase 03-metadata-and-cover-art]: GameVersion composite index on (GameId, Region) enables efficient regional variant lookups per game
- [Phase 03-metadata-and-cover-art]: MAME and NGPC have null screenscraperSystemId - no standard ScreenScraper mapping exists for these platforms
- [Phase 03-metadata-and-cover-art]: ScraperPipeline accepts two IMetadataScraper params (not concrete types) to enable NSubstitute mocking in tests
- [Phase 03-metadata-and-cover-art]: FakeMessageHandler sealed inner class pattern used for HttpClient mocking — NSubstitute cannot mock HttpMessageHandler.SendAsync (protected)
- [Phase 03-metadata-and-cover-art]: IGDB FirstReleaseDate is DateTimeOffset? not Unix timestamp — SDK deserializes internally, no manual conversion needed
- [Phase 03-03]: TestScheduler debounce flush margin: advance beyond 250ms to drain ObserveOn queue after Throttle fires
- [Phase 03-03]: IServiceScopeFactory in GameDetailViewModel for scoped DB lifetime — matches existing LibraryViewModel pattern
- [Phase 03-03]: CombineLatest 5-predicate filter: platform + search + genre + developer + year with AND logic via Func<GameViewModel,bool> composition
- [Phase 03-04]: ScraperHostedService registered as AddSingleton + AddHostedService(sp => sp.GetRequired<ScraperHostedService>()) — allows same singleton instance to be resolved directly and as IHostedService
- [Phase 03-04]: RescrapeLibraryCommand uses () => _ = hostedService.QueueAllForRescrapeAsync() fire-and-forget lambda — avoids async void in ReactiveCommand.Create Action callback
- [Phase 03-04]: IConfiguration registered via ConfigurationBuilder.AddEnvironmentVariables() so ScreenScraperClient can read YARL_SS_* credentials without appsettings.json
- [Phase 03-04]: Polly using directive required explicitly for DelayBackoffType/HttpRetryStrategyOptions — not included in Microsoft.Extensions.Http.Resilience implicit usings
- [Phase 03-05]: UniformGridLayout is in Avalonia.Layout namespace (not Avalonia.Controls) in Avalonia.Controls.ItemsRepeater 11.1.5 — requires separate xmlns:irLayout alias pointing to same assembly
- [Phase 03-05]: HasMoreResults/LoadMoreCommand/DisplayLimit added to LibraryViewModel — missing from 03-03 plan but required by GameListView AXAML bindings
- [Phase 03-06]: GameDetailDrawer uses CSS class toggle (drawerOpen) on inner Border for slide animation — TransformOperationsTransition triggers on class change
- [Phase 03-06]: InteractiveSearchDialogModel implements INotifyPropertyChanged directly (not ReactiveObject) to keep dialog self-contained without ReactiveUI dependency
- [Phase 03-06]: ScrapingStatusViewModel injected into SettingsViewModel as nullable optional parameter to preserve testability without DI
- [Phase 04-emulator-integration-and-launch]: Wave 0 stub pattern: #if false/#endif wraps test class body referencing unimplemented types; one compilable Stub_FailsUntilImplemented Fact per file for Category=Phase4 filter
- [Phase 04-emulator-integration-and-launch]: EmulatorConfigTests upgraded to real tests immediately because EmulatorConfig record already existed in AppConfig.cs; GameLaunchServiceTests likewise upgraded since GameLaunchService pre-existed
- [Phase 04-02]: HasDefaultValue(TimeSpan.Zero) required instead of 0L — EF Core design-time rejects non-matching types; migration correctly emits defaultValue: 0L
- [Phase 04-02]: BuildStartInfo and UpdatePlayTimeAsync made public for direct unit testability without process spawning
- [Phase 04-02]: BringToFront P/Invoke guarded by RuntimeInformation.IsOSPlatform(OSPlatform.Windows) for Linux/SteamOS compatibility
- [Phase 04-emulator-integration-and-launch]: EmulatorRowViewModel uses WhenAnyValue reactive subscription for IsPathValid — Flatpak IDs always valid, file paths use File.Exists
- [Phase 04-emulator-integration-and-launch]: AllPlatforms property added to PlatformRegistry as IReadOnlyList<PlatformDefinition> for GetAvailablePlatformsForAdd dropdown
- [Phase 04-emulator-integration-and-launch]: IsVisible='{Binding IsVisible}' on LaunchOverlay in DesktopShell — compiled bindings resolve against sub-VM's DataType when DataContext is re-bound, not parent MainViewModel
- [Phase 04-emulator-integration-and-launch]: DispatcherTimer excluded from unit tests (requires Avalonia headless); only state transitions tested via HandleStateChanged
- [Phase 04-emulator-integration-and-launch]: FindAncestorOfType<DesktopShell>() requires Avalonia.VisualTree namespace; NavigateToSettings() public method added to DesktopShell for cross-component nav

### Pending Todos

None yet.

### Blockers/Concerns

- [Pre-Phase 1]: Avalonia exact version must be verified at project start (dotnet new install Avalonia.Templates)
- [Pre-Phase 1]: ReactiveUI vs CommunityToolkit.Mvvm decision must be made before Phase 1 scaffolding — ReactiveUI IScreen/routing is needed for fullscreen navigation model
- [Pre-Phase 3]: ScreenScraper rate limits and IGDB OAuth token mechanics must be verified before Phase 3 begins
- [Pre-Phase 4]: RetroArch core paths on SteamOS and Flatpak --filesystem flags per emulator must be verified before Phase 4 begins
- [Pre-Phase 6]: Avalonia Wayland/Gamescope support status must be checked before Phase 6 begins

## Session Continuity

Last session: 2026-03-30T20:39:10.399Z
Stopped at: Completed 04-03-PLAN.md
Resume file: None
