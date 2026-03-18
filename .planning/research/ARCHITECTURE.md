# Architecture Research

**Domain:** Cross-platform ROM browser / manager / launcher (.NET + Avalonia UI)
**Researched:** 2026-03-18
**Confidence:** MEDIUM
**Note:** Network tools unavailable during research. Patterns drawn from training knowledge of open-source launchers (EmulationStation, Pegasus Frontend, Playnite, LaunchBox) and .NET desktop application conventions. Confidence is MEDIUM because specific implementation details of named projects could not be verified live. The component pattern itself is well-established in this domain.

---

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Presentation Layer                          │
│                                                                      │
│  ┌──────────────────────┐     ┌──────────────────────────────────┐  │
│  │   Desktop Mode UI    │     │   Fullscreen / Gamepad Mode UI   │  │
│  │  (Grid, List, Search,│     │  (Big Art, D-pad nav, console-   │  │
│  │   Sidebar, Filters)  │     │   like browsing)                 │  │
│  └──────────┬───────────┘     └────────────────┬─────────────────┘  │
│             │                                  │                     │
│             └─────────────────┬────────────────┘                     │
│                               │ Avalonia UI (MVVM)                  │
├───────────────────────────────┼─────────────────────────────────────┤
│                          Application Layer                           │
│                                                                      │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────────┐  │
│  │  Library   │  │  Metadata  │  │  Emulator  │  │    Cache      │  │
│  │  Service   │  │  Service   │  │  Manager   │  │   Manager     │  │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └──────┬────────┘  │
│        │               │               │                 │           │
├────────┼───────────────┼───────────────┼─────────────────┼───────────┤
│                          Domain / Core Layer                         │
│                                                                      │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────────┐  │
│  │    ROM     │  │  Platform  │  │  Emulator  │  │  Collection   │  │
│  │  Scanner   │  │  Registry  │  │  Registry  │  │   Engine      │  │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └──────┬────────┘  │
│        │               │               │                 │           │
├────────┼───────────────┼───────────────┼─────────────────┼───────────┤
│                     Infrastructure / Provider Layer                  │
│                                                                      │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────────┐  │
│  │  Storage   │  │  Metadata  │  │  Emulator  │  │  ROM Source   │  │
│  │ Providers  │  │  Scrapers  │  │ Installers │  │  Providers    │  │
│  │(Local/Mount│  │(ScreenScrp/│  │(Win/Linux) │  │(Local/Mount,  │  │
│  │  /Cache)   │  │ IGDB/TGDB) │  │            │  │ extensible)   │  │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └──────┬────────┘  │
├────────┼───────────────┼───────────────┼─────────────────┼───────────┤
│                          Persistence Layer                           │
│                                                                      │
│  ┌───────────────────┐  ┌─────────────────┐  ┌────────────────────┐  │
│  │   SQLite / LiteDB │  │   File Cache    │  │   Config / TOML    │  │
│  │   (ROM library,   │  │  (Cover art,    │  │   (App settings,   │  │
│  │    metadata,      │  │   cached ROMs)  │  │    emulator defs,  │  │
│  │    play history)  │  │                 │  │    platform defs)  │  │
│  └───────────────────┘  └─────────────────┘  └────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| Desktop Mode UI | Grid/list browsing, search, sidebar, filters | Avalonia MVVM Views + ReactiveUI |
| Fullscreen Mode UI | Console-like big-art browsing, D-pad navigation | Separate Avalonia Views, same ViewModels |
| Library Service | Orchestrates the game library — add/remove/query ROMs | Application service, coordinates Scanner + DB |
| Metadata Service | Fetch/update/override metadata and cover art | Orchestrates scraper providers, stores results |
| Emulator Manager | Install, configure, and invoke emulators | Platform-aware, provider per OS |
| Cache Manager | Download/copy remote ROMs locally, enforce disk limits, LRU eviction | Background service, tracks access times |
| ROM Scanner | Walk filesystem paths, identify ROM files, compute hashes | Background pipeline, produces RomFile events |
| Platform Registry | Known platform definitions (extensions, names, recommended emulator) | Config-driven data, ships with app |
| Emulator Registry | Known emulator definitions (install source, CLI args, supported platforms) | Config-driven data, ships with app |
| Collection Engine | Manage user collections, favorites, play history, recommendations | Service operating on ROM library data |
| Storage Providers | Abstract local path vs. OS-mounted path vs. future cloud | `IStorageProvider` interface |
| Metadata Scrapers | ScreenScraper, IGDB, TheGamesDB adapters | `IMetadataScraper` interface per source |
| Emulator Installers | Windows (direct download/7zip) and Linux (Flatpak/AppImage/native) | `IEmulatorInstaller` interface per OS |
| ROM Source Providers | Enumerate ROMs from a source location | `IRomSourceProvider` interface |

---

## Recommended Project Structure

```
src/
├── Yarl.App/                    # Entry point, DI composition root, startup
│   ├── Program.cs
│   └── App.axaml / App.axaml.cs
│
├── Yarl.UI/                     # All Avalonia views and view models
│   ├── Desktop/                 # Desktop mode views
│   │   ├── LibraryView.axaml
│   │   ├── GameDetailView.axaml
│   │   └── SettingsView.axaml
│   ├── Fullscreen/              # Fullscreen / gamepad mode views
│   │   ├── PlatformSelectView.axaml
│   │   └── GameGridView.axaml
│   ├── ViewModels/              # Shared ViewModels (MVVM)
│   │   ├── LibraryViewModel.cs
│   │   ├── GameDetailViewModel.cs
│   │   └── SettingsViewModel.cs
│   └── Controls/                # Shared custom controls
│       ├── GameCard.axaml
│       └── ControllerNavigable.cs
│
├── Yarl.Application/            # Application services (use cases)
│   ├── Library/
│   │   └── LibraryService.cs
│   ├── Metadata/
│   │   └── MetadataService.cs
│   ├── Emulators/
│   │   └── EmulatorManager.cs
│   └── Cache/
│       └── CacheManager.cs
│
├── Yarl.Domain/                 # Core domain models and logic
│   ├── Models/
│   │   ├── RomEntry.cs          # ROM record in the library
│   │   ├── Platform.cs
│   │   ├── EmulatorDefinition.cs
│   │   └── Collection.cs
│   ├── Scanner/
│   │   ├── RomScanner.cs        # Filesystem walker
│   │   └── HashComputer.cs      # CRC32/MD5 for scraper matching
│   ├── Registry/
│   │   ├── PlatformRegistry.cs
│   │   └── EmulatorRegistry.cs
│   └── Interfaces/              # Ports (interfaces for providers)
│       ├── IStorageProvider.cs
│       ├── IMetadataScraper.cs
│       ├── IEmulatorInstaller.cs
│       └── IRomSourceProvider.cs
│
├── Yarl.Infrastructure/         # Adapters implementing domain interfaces
│   ├── Storage/
│   │   ├── LocalStorageProvider.cs
│   │   └── MountedPathProvider.cs
│   ├── Scrapers/
│   │   ├── ScreenScraperClient.cs
│   │   ├── IgdbClient.cs
│   │   └── TheGamesDbClient.cs
│   ├── Emulators/
│   │   ├── WindowsEmulatorInstaller.cs
│   │   └── LinuxEmulatorInstaller.cs
│   ├── Cache/
│   │   └── LocalRomCache.cs
│   └── Persistence/
│       ├── LibraryRepository.cs  # SQLite via EF Core or Dapper
│       └── MetadataRepository.cs
│
└── Yarl.Data/                   # Seed data and config schemas
    ├── platforms.json            # Built-in platform definitions
    └── emulators.json            # Built-in emulator definitions
```

### Structure Rationale

- **Yarl.Domain/:** Contains pure domain models and interfaces with no framework dependencies. ROM scanning logic lives here because it is platform-agnostic file traversal — the provider handles where files come from.
- **Yarl.Infrastructure/:** All external adapters (HTTP scrapers, file system, SQLite). Never imported by Domain — only by Application and App layers.
- **Yarl.Application/:** Orchestration services that compose domain logic and infrastructure. ViewModels call these, not infrastructure directly.
- **Yarl.UI/:** Strict separation of Desktop and Fullscreen views prevents coupling. Shared ViewModels allow both modes to operate on the same state.
- **Yarl.Data/:** Platform and emulator definitions ship as embedded JSON/TOML. This makes community contributions possible without code changes.

---

## Architectural Patterns

### Pattern 1: Provider Pattern for ROM Sources and Scrapers

**What:** Define `IRomSourceProvider` and `IMetadataScraper` interfaces. Each source (local disk, OS-mounted path, future SMB/cloud) or scraper (ScreenScraper, IGDB) is a separate implementation registered via DI.

**When to use:** Always. The project brief explicitly requires this for ROM sources. Apply the same pattern to scrapers since metadata source priority matters (ScreenScraper first for retro, IGDB for newer).

**Trade-offs:** Adds interface boilerplate. The extensibility payoff is high for this domain — new sources and scrapers are common asks.

**Example:**
```csharp
public interface IRomSourceProvider
{
    string ProviderId { get; }
    bool CanHandle(string path);
    IAsyncEnumerable<RomFile> EnumerateAsync(string path, CancellationToken ct);
    Task<Stream> OpenAsync(string romPath, CancellationToken ct);
}

// LocalRomSourceProvider, MountedPathProvider — v1
// SmbProvider, SftpProvider — future, no refactor needed
```

### Pattern 2: Background Pipeline with Progress Reporting for ROM Scanning

**What:** ROM scanning runs on a background thread/task. It emits progress events that the UI subscribes to via `IProgress<T>` or reactive streams. The library is browsable while a scan is in progress — already-scanned ROMs appear immediately.

**When to use:** Any scan of more than ~50 files can cause perceptible lag. With large collections (10k+ ROMs), a blocking scan makes the app unusable.

**Trade-offs:** Requires careful state management (library can be "partially loaded"). Pay-off is a responsive UI — essential for Steam Deck where cold starts are common.

**Example:**
```csharp
public class RomScanner
{
    public async IAsyncEnumerable<ScanResult> ScanAsync(
        IRomSourceProvider source,
        string path,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var file in source.EnumerateAsync(path, ct))
        {
            var hash = await _hasher.ComputeAsync(file, ct);
            yield return new ScanResult(file, hash);
        }
    }
}
```

### Pattern 3: Dual-Mode UI with Shared ViewModels

**What:** Desktop Mode and Fullscreen/Gamepad Mode are two separate top-level view trees (not nested panels), but they bind to the same ViewModel layer. A single `UIMode` setting (or startup argument) determines which root view loads. Navigation stack logic lives in the ViewModel.

**When to use:** Always, for this project. Attempting to implement both modes as toggled panels in a single view causes focus management, layout, and input routing nightmares — especially for controller navigation.

**Trade-offs:** Two view trees mean doubled AXAML maintenance for layout. The alternative (single adaptive view) is much harder to get right and degrades both modes.

**Example:**
```csharp
// In App.axaml.cs startup
MainWindow.Content = _settings.UIMode == UIMode.Fullscreen
    ? new FullscreenShell()
    : new DesktopShell();
// Both shells bind to the same LibraryViewModel
```

### Pattern 4: Metadata Cascade with Manual Override

**What:** Metadata lookup follows a priority chain: (1) user manual override in DB, (2) cached scraped data in DB, (3) live scraper request. An override flag on `RomEntry` prevents any scraper from overwriting user-entered data.

**When to use:** Always. Without this, a re-scrape silently overwrites user corrections — a major usability failure in ROM managers.

**Trade-offs:** Slightly more complex update logic. Absolutely required for user trust.

### Pattern 5: Emulator Launch via Process Abstraction

**What:** Launching a ROM is: resolve emulator definition → build CLI args from template → spawn process. The CLI arg template lives in `EmulatorDefinition` (config-driven, not hardcoded). A `IProcessLauncher` abstraction wraps `System.Diagnostics.Process` for testability.

**When to use:** Always. Hardcoding emulator CLI arguments is a maintenance trap — each emulator has different flags, and they change across versions.

**Trade-offs:** Template parsing has edge cases (paths with spaces, special characters). Use well-tested string interpolation, not regex.

**Example:**
```json
// emulators.json
{
  "id": "retroarch",
  "name": "RetroArch",
  "launchTemplate": "{exe} -L \"{core}\" \"{rom}\"",
  "platforms": ["nes", "snes", "gba", "..."]
}
```

---

## Data Flow

### ROM Scan Flow

```
User triggers scan (or auto-scan on startup)
    ↓
LibraryService.ScanAsync(path)
    ↓
IRomSourceProvider.EnumerateAsync(path)   ← Local or mounted path
    ↓ (stream of RomFile)
RomScanner: compute CRC32/MD5 hash per file
    ↓ (stream of ScanResult)
LibraryRepository.UpsertAsync(scanResult)  ← Write to SQLite (by hash)
    ↓
Emit IObservable<RomEntry> / raise event
    ↓
LibraryViewModel updates collection (UI thread marshal)
    ↓
Grid/List view renders new ROM cards
```

### Metadata Scrape Flow

```
RomEntry added to library (or user requests re-scrape)
    ↓
MetadataService.ScrapeAsync(romEntry)
    ↓
Check DB: has manual override? → YES → stop, use override
    ↓ NO
Check DB: has cached data less than [staleness threshold]? → YES → stop, use cache
    ↓ NO
IMetadataScraper[] (priority order):
    1. ScreenScraper (hash lookup — best for retro)
    2. IGDB (name/platform search — best for newer)
    3. TheGamesDB (fallback)
    ↓ (first successful result wins)
MetadataRepository.SaveAsync(metadata)
    ↓
Download cover art → FileCache.Store(imageUrl, localPath)
    ↓
Notify LibraryViewModel (metadata ready — async image load)
```

### ROM Launch Flow

```
User selects ROM + presses launch
    ↓
LibraryViewModel.LaunchCommand.Execute(romEntry)
    ↓
EmulatorManager.LaunchAsync(romEntry)
    ↓
Resolve emulator: romEntry.PreferredEmulator ?? PlatformRegistry.DefaultEmulator(platform)
    ↓
Is ROM remote / not cached?
    → YES: CacheManager.EnsureLocalAsync(romEntry) → copy to cache, track access time
    → NO: use local path directly
    ↓
Build CLI args from EmulatorDefinition.LaunchTemplate + resolved ROM path
    ↓
IProcessLauncher.StartAsync(executablePath, args)
    ↓
LibraryRepository.RecordLaunch(romEntry)  ← Update last-played, play count
    ↓
UI: show "now playing" state (optional)
```

### Cache Eviction Flow

```
Scheduled background job (or on app startup / after launch)
    ↓
CacheManager.RunEvictionAsync()
    ↓
Query all cached entries ordered by last_accessed ASC
    ↓
For each entry (oldest first):
    Is total cache size > MaxDiskUsage setting? → NO → stop
    Is entry.last_accessed older than eviction threshold? → YES → delete file
    ↓
LibraryRepository.MarkUncached(romEntry)
```

### State Flow (MVVM + Reactive)

```
SQLite / File Cache (source of truth)
    ↑↓
LibraryRepository (read/write)
    ↓
LibraryService (domain operations)
    ↓
LibraryViewModel (observable state)
    ↓ (ReactiveUI bindings)
Views: Desktop Grid / Fullscreen Grid
    ↑ (user actions: search, filter, navigate)
    ↓ (commands back to ViewModel → Service)
```

---

## Component Communication Map

| From | To | Method | Notes |
|------|----|--------|-------|
| LibraryViewModel | LibraryService | Direct method call (async) | Application layer serves UI |
| LibraryService | RomScanner | Async stream (`IAsyncEnumerable`) | Non-blocking, yields per ROM |
| LibraryService | LibraryRepository | Async repository calls | SQLite reads/writes |
| MetadataService | IMetadataScraper[] | Interface dispatch (strategy) | Priority order, first hit wins |
| MetadataService | MetadataRepository | Async repository calls | Cache check before HTTP |
| EmulatorManager | CacheManager | Awaited call | Must resolve local path before launch |
| EmulatorManager | IProcessLauncher | Interface call | Abstracted for testability |
| CacheManager | IRomSourceProvider | Interface call | Read remote ROM bytes for caching |
| RomScanner | IRomSourceProvider | Interface call | Enumerate files from source |
| UI Desktop ↔ Fullscreen | Shared ViewModels | Avalonia binding | Both modes bind the same VM instances |
| Any component | Event Bus / Messenger | Weak-reference messaging | Cross-cutting concerns: scan progress, error notifications |

---

## Build Order Implications

The component dependency graph suggests this build order for phases:

1. **Persistence + Domain Models** — `RomEntry`, `Platform`, `EmulatorDefinition`, `Collection`, SQLite schema, repository interfaces. Everything else depends on this.

2. **ROM Scanner + Storage Providers** — `RomScanner`, `LocalStorageProvider`, `MountedPathProvider`. These produce the raw data the rest of the system consumes.

3. **Library Service + Basic UI** — `LibraryService`, minimal desktop UI grid. Validates the scan → display pipeline end-to-end before adding complexity.

4. **Metadata Service + Scrapers** — `MetadataService`, ScreenScraper adapter, IGDB adapter. Depends on having ROMs in the library to test against.

5. **Emulator Registry + Launch** — `EmulatorDefinition` config, `EmulatorManager`, `IProcessLauncher`. Depends on library + platform definitions.

6. **Cache Manager** — Depends on Storage Providers and Launch flow (needs to intercept pre-launch for remote paths). Can be added after basic launch works.

7. **Fullscreen / Gamepad Mode** — Shares ViewModels with Desktop mode. Build after Desktop mode is stable so ViewModels are proven.

8. **Collections, Favorites, Recommendations** — Operates on top of the populated library. Lowest infrastructure dependency.

---

## Anti-Patterns

### Anti-Pattern 1: Blocking ROM Scan

**What people do:** Scan all ROM folders synchronously at startup before showing the UI.
**Why it's wrong:** Collections of 5000+ ROMs take 10-60 seconds to scan depending on storage. The app appears frozen. On Steam Deck with slow SD card reads this is unacceptable.
**Do this instead:** Use `IAsyncEnumerable` scanning. Show the library shell immediately. ROMs appear as they are discovered. Display a scan progress indicator in a non-blocking status bar.

### Anti-Pattern 2: Hardcoding Emulator CLI Arguments

**What people do:** `var args = $"{romPath}";` or per-emulator switch statements in launch code.
**Why it's wrong:** Every emulator version can change its flags. New emulators require code changes. Cores (RetroArch) add another layer of indirection.
**Do this instead:** Store CLI templates in `emulators.json` config. The launch code is a template renderer. Updates ship as data, not code. Community contributions don't require forks.

### Anti-Pattern 3: Single UI Tree with Mode Toggle

**What people do:** One main window with a boolean `IsFullscreen` that shows/hides panels.
**Why it's wrong:** Controller focus management in Avalonia (and most frameworks) becomes deeply complex when the same view tree handles two very different navigation models. Tab/D-pad routing breaks, accessibility breaks, and testing is painful.
**Do this instead:** Two root shells (`DesktopShell`, `FullscreenShell`) sharing ViewModels. Switch at startup based on settings. Each shell owns its own navigation stack.

### Anti-Pattern 4: Storing Cover Art in the Database

**What people do:** Store image bytes as BLOBs in SQLite.
**Why it's wrong:** Cover art for a 5000-game library can be 2-10 GB. SQLite is not a file store. Queries slow down, WAL grows, memory pressure increases.
**Do this instead:** Store images in a flat file cache directory (e.g., `~/.config/yarl/art/{platform}/{hash}.jpg`). Store only the relative path in the database. Serve images to the UI via `file://` URIs or an `IImage` loader.

### Anti-Pattern 5: Scraping Without a Staleness Strategy

**What people do:** Scrape once at add time and never update, OR re-scrape every launch.
**Why it's wrong:** Stale data accumulates (cover art URLs break, descriptions get corrected). Re-scraping every launch hammers API rate limits and slows down launches.
**Do this instead:** Store a `scraped_at` timestamp per entry. Re-scrape if older than a configurable threshold (default: 30 days). Manual override flag prevents any automatic re-scrape of user-corrected entries.

### Anti-Pattern 6: Coupling Emulator Knowledge into Domain Logic

**What people do:** `if (platform == "PlayStation 2") { LaunchPCSX2(...) }` in core services.
**Why it's wrong:** Every new platform, emulator, or configuration option requires a code change. Forks proliferate because users want different defaults.
**Do this instead:** All emulator knowledge lives in `emulators.json` and `platforms.json` seed data. Domain code operates on `EmulatorDefinition` records — it does not know what Dolphin or PCSX2 are.

---

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| ScreenScraper API | REST HTTP, ROM hash lookup (CRC32/MD5/SHA1) | Rate limiting: ~20k queries/day free tier; requires user account; strongest retro coverage |
| IGDB API | REST HTTP, Twitch OAuth2 token required | Better for 6th-gen+ consoles; free tier with rate limits |
| TheGamesDB API | REST HTTP, API key | Backup/fallback; coverage broader than ScreenScraper for some platforms |
| Emulator download sources | HTTPS direct download or package manager CLI | Windows: GitHub Releases ZIP/7z; Linux: Flatpak (`flatpak install`), AppImage |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| UI ↔ Application | ViewModel calls Application services directly; services never reference UI | Standard MVVM boundary; ViewModels use `IScheduler` for thread marshaling |
| Application ↔ Domain | Application services call domain logic and repository interfaces | Domain has no reference to Application |
| Domain ↔ Infrastructure | Domain defines interfaces; Infrastructure implements them | Dependency inversion; Infrastructure registered at DI composition root |
| Scanner ↔ Library | Scanner yields `ScanResult` stream; Library Service writes to DB and emits updates | Decoupled via `IAsyncEnumerable` — scanner does not know about persistence |
| CacheManager ↔ Launch | Launch flow awaits `CacheManager.EnsureLocalAsync()` before resolving ROM path | Sequential — launch cannot proceed until local path guaranteed |

---

## Scaling Considerations

This is a local desktop application, not a server. "Scaling" means handling large ROM collections gracefully.

| Collection Size | Architecture Adjustments |
|-----------------|--------------------------|
| 0-500 ROMs | Default approach works; eager load is acceptable |
| 500-5000 ROMs | Background scanning essential; virtualized grid/list in UI (Avalonia `VirtualizingStackPanel`) |
| 5000-50000 ROMs | Database indexes on platform, genre, name; pagination or windowed loading in ViewModels; cover art loaded on demand (not eagerly) |
| 50000+ ROMs | Possible with the above; search becomes critical — SQLite FTS5 full-text search index recommended |

### Scaling Priorities

1. **First bottleneck:** UI grid rendering with thousands of cover art images. Fix with virtualized lists and lazy image loading — load art only for visible cards.
2. **Second bottleneck:** SQLite query performance on large libraries. Fix with indexes on `platform_id`, `last_played`, `genre`, and a FTS5 index on `name` + `description`.
3. **Third bottleneck:** Metadata scraper rate limits. Fix with request queuing, exponential backoff, and respecting per-source daily limits.

---

## Sources

- Architecture patterns derived from training knowledge of: EmulationStation (C++), Pegasus Frontend (Qt/QML), Playnite (C#/WPF), LaunchBox (C#/.NET)
- Provider pattern: standard hexagonal architecture / ports-and-adapters applied to this domain
- MVVM + ReactiveUI: standard Avalonia UI application pattern
- SQLite scaling considerations: SQLite documentation on FTS5 and index design
- Confidence: MEDIUM — structural patterns are well-established and consistent across named projects; specific API details (ScreenScraper endpoints, IGDB token flow) should be verified in Phase-specific research

---
*Architecture research for: Cross-platform ROM browser / manager / launcher*
*Researched: 2026-03-18*
