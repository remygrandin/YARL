# Phase 3: Metadata and Cover Art - Research

**Researched:** 2026-03-19
**Domain:** API scraping (ScreenScraper, IGDB), Avalonia image rendering, DynamicData filtering, EF Core schema migration
**Confidence:** MEDIUM-HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Scraping trigger**: Auto-scrape after scan (background, non-blocking); manual re-scrape per-game and global
- **Matching pipeline**: CRC32 hash match → title-search fallback → mark as unmatched with "No art" badge
- **Interactive search**: Radarr/Sonarr-style dialog for unmatched games
- **Progress location**: Settings panel (not status bar); main UI uncluttered during scraping
- **Cover art tile layout**: Art fills dominant tile area, title as caption below
- **Platform-themed border**: 2px border on cover art area, color from platform theme color in platforms.json
- **Play button overlay**: Bottom-right of art area, 44×44px, visible on hover/focus only
- **Art type priority**: Box front first; screenshot fallback
- **Placeholder**: `#0f3460` border until art fetched; "No art" badge for confirmed unmatched
- **Detail drawer**: Right-side, 320px fixed, slides in over game list, no page navigation
- **Drawer content**: Large boxart, title, aliases (collapsible), metadata grid, description, regional variants, region picker, Play + Edit buttons
- **Manual override (META-04)**: Inline edit mode in drawer; user overrides survive re-scrape; stored in DB
- **Search (LIB-04)**: Per-platform inline search; 50-result cap + "Load more"; status text `X matches in [Platform] / Y globally`; all aliases matchable; "search globally" pivot
- **Filters (LIB-05)**: Filter chip row (Genre, Year, Developer); AND logic; "Clear filters" button
- **GameVersion**: One tile per canonical game; regional variants grouped; English name default; all aliases searchable; flag icons in drawer

### Claude's Discretion

- Image storage format and directory structure for cached cover art (e.g., `{AppData}/yarl/art/{platformId}/{gameId}.jpg`)
- Image loading async pattern in Avalonia (background thread → UI update; placeholder while loading)
- ScreenScraper and IGDB API client implementation details (rate limiting, retry policy, auth token storage)
- Exact drawer slide-in animation style
- How GameVersion grouping handles edge cases (no ScreenScraper ID found, conflicting hash matches)
- Search debounce timing (typically 200–300ms after last keystroke)
- Filter chip visual styling (inherits existing `#7C6FF7` accent, matching nav active state)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within Phase 3 scope.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| META-01 | App scrapes metadata from ScreenScraper using ROM hash matching | ScreenScraper API v2 endpoint, CRC32 parameter name `crc`, authentication pattern, rate limits documented in §Standard Stack and §Architecture Patterns |
| META-02 | App falls back to IGDB for games not found on ScreenScraper | IGDB NuGet SDK (kamranayub/igdb-dotnet v6.1.0), OAuth2 client credentials flow, game search endpoint documented |
| META-03 | Scraping fetches cover art, description, genre, year, developer, publisher | ScreenScraper `response.jeu` JSON structure, IGDB fields query, image download URL format, local file storage path documented |
| META-04 | User can manually override any metadata field per game | EF Core migration pattern for `IsMetadataOverridden` flag, inline edit mode pattern, re-scrape survivability logic documented |
| LIB-04 | User can search games by name with real-time filtering | DynamicData `Filter()` with observable predicate, `WhenAnyValue` + `Throttle(250ms)` pattern, alias matching strategy documented |
| LIB-05 | User can filter games by genre, year, and developer | Multi-filter `CombineLatest` composition, AND logic via chained `Filter()` operators, filter chip VM pattern documented |

</phase_requirements>

---

## Summary

Phase 3 adds metadata scraping, cover art display, search/filtering, and the game detail drawer. The core technical challenges are: (1) building a correct HTTP client for ScreenScraper's API v2 (no .NET library exists — must hand-roll) and integrating the IGDB NuGet SDK; (2) achieving 60fps grid scrolling with 500+ image tiles (requires upgrading the Phase 2 WrapPanel to a virtualized layout); (3) extending the DynamicData pipeline with search + multi-filter predicates; and (4) safe EF Core schema migration adding metadata fields and a new GameVersion entity.

The single most important finding is the **WrapPanel non-virtualization problem**. The Phase 2 CONTEXT.md states "ItemsRepeater and UniformGridLayout do not exist in Avalonia 11.3.12." This was incorrect — `Avalonia.Controls.ItemsRepeater` v11.1.5 is a separate NuGet package that works with Avalonia ≥ 11.1.5 (fully compatible with 11.3.12). WrapPanel does not virtualize and will cause dropped frames at 500+ items once images are loaded. The grid view must be migrated to `ItemsRepeater` + `UniformGridLayout` to meet the 60fps success criterion.

**Primary recommendation:** Implement Phase 3 in five waves: (1) EF Core schema migration + GameVersion entity; (2) image loading infrastructure (AsyncImageLoader + file cache); (3) ScreenScraper + IGDB clients with Polly retry in a BackgroundService; (4) DynamicData search/filter pipeline + drawer VM; (5) AXAML views (tile upgrade, drawer, search bar, filter chips).

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Avalonia.Controls.ItemsRepeater` | 11.1.5 | Virtualized wrapping grid (`UniformGridLayout`) | Only way to hit 60fps with 500+ image tiles; WrapPanel has no virtualization |
| `AsyncImageLoader.Avalonia` | 3.7.0 | Async local-file bitmap loading with memory cache | Handles off-thread decode and caches; prevents UI freeze from image I/O |
| `IGDB` (kamranayub/igdb-dotnet) | 6.1.0 | IGDB APIv4 .NET wrapper with auto token refresh | Official .NET SDK; handles OAuth2 bearer token acquisition and renewal automatically |
| `Microsoft.Extensions.Http.Resilience` | 10.4.0 | Polly-based retry + exponential backoff for HttpClient | Built-in .NET 9/10 resilience pipeline; handles 429 + Retry-After header correctly |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.Text.Json` | (built-in .NET 10) | Deserialize ScreenScraper JSON response | Already in SDK; no extra package needed |
| `DynamicData` | 9.4.1 (already installed) | Multi-filter + search pipeline extension | Chain additional `Filter()` operators on existing `_gamesSource.Connect()` |
| `Xdg.Directories` | 0.1.2 (already installed) | Cross-platform art cache directory resolution | `AppPaths.ArtCacheDir` already defined as `{DataHome}/yarl/art` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `IGDB` NuGet SDK | Raw `HttpClient` + `System.Text.Json` | Raw gives control, but token management and model serialization are ~200 lines of boilerplate; SDK handles it |
| `Microsoft.Extensions.Http.Resilience` | Polly v8 directly | Resilience package is the recommended .NET 10 path; wraps Polly internally with simpler API |
| `AsyncImageLoader.Avalonia` | Task\<Bitmap\> property + `^` binding | Custom approach works but requires manually managing decode thread, dispose, and caching; AsyncImageLoader covers all of this |
| `ItemsRepeater` + `UniformGridLayout` | WrapPanel (Phase 2 incumbent) | WrapPanel does not virtualize — unacceptable at 500+ items with loaded images |

**Installation (new packages only):**
```bash
dotnet add src/YARL/YARL.csproj package Avalonia.Controls.ItemsRepeater --version 11.1.5
dotnet add src/YARL/YARL.csproj package AsyncImageLoader.Avalonia --version 3.7.0
dotnet add src/YARL/YARL.csproj package IGDB --version 6.1.0
dotnet add src/YARL/YARL.csproj package Microsoft.Extensions.Http.Resilience --version 10.4.0
```

**Version verification (confirmed 2026-03-19):**
- `AsyncImageLoader.Avalonia` → 3.7.0 (NuGet, 208k downloads)
- `Microsoft.Extensions.Http.Resilience` → 10.4.0 (NuGet, 73M downloads)
- `IGDB` → 6.1.0 (NuGet, released 2025-05-26, targets netstandard2.0 + net10.0)
- `Avalonia.Controls.ItemsRepeater` → 11.1.5 (requires Avalonia ≥ 11.1.5; compatible with 11.3.12)

---

## Architecture Patterns

### Recommended Project Structure
```
src/YARL/
├── Domain/
│   ├── Models/
│   │   ├── Game.cs               # + metadata fields + IsMetadataOverridden
│   │   └── GameVersion.cs        # NEW: regional variant grouping entity
│   └── Enums/
│       └── ScrapeStatus.cs       # NEW: Pending/Matched/Unmatched/Override
├── Infrastructure/
│   ├── Persistence/
│   │   ├── YarlDbContext.cs       # + GameVersions DbSet + new entity config
│   │   └── Migrations/           # New migration: AddMetadataAndGameVersion
│   ├── Scraping/
│   │   ├── IMetadataScraper.cs   # NEW: interface (ScreenScraper + IGDB implement)
│   │   ├── ScreenScraperClient.cs # NEW: hand-rolled HTTP client
│   │   ├── IgdbClient.cs          # NEW: wrapper around IGDB NuGet SDK
│   │   ├── ScraperPipeline.cs    # NEW: CRC32 → SS → IGDB → unmatched
│   │   ├── ScraperHostedService.cs # NEW: BackgroundService for auto-scrape
│   │   └── ScrapingProgress.cs   # NEW: progress/status model
│   └── Images/
│       └── ArtCacheService.cs    # NEW: download + cache + path resolution
└── UI/
    ├── ViewModels/
    │   ├── GameViewModel.cs       # + CoverArtPath, metadata fields, ScrapeStatus
    │   ├── LibraryViewModel.cs    # + SearchText, ActiveFilters, scraping commands
    │   ├── GameDetailViewModel.cs # NEW: drawer VM (selected game, edit mode)
    │   └── ScrapingStatusViewModel.cs # NEW: progress panel in Settings
    ├── Views/
    │   ├── GameListView.axaml     # + search bar, filter chips, ItemsRepeater grid
    │   ├── GameDetailDrawer.axaml # NEW: right-side 320px drawer
    │   ├── InteractiveSearchDialog.axaml # NEW: unmatched game lookup dialog
    │   └── SettingsView.axaml     # + scraping progress section
    └── Converters/
        ├── PathToImageConverter.cs # NEW: string path → IImage (via AsyncImageLoader)
        └── NullToPlaceholderConverter.cs # NEW: null CoverArtPath → visibility
```

---

### Pattern 1: ScreenScraper API v2 HTTP Client

**What:** Hand-rolled `HttpClient` wrapper (no .NET NuGet library for screenscraper.fr exists).

**Endpoint:** `https://www.screenscraper.fr/api2/jeuInfos.php`

**Key parameters:**
```
devid={devId}&devpassword={devPassword}&softname=yarl&output=json
&ssid={userId}&sspassword={userPassword}   (optional, recommended)
&systemeid={screenscraper_system_id}        (platform ID — see §Code Examples)
&crc={CRC32_upperhex_8digits}              (e.g. A1B2C3D4)
&md5={md5_hex}&sha1={sha1_hex}             (supplementary hashes)
&romnom={filename}&romtaille={file_size}   (fallback when hash misses)
```

**Response JSON structure (relevant fields):**
```
response.jeu.noms[]           — array of titles by region (region: "eu","us","jp","wor","ss")
response.jeu.synopsis[]       — descriptions by lang (langue: "en","fr","de",...)
response.jeu.genres[].noms[]  — genre names by lang
response.jeu.dates[]          — release dates by region (value: "YYYY-MM-DD")
response.jeu.developpeur.text — developer name
response.jeu.editeur.text     — publisher name
response.jeu.medias[]         — images/video: type, region, url, format
```

**Media type selection for cover art:**
- Primary: `type == "box-2D"` (box front cover)
- Fallback: `type == "ss"` (screenshot)
- Region priority: `"us"` → `"eu"` → `"wor"` → `"jp"` → first available

**Rate limits (MEDIUM confidence — from community documentation):**
- ~20,000 requests/day for registered users (free account)
- Single thread recommended for free accounts
- 429 response triggers pause; use exponential backoff with `Retry-After` header

**Auth requirements:** Developer credentials (devid + devpassword) required; obtainable by registering an app at screenscraper.fr. User credentials optional but strongly recommended to access full quota.

---

### Pattern 2: IGDB Client (Fallback)

**What:** `IGDB` NuGet SDK (kamranayub/igdb-dotnet) wrapping the IGDB APIv4.

**Authentication:** SDK handles Twitch OAuth2 client credentials automatically.

**Rate limits:** 4 requests/second; up to 8 concurrent open requests (verified from IGDB API docs).

**Game search + cover art:**
```csharp
// Source: kamranayub/igdb-dotnet README
var igdb = IGDBClient.CreateWithDefaults(clientId, clientSecret);

// Search by title
var games = await igdb.QueryAsync<Game>(
    IGDBClient.Endpoints.Games,
    query: $"fields id,name,cover,first_release_date,genres.name,involved_companies.company.name,summary; search \"{title}\"; limit 10;");

// Resolve cover art URL
var coverUrl = IGDB.ImageHelper.GetImageUrl(
    imageId: game.Cover?.Value?.ImageId,
    size: ImageSize.CoverBig,
    retina: false);
// e.g. "//images.igdb.com/igdb/image/upload/t_cover_big/{imageId}.jpg"
```

---

### Pattern 3: Resilient HttpClient for ScreenScraper

**What:** `Microsoft.Extensions.Http.Resilience` registered in DI for the ScreenScraper `HttpClient`.

```csharp
// Source: Microsoft Learn — Build resilient HTTP apps
services.AddHttpClient<ScreenScraperClient>()
    .AddResilienceHandler("screenscraper", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 4,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests ||
                args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError)
        });
    });
```

---

### Pattern 4: DynamicData Search + Multi-Filter Pipeline

**What:** Extend the existing `_gamesSource.Connect()` in `LibraryViewModel` with search text + filter predicates.

**Key insight:** DynamicData's `Filter(IObservable<Func<T, bool>>)` overload accepts a dynamic predicate. Combine multiple observable predicates with `CombineLatest` to compose AND logic.

```csharp
// Source: reactivemarbles/DynamicData README + ReactiveUI docs

// 1. Search predicate observable (with 250ms debounce)
var searchFilter = this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(250), _mainThreadScheduler)
    .DistinctUntilChanged()
    .Select(term => string.IsNullOrWhiteSpace(term)
        ? (Func<GameViewModel, bool>)(_ => true)
        : g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
               || g.Aliases.Any(a => a.Contains(term, StringComparison.OrdinalIgnoreCase)));

// 2. Genre filter predicate observable
var genreFilter = this.WhenAnyValue(x => x.ActiveGenreFilter)
    .Select(genre => genre == null
        ? (Func<GameViewModel, bool>)(_ => true)
        : g => g.Genre == genre);

// 3. Combine platform + search + genre (AND logic) into single predicate
var combinedFilter = Observable.CombineLatest(
        this.WhenAnyValue(x => x.SelectedPlatform, x => x.ShowFavoritesOnly, x => x.ShowAllGames),
        searchFilter,
        genreFilter,
        (platformTuple, search, genre) => BuildCombinedFilter(platformTuple, search, genre));

_disposables.Add(
    _gamesSource.Connect()
        .AutoRefresh(g => g.IsFavorite)
        .Filter(combinedFilter)
        .SortBy(g => g.Title)
        .ObserveOn(_mainThreadScheduler)
        .Bind(out _filteredGames)
        .Subscribe());
```

---

### Pattern 5: Async Image Loading with AsyncImageLoader

**What:** Use `AsyncImageLoader.Avalonia` to load local JPEG files off-thread with memory caching.

**Recommended approach:** Register a custom `DiskCachedWebImageLoader` (or `RamCachedWebImageLoader`) with file URI support. For local files, use `file://` URIs or the `AdvancedImage` control.

```xml
<!-- App.axaml: register style -->
<StyleInclude Source="avares://AsyncImageLoader.Avalonia/AdvancedImage.axaml" />

<!-- In game tile DataTemplate -->
<asyncLoader:AdvancedImage
    Source="{Binding CoverArtPath}"
    Width="120" Height="100"
    Stretch="UniformToFill" />
```

For GameViewModel, `CoverArtPath` is a `string?` pointing to the local file path (e.g., `/home/user/.local/share/yarl/art/snes/42.jpg`). When null, show `#0f3460` placeholder (use `IsVisible` binding with `NullToPlaceholderConverter`).

**Alternative (no library):** Expose `Task<Bitmap>?` property and use `Source="{Binding CoverArtBitmap^}"`. Requires manual off-thread decode in ViewModel.

**Recommendation:** Use `AsyncImageLoader.Avalonia` for the memory cache (avoids reloading the same JPEG on every scroll) and simpler AXAML.

---

### Pattern 6: Virtualized Game Grid with ItemsRepeater

**What:** Replace `ItemsControl + WrapPanel` in grid mode with `ItemsRepeater + UniformGridLayout`.

**Why:** WrapPanel has no item virtualization — all 500+ tiles are created and rendered at once, which will cause frame drops during scrolling once images are decoded.

**AXAML migration:**
```xml
<!-- Replace: ItemsControl + WrapPanel -->
<!-- With: ScrollViewer + ItemsRepeater + UniformGridLayout -->
<ScrollViewer>
    <ItemsRepeater ItemsSource="{Binding FilteredGames}">
        <ItemsRepeater.Layout>
            <UniformGridLayout
                MinItemWidth="136"
                MinItemHeight="176"
                MinColumnSpacing="0"
                MinRowSpacing="0" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="vm:GameViewModel">
                <!-- existing tile content -->
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

**Caveat:** `ItemsRepeater` has no built-in selection model. Selection (opening the drawer) must be handled via pointer-pressed event handlers in code-behind or a command binding on the tile. The existing `Button` click pattern for favorites still works.

---

### Pattern 7: Detail Drawer Slide-In Animation

**What:** Translate-X slide from right (320px to 0) using Avalonia `TransformOperationsTransition`.

**Key constraint:** Cannot animate `RenderTransform` directly. Must use `TransformOperations.Parse()` syntax in styles.

```xml
<!-- GameDetailDrawer.axaml -->
<Border x:Name="DrawerPanel"
        Width="320"
        Background="#1a1a2e"
        BorderBrush="#0f3460"
        BorderThickness="1,0,0,0"
        HorizontalAlignment="Right">

    <Border.Styles>
        <!-- Closed state: translated off-screen right -->
        <Style Selector="Border">
            <Setter Property="RenderTransform" Value="translateX(320px)" />
        </Style>
        <!-- Open state: class toggled from VM -->
        <Style Selector="Border.drawerOpen">
            <Setter Property="RenderTransform" Value="translateX(0px)" />
        </Style>
    </Border.Styles>

    <Border.Transitions>
        <Transitions>
            <TransformOperationsTransition Property="RenderTransform"
                                           Duration="0:0:0.2"
                                           Easing="CubicEaseOut" />
        </Transitions>
    </Border.Transitions>
</Border>
```

Toggle `drawerOpen` CSS class from code-behind when `SelectedGame` changes (or use a `Classes` binding via attached property).

---

### Pattern 8: Background Scraping Service

**What:** Extend `RomScanHostedService` pattern — new `ScraperHostedService : BackgroundService` queues games for scraping after scan completes.

```csharp
// Triggered after RomScanHostedService completes
public class ScraperHostedService : BackgroundService
{
    private readonly Channel<int> _scrapeQueue;   // gameId queue
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LibraryViewModel _libraryVm; // for progress updates

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var gameId in _scrapeQueue.Reader.ReadAllAsync(stoppingToken))
        {
            // Scrape with pipeline: CRC32 → ScreenScraper → IGDB → unmatched
            // Report progress back to ScrapingStatusViewModel
        }
    }
}
```

Use `System.Threading.Channels.Channel<T>` for the queue (bounded, capacity = library size). Progress updates posted to `ScrapingStatusViewModel` on the main thread via `ObserveOn(_mainThreadScheduler)`.

---

### Pattern 9: EF Core Schema Migration

**What:** Add metadata fields to `Game`, add `GameVersion` entity, new EF Core migration on existing `20260318234511_AddGameAndRomFile` migration.

**New fields on `Game`:**
```csharp
// Metadata (nullable — populated by scraper)
public string? Description { get; set; }
public string? Genre { get; set; }
public int? ReleaseYear { get; set; }
public string? Developer { get; set; }
public string? Publisher { get; set; }
public string? CoverArtPath { get; set; }        // local file path
public string? ScraperMatchId { get; set; }       // ScreenScraper jeu id or IGDB game id
public string? ScraperSource { get; set; }        // "screenscraper" | "igdb" | "manual"
public bool IsMetadataOverridden { get; set; }    // true = manual edit survives re-scrape
public ScrapeStatus ScrapeStatus { get; set; }   // Pending/Matched/Unmatched
```

**New `GameVersion` entity:**
```csharp
public class GameVersion
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string Region { get; set; } = "";     // "usa" | "europe" | "japan" | "world"
    public string? LocalizedTitle { get; set; }  // regional name alias
    public int? RomFileId { get; set; }          // FK to RomFile (nullable)
}
```

**EF Core config (follows existing `HasConversion<string>()` pattern):**
```csharp
builder.Entity<Game>(e => {
    e.Property(x => x.ScrapeStatus).HasConversion<string>().HasMaxLength(20);
    e.Property(x => x.Description).HasMaxLength(4000);
    e.Property(x => x.CoverArtPath).HasMaxLength(1024);
    e.Property(x => x.ScraperSource).HasMaxLength(20);
});

builder.Entity<GameVersion>(e => {
    e.HasKey(x => x.Id);
    e.Property(x => x.Region).IsRequired().HasMaxLength(20);
    e.Property(x => x.LocalizedTitle).HasMaxLength(500);
    e.HasOne(x => x.Game).WithMany(g => g.Versions).HasForeignKey(x => x.GameId);
    e.HasOne<RomFile>().WithMany().HasForeignKey(x => x.RomFileId).IsRequired(false);
    e.HasIndex(x => new { x.GameId, x.Region });
});
```

**SQLite migration note:** All new columns must be nullable (or have a default value) since existing rows contain no metadata. EF Core SQLite rebuilds the table for structural changes — this is safe with small libraries but adds startup time; add migration in one batch.

---

### Anti-Patterns to Avoid

- **Loading Bitmap on UI thread:** `new Bitmap(filePath)` called synchronously in a converter or template → blocks UI. Always load off-thread. Use `AsyncImageLoader` or `Task<Bitmap>` + `^` binding.
- **WrapPanel for 500+ items:** Confirmed non-virtualizing. Do not keep it for the grid view once images are loaded — switch to `ItemsRepeater + UniformGridLayout`.
- **Storing cover art blobs in SQLite:** Large BLOBs degrade SQLite query performance and inflate DB size. Store as local files; persist the path in DB.
- **Animating `RenderTransform` directly in AXAML:** Does not work in Avalonia 11. Must use `TransformOperations.Parse()` syntax in style setters with `TransformOperationsTransition`.
- **Calling ScreenScraper API without registered developer credentials:** Unauthenticated daily quota is lower; developer app registration at screenscraper.fr is required before shipping.
- **Re-scraping overwriting user overrides:** Check `game.IsMetadataOverridden` before writing any scraped value. If `true`, skip all fields — or apply field-level override flags if a field-granular approach is chosen.
- **Single flat filter predicate in FilteredGames:** Embedding search + genre + year + developer into the existing `BuildPlatformFilter` function creates an unmaintainable mega-predicate. Use separate `Filter()` operator chain or `CombineLatest` composition.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| IGDB OAuth2 token management | `HttpClient` + manual Twitch token endpoint calls | `IGDB` NuGet SDK v6.1.0 | Token refresh, 401 retry, APIcalypse serialization already handled |
| HTTP retry with 429 backoff | Custom retry loop with `Task.Delay` | `Microsoft.Extensions.Http.Resilience` | Respects `Retry-After` header; jittered exponential backoff; circuit breaker included |
| Async image decode + memory cache | `Dictionary<string, Bitmap>` in ViewModel | `AsyncImageLoader.Avalonia` | Handles off-thread decode, LRU cache, placeholder state, and dispose correctly |
| ScreenScraper platform ID mapping | String comparison against SS ID list | Lookup table in `PlatformDefinition` (platforms.json) | ScreenScraper has its own `systemeid` values distinct from the internal `id` field |

**Key insight:** The only hand-rolled client is ScreenScraper — no .NET library exists. Everything else has a battle-tested NuGet package.

---

## Common Pitfalls

### Pitfall 1: WrapPanel Performance Cliff
**What goes wrong:** The existing `ItemsControl + WrapPanel` grid renders fine with placeholders (no images). Once `AsyncImageLoader` starts filling tiles with decoded bitmaps, memory spikes and frame times increase linearly. At ~200+ items, stuttering begins; at 500+ it is severe.
**Why it happens:** WrapPanel creates a visual element for every item in `FilteredGames` regardless of scroll position. Image decode is off-thread but GPU upload is on the render thread — 500 textures uploaded on first layout causes frame drops.
**How to avoid:** Migrate grid view to `ItemsRepeater + UniformGridLayout` before adding `AsyncImageLoader`. The ItemsRepeater only instantiates controls for visible items + a small buffer.
**Warning signs:** Frame time monitor (Avalonia diagnostics) spikes on scroll; `_filteredGames.Count > 100` and any image loading.

### Pitfall 2: ScreenScraper API App Registration Requirement
**What goes wrong:** Calling the API with placeholder `devid`/`devpassword` values returns HTTP 401 or a soft error JSON with no game data.
**Why it happens:** ScreenScraper requires all non-personal scrapers to register a developer application to receive a devid/devpassword. These are separate from user credentials.
**How to avoid:** Register at https://screenscraper.fr before implementing the client. Store devid/devpassword in app config (not hardcoded). Flag in settings if credentials are missing.
**Warning signs:** All scrape calls return `{"header":{"APIversion":...,"ret":"KO"}}` with no `jeu` key.

### Pitfall 3: CRC32 Endianness (Already Known from Phase 2)
**What goes wrong:** ScreenScraper expects CRC32 as 8-digit uppercase hex. `System.IO.Hashing.Crc32` via `Convert.ToHexString(GetCurrentHash())` produces wrong endianness.
**Why it happens:** `GetCurrentHash()` returns bytes in little-endian. ScreenScraper expects the uint value formatted as big-endian hex.
**How to avoid:** Use `.GetCurrentHashAsUInt32().ToString("X8")` — already established in Phase 2. Document this in the scraper client as a gotcha comment.
**Warning signs:** CRC32 hash lookups return no results even for well-known ROM titles.

### Pitfall 4: IGDB Cover Art URL Scheme
**What goes wrong:** `IGDB.ImageHelper.GetImageUrl()` returns URLs with scheme `//images.igdb.com/...` (protocol-relative). `HttpClient` needs an absolute URL.
**Why it happens:** IGDB API historically returned protocol-relative URLs for CDN flexibility.
**How to avoid:** Prepend `"https:"` when the URL starts with `"//"`. `"https:" + imageUrl` or `Uri("https:" + imageUrl)`.
**Warning signs:** `HttpClient.GetAsync()` throws `UriFormatException` or `InvalidOperationException`.

### Pitfall 5: EF Core SQLite Rebuild on Migration
**What goes wrong:** Adding a NOT NULL column without a default to a populated SQLite table fails. EF Core rebuilds the table — this is slow on large libraries.
**Why it happens:** SQLite does not support `ALTER TABLE ADD COLUMN NOT NULL` without a default.
**How to avoid:** All new metadata columns MUST be nullable (`string?`, `int?`, `bool` with default false). Phase 3 adds only nullable or defaulted columns — no user data will exist yet but this is the correct pattern for schema evolution.
**Warning signs:** `dotnet ef database update` throws `SQLiteException: Cannot add a NOT NULL column with default value NULL`.

### Pitfall 6: TransformOperations Drawer Animation
**What goes wrong:** Setting `RenderTransform` to a `TranslateTransform` in AXAML and animating it via `Transitions` does not work in Avalonia 11.
**Why it happens:** Avalonia's transition system cannot interpolate `RenderTransform` set via object syntax. Only `TransformOperations` (CSS-style string syntax) supports `TransformOperationsTransition`.
**How to avoid:** Use `TransformOperations.Parse("translateX(320px)")` string syntax in style setters. Add `<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2" />`.
**Warning signs:** Drawer appears instantly with no animation, or transition throws `No animator registered for the property RenderTransform` at runtime.

### Pitfall 7: IGDB August 2025 Enum-to-Endpoint Migration
**What goes wrong:** IGDB NuGet SDK documentation references an upcoming breaking change (enums → endpoint types). Queries written for pre-August 2025 API may break.
**Why it happens:** Twitch is migrating IGDB API to eliminate enum-based fields in favor of endpoint-based lookups.
**How to avoid:** Use `IGDB` SDK v6.1.0 which was released 2025-05-26 and explicitly addresses this migration. Do not pin to older versions.
**Warning signs:** Query returns unexpected `null` fields for genres, companies, or ratings.

---

## Code Examples

Verified patterns from official and authoritative sources:

### ScreenScraper HTTP Call (verified from skyscraper/src/screenscraper.cpp)
```csharp
// ScreenScraperClient.cs
private static string BuildQueryUrl(string crc32, int systemId, string romFilename, long romSize)
{
    var query = new StringBuilder("https://www.screenscraper.fr/api2/jeuInfos.php?");
    query.Append($"devid={Uri.EscapeDataString(_devId)}&");
    query.Append($"devpassword={Uri.EscapeDataString(_devPassword)}&");
    query.Append($"softname=yarl&output=json&");
    query.Append($"systemeid={systemId}&");
    query.Append($"crc={crc32.ToUpperInvariant()}&");          // 8-char uppercase hex
    query.Append($"romnom={Uri.EscapeDataString(romFilename)}&");
    query.Append($"romtaille={romSize}");
    if (!string.IsNullOrEmpty(_userId))
        query.Append($"&ssid={Uri.EscapeDataString(_userId)}&sspassword={Uri.EscapeDataString(_userPassword)}");
    return query.ToString();
}
```

### DynamicData Multi-Filter with CombineLatest (verified from DynamicData GitHub README)
```csharp
// In LibraryViewModel constructor — extends existing FilteredGames pipeline
var searchPredicate = this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(250), _mainThreadScheduler)
    .DistinctUntilChanged()
    .Select<string?, Func<GameViewModel, bool>>(term =>
        string.IsNullOrWhiteSpace(term)
            ? _ => true
            : g => MatchesSearch(g, term));

var genrePredicate = this.WhenAnyValue(x => x.ActiveGenreFilter)
    .Select<string?, Func<GameViewModel, bool>>(g =>
        g == null ? _ => true : vm => vm.Genre == g);

var platformPredicate = this.WhenAnyValue(
        x => x.SelectedPlatform, x => x.ShowFavoritesOnly, x => x.ShowAllGames)
    .Select(t => BuildPlatformFilter(t.Item1, t.Item2, t.Item3));

var combinedFilter = Observable.CombineLatest(
    platformPredicate, searchPredicate, genrePredicate,
    (pf, sf, gf) => (Func<GameViewModel, bool>)(g => pf(g) && sf(g) && gf(g)));

_gamesSource.Connect()
    .AutoRefresh(g => g.IsFavorite)
    .Filter(combinedFilter)
    .SortBy(g => g.Title)
    .ObserveOn(_mainThreadScheduler)
    .Bind(out _filteredGames)
    .Subscribe()
    .DisposeWith(_disposables);
```

### IGDB Cover Art URL Fix (verified from IGDB API docs)
```csharp
// IgdbClient.cs
private static string NormalizeImageUrl(string? rawUrl)
{
    if (string.IsNullOrWhiteSpace(rawUrl)) return "";
    return rawUrl.StartsWith("//") ? "https:" + rawUrl : rawUrl;
}
```

### AsyncImageLoader local file binding (verified from AsyncImageLoader.Avalonia README)
```xml
<!-- App.axaml — register style once -->
<StyleInclude Source="avares://AsyncImageLoader.Avalonia/AdvancedImage.axaml" />

<!-- GameListView.axaml — in tile DataTemplate -->
<asyncLoader:AdvancedImage
    xmlns:asyncLoader="clr-namespace:AsyncImageLoader;assembly=AsyncImageLoader.Avalonia"
    Source="{Binding CoverArtPath}"
    Width="120" Height="100"
    Stretch="UniformToFill" />
<!-- When CoverArtPath is null, AdvancedImage shows a configurable placeholder or nothing -->
```

### Drawer animation via TransformOperationsTransition (verified from Avalonia community discussion #15086)
```xml
<Border Classes.drawerOpen="{Binding IsDrawerOpen}">
    <Border.Styles>
        <Style Selector="Border">
            <Setter Property="RenderTransform" Value="translateX(320px)" />
        </Style>
        <Style Selector="Border.drawerOpen">
            <Setter Property="RenderTransform" Value="translateX(0px)" />
        </Style>
    </Border.Styles>
    <Border.Transitions>
        <Transitions>
            <TransformOperationsTransition Property="RenderTransform"
                                           Duration="0:0:0.2"
                                           Easing="CubicEaseOut" />
        </Transitions>
    </Border.Transitions>
</Border>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WrapPanel for tile grids | ItemsRepeater + UniformGridLayout | Avalonia 11 (ItemsRepeater separate NuGet) | 60fps scrolling with 500+ items becomes achievable |
| Manual Polly retry setup | `Microsoft.Extensions.Http.Resilience.AddResilienceHandler` | .NET 8 (became standard in .NET 9/10) | Simpler DI-integrated retry + circuit breaker |
| AnimateRenderTransform directly | `TransformOperations` string syntax + `TransformOperationsTransition` | Avalonia 11.0 | Fixed crash "No animator registered for RenderTransform" |
| `RxApp.MainThreadScheduler` | Injected `IScheduler` (SynchronizationContextScheduler) | ReactiveUI v23 | Required pattern already established in Phase 2 |
| IGDB enums (genre, rating type) | IGDB endpoint-based lookups | IGDB API August 2025 migration | Use SDK v6.1.0 which is migration-aware |

**Deprecated/outdated:**
- `RxApp.MainThreadScheduler`: Removed in ReactiveUI v23 — already handled in Phase 2
- `ItemsControl + WrapPanel` for large grids: Does not virtualize — must migrate for Phase 3's 60fps goal

---

## Open Questions

1. **ScreenScraper developer credentials**
   - What we know: A `devid` + `devpassword` pair is required to make API calls; obtained by registering a developer application at screenscraper.fr
   - What's unclear: Whether YARL can be registered as a public open-source application with a shared devid, or whether each user must supply their own credentials
   - Recommendation: Register YARL as a developer application before implementation begins. If the screenscraper.fr registration workflow is unclear, use placeholder credentials in config and surface a "Configure scraper credentials" step in Settings

2. **ScreenScraper systemeid mapping for platforms.json**
   - What we know: The API requires `systemeid` as an integer (e.g., SNES = 3, NES = 3, PS1 = 57). These are ScreenScraper-internal IDs, not the internal `id` strings ("snes", "nes")
   - What's unclear: The complete mapping for all platforms in platforms.json has not been verified
   - Recommendation: Wave 0 task: compile SS system ID table from skyscraper source or ScreenScraper wiki; add `screenscraper_system_id` field to platforms.json entries

3. **ItemsRepeater selection model for drawer trigger**
   - What we know: `ItemsRepeater` has no built-in selection. Tile clicks must be handled manually
   - What's unclear: Whether a `Tapped` event on the tile DataTemplate + command binding is sufficient, or whether a selection-aware wrapper is needed
   - Recommendation: Use `Button.Command` on the tile outer Border (make the tile a Button) bound to `OpenDrawerCommand` in LibraryViewModel, passing the `GameViewModel` as parameter. This avoids any selection infrastructure

4. **IGDB rate limit under bulk scraping**
   - What we know: 4 req/s limit; the `Microsoft.Extensions.Http.Resilience` handler respects `Retry-After`
   - What's unclear: Whether the IGDB SDK's internal HTTP client can be replaced with the resilience-configured one
   - Recommendation: Wrap the IGDB SDK calls manually — create the `IGDBClient` with a custom `HttpClient` instance obtained from `IHttpClientFactory` with the resilience handler registered

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xunit 2.9.3 + Avalonia.Headless.XUnit 11.3.12 |
| Config file | none — test runner configuration in YARL.Tests.csproj |
| Quick run command | `dotnet test tests/YARL.Tests/ --filter "Category=Phase3" -x` |
| Full suite command | `dotnet test tests/YARL.Tests/ --logger "console;verbosity=normal"` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| META-01 | CRC32 hash lookup returns game data (mocked HTTP) | unit | `dotnet test --filter "Category=Phase3&Class=ScreenScraperClientTests"` | Wave 0 |
| META-01 | Pipeline routes to ScreenScraper first | unit | `dotnet test --filter "Category=Phase3&Class=ScraperPipelineTests"` | Wave 0 |
| META-02 | IGDB fallback triggered when SS returns no match | unit | `dotnet test --filter "Category=Phase3&Class=ScraperPipelineTests"` | Wave 0 |
| META-03 | Scraped metadata fields populated on Game entity | unit | `dotnet test --filter "Category=Phase3&Class=ScraperPipelineTests"` | Wave 0 |
| META-04 | Re-scrape does not overwrite `IsMetadataOverridden=true` fields | unit | `dotnet test --filter "Category=Phase3&Class=MetadataOverrideTests"` | Wave 0 |
| LIB-04 | Search text filters FilteredGames by title and alias | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | Wave 0 |
| LIB-04 | Search debounce is 250ms (test with TestScheduler) | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | Wave 0 |
| LIB-05 | Genre + Year + Developer filters compose with AND logic | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | Wave 0 |
| LIB-05 | Clearing all filters restores full platform list | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test tests/YARL.Tests/ --filter "Category=Phase3" -x`
- **Per wave merge:** `dotnet test tests/YARL.Tests/ --logger "console;verbosity=normal"`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs` — covers META-01 (mocked HTTP)
- [ ] `tests/YARL.Tests/Phase3/ScraperPipelineTests.cs` — covers META-01, META-02, META-03
- [ ] `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs` — covers META-04
- [ ] `tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs` — covers LIB-04, LIB-05
- [ ] `NSubstitute` already installed (YARL.Tests.csproj) — can mock `IMetadataScraper`, `HttpMessageHandler`
- [ ] `Avalonia.Headless.XUnit 11.3.12` already installed — available if UI tests needed but not required for Phase 3 unit tests

---

## Sources

### Primary (HIGH confidence)
- [skyscraper/src/screenscraper.cpp](https://github.com/muldjord/skyscraper/blob/master/src/screenscraper.cpp) — ScreenScraper API v2 endpoint URL, parameters, response JSON structure, CRC32 parameter name
- [kamranayub/igdb-dotnet GitHub](https://github.com/kamranayub/igdb-dotnet) — IGDB .NET SDK authentication, query syntax, ImageHelper URL generation
- [IGDB NuGet v6.1.0](https://www.nuget.org/packages/IGDB) — version confirmed net10.0 compatible, released 2025-05-26
- [AsyncImageLoader.Avalonia GitHub](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia) — async file loading API, AdvancedImage control, v3.7.0 verified
- [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) — AddResilienceHandler pattern, 429 handling
- [Avalonia discussion #15086](https://github.com/AvaloniaUI/Avalonia/discussions/15086) — TransformOperationsTransition drawer slide pattern
- [DynamicData GitHub README](https://github.com/reactivemarbles/DynamicData) — Filter with observable predicate, CombineLatest composition
- [Avalonia discussion #15321](https://github.com/AvaloniaUI/Avalonia/discussions/15321) — WrapPanel non-virtualization confirmed; ItemsRepeater + UniformGridLayout recommendation
- `npm view` equivalent: `dotnet package search` ran locally — confirmed package versions 2026-03-19

### Secondary (MEDIUM confidence)
- [Skyscraper SCRAPINGMODULES docs](https://gemba.github.io/skyscraper/SCRAPINGMODULES/) — ScreenScraper rate limits (20k/day), thread limits
- [IGDB API docs (WebSearch summary)](https://api-docs.igdb.com/) — 4 req/s rate limit, OAuth2 client credentials flow
- [Avalonia.Controls.ItemsRepeater NuGet](https://www.nuget.org/packages/Avalonia.Controls.ItemsRepeater) — v11.1.5, requires Avalonia ≥ 11.1.5

### Tertiary (LOW confidence — flag for validation)
- ScreenScraper developer credential registration flow: community implementations reference `devid`/`devpassword` but registration process for new apps not directly verified from official documentation
- IGDB August 2025 enum migration: referenced in igdb-dotnet releases page; exact behavior change not independently confirmed

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages confirmed on NuGet with exact version dates
- ScreenScraper API: MEDIUM — no official .NET docs; verified from multiple community implementations (Skyscraper C++, Dart wrapper, Go package)
- IGDB API: HIGH — official docs available; NuGet SDK confirmed
- Architecture patterns: HIGH — based on existing Phase 2 code + verified library APIs
- Pitfalls: HIGH — WrapPanel limitation confirmed from official Avalonia discussion; other pitfalls from direct code inspection

**Research date:** 2026-03-19
**Valid until:** 2026-06-19 for stable stack items; re-verify ScreenScraper rate limits before shipping (community-reported, may change)

---

## RESEARCH COMPLETE
