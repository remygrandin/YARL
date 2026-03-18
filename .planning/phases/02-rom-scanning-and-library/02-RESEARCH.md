# Phase 2: ROM Scanning and Library - Research

**Researched:** 2026-03-18
**Domain:** Background scanning, EF Core incremental upsert, ReactiveUI/DynamicData live collections, file hashing, platform detection
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Platform Detection**
- Folder-based: parent folder name matched against `platforms.json` — primary and only method
- Matching rule: exact match (case-insensitive) against `Name` field OR any entry in the `Aliases` array
- Non-matching folders: skip silently, record in scan report
- Scan depth: recursive — all subdirectories within a platform folder
- Extension filtering: `platforms.json` `Extensions` array is authoritative per platform
- Multi-disc: `.m3u` files are the game entry; referenced disc files (`.iso`, `.bin`) are RomFiles with DiscNumber set
- Unrecognized extensions: excluded entirely

**Library Browse Model**
- Two view modes: platform grid AND flat list, switchable at runtime
- Platform grid: tiles for platforms with ≥1 ROM (empty platforms hidden)
- Placeholder art: platform logo as placeholder; fallback is platform color + name text
- Game tile: cleaned title + placeholder art + platform badge
- Title cleaning: strip `[USA]`, `[Europe]`, `[Japan]`, `[World]`, `(v1.1)`, `(Rev A)`, `(Disc 1)`, file extension
- Recently Played (LIB-07): horizontal carousel row, last 10–15 games by LastPlayedAt
- Favorites (LIB-06): horizontal carousel row on home view + filter within platform list
- Empty states: "Add ROM folders" CTA when no sources; scanning progress when sources exist

**Scan Triggers and Progress**
- Auto-scan on app startup (background, non-blocking) + on-demand Rescan button
- Status bar: text updates "Scanning SNES... 142 games found"; disappears or shows summary
- Live library updates: platform tiles and game lists update in real-time during scan
- Incremental rescan: add new files, mark removed as Missing, leave existing records untouched
- Missing files: mark `Status = Missing`, grey out in UI; restore to Active if file reappears
- File hashing: CRC32, MD5, SHA1 per RomFile, computed async after import
- Scan report: platforms found, games added, games removed, unmatched folders

**ROM Data Model**
- 3-tier target: `Game → GameVersion → RomFile`; Phase 2 implements `Game → RomFile` only
- At scan time: each file/m3u creates its own `Game` record (no grouping)
- Region detection: parse `[USA]`, `[Europe]`, `[Japan]`, `[World]`, `(Rev A)` into `Region` field

Game fields: Id, Title, RawTitle, PlatformId, SourceId, IsFavorite, LastPlayedAt, CreatedAt, Status enum, Region
RomFile fields: Id, GameId, FilePath, FileName, FileSize, CRC32Hash, MD5Hash, SHA1Hash, DiscNumber, IsM3uPlaylist, SourceId, CreatedAt

### Claude's Discretion
- Exact platform logo asset format and storage location (embedded resources vs file-based)
- Precise title-cleaning normalization rules beyond common no-intro tags
- Scan concurrency level (how many platforms scan in parallel)
- Hash computation batching strategy (how many files hashed concurrently)
- Exact status bar update frequency
- How the scan report is stored and displayed

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| LIB-01 | User can configure ROM folder paths for scanning | RomSource entity exists from Phase 1; Phase 2 reads enabled sources at scan time |
| LIB-02 | App scans folders in background without blocking browsing | BackgroundService + IServiceScopeFactory pattern; DynamicData SourceCache with ObserveOn(RxApp.MainThreadScheduler) |
| LIB-03 | User can browse ROMs organized by platform with cover art grid | PlatformViewModel backed by SourceCache; ItemsControl + WrapPanel; platform grid filter hides empty platforms |
| LIB-06 | User can mark games as favorites | IsFavorite bool on Game entity; toggle ReactiveCommand; SourceCache filter for favorites carousel |
| LIB-07 | App tracks recently played games with last-launched time | LastPlayedAt DateTime? on Game entity; top-N query by LastPlayedAt for carousel; updated at launch time (Phase 4 wires the actual call) |
</phase_requirements>

---

## Summary

Phase 2 transforms a dormant foundation into a live library. The core technical problem is wiring background I/O (file enumeration + hashing) through to a reactive UI without blocking the main thread. The established pattern for this in the ReactiveUI/Avalonia stack is: maintain data sources in DynamicData `SourceCache<TObject, TKey>` on any thread, push changes through `.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out collection).Subscribe()`, and expose a `ReadOnlyObservableCollection<T>` to the view.

The incremental scan design is critical: the scanner must compare discovered file paths against existing database records to add new games, mark vanished ones as `Status = Missing`, and leave metadata untouched on existing records. EF Core's built-in `ExecuteUpdateAsync` (added in EF Core 7) handles the Missing-status bulk update efficiently. Insert-on-first-see uses `AddAsync` + `SaveChangesAsync` with FilePath as a unique key enforced at the database level. No third-party upsert library is required.

Hash computation (CRC32 via `System.IO.Hashing`, MD5/SHA1 via `System.Security.Cryptography`) must run after the game record is inserted — never before — so the UI receives game titles immediately and hash values fill in asynchronously. The platforms.json file currently lacks an `aliases` field; that array must be added before the scanner can apply alias-based platform matching.

**Primary recommendation:** Use DynamicData SourceCache as the in-memory game store; BackgroundService with IServiceScopeFactory for the scanner; System.IO.Hashing 10.0.4 for CRC32; System.Security.Cryptography (in-box) for MD5/SHA1. No additional NuGet packages beyond these are needed.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| DynamicData | 9.4.1 (transitive via ReactiveUI.Avalonia) | Thread-safe reactive collection for SourceCache/SourceList | Official ReactiveUI collection library; already a transitive dependency |
| System.IO.Hashing | 10.0.4 | CRC32 (and XxHash variants) | Official .NET library; Crc32 class with AppendAsync(Stream, CancellationToken) |
| System.Security.Cryptography | in-box (.NET 10) | MD5 + SHA1 via MD5.HashData / SHA1.HashData | Built into .NET runtime; static one-shot methods on .NET 7+ |
| Microsoft.Extensions.Hosting | 10.0.5 (already in csproj) | BackgroundService base class for scanner | Already present; Generic Host is already configured |
| ReactiveUI.SourceGenerators | 2.6.1 (already in csproj) | [Reactive] attribute on LibraryViewModel properties | Already present; eliminates RaiseAndSetIfChanged boilerplate |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | in-box | Deserialize platforms.json at runtime | Used with JsonSerializer.Deserialize for PlatformDefinition DTO |
| Microsoft.EntityFrameworkCore | 10.0.5 (already in csproj) | Game + RomFile EF entities; incremental scan queries | ExecuteUpdateAsync for bulk Missing status; tracked insert for new records |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DynamicData SourceCache | Plain ObservableCollection | ObservableCollection is not thread-safe; requires Dispatcher.UIThread.Post() manually; DynamicData eliminates this entirely |
| System.IO.Hashing | Force.Crc32 (NuGet) | System.IO.Hashing is first-party, no extra dependency, same API surface |
| BackgroundService | Task.Run + CancellationTokenSource | BackgroundService integrates with Generic Host lifetime; handles graceful shutdown automatically |
| EFCore.BulkExtensions | Manual check-then-insert | BulkExtensions is a commercial library; EF Core 7+ ExecuteUpdateAsync + individual tracked inserts is sufficient for scan volumes |

**Installation (new package only):**
```bash
dotnet add src/YARL/YARL.csproj package System.IO.Hashing --version 10.0.4
```

DynamicData 9.4.1 is already a transitive dependency — no install needed. All other packages already in csproj.

---

## Architecture Patterns

### Recommended Project Structure

```
src/YARL/
├── Domain/
│   ├── Models/
│   │   ├── Game.cs            # Phase 2: new entity
│   │   ├── RomFile.cs         # Phase 2: new entity
│   │   └── ScanReport.cs      # Phase 2: lightweight scan summary
│   ├── Enums/
│   │   └── GameStatus.cs      # Active | Missing | Hidden
│   └── Interfaces/
│       └── IRomScanner.cs     # IScanProgressReporter interface
├── Infrastructure/
│   ├── Persistence/
│   │   ├── YarlDbContext.cs    # Add DbSet<Game>, DbSet<RomFile>
│   │   └── Migrations/        # New EF migration after entity changes
│   ├── Scanning/
│   │   ├── RomScannerService.cs      # Core scan logic (scoped service)
│   │   ├── RomScanHostedService.cs   # BackgroundService wrapper
│   │   ├── PlatformRegistry.cs       # Loads platforms.json; name/alias lookup
│   │   ├── FileHasher.cs             # CRC32 + MD5 + SHA1 computation
│   │   ├── FilenameParser.cs         # No-intro tag stripping; region extraction
│   │   └── M3uParser.cs              # .m3u playlist parsing
│   └── Config/
│       └── AppPaths.cs        # Add ScanReportPath property
├── UI/
│   ├── ViewModels/
│   │   ├── LibraryViewModel.cs   # Expand: add SourceCaches, scan state, commands
│   │   ├── PlatformViewModel.cs  # Phase 2: new; platform tile data
│   │   └── GameViewModel.cs      # Phase 2: new; game tile data
│   ├── Desktop/
│   │   ├── DesktopShell.axaml    # Update: add status bar, platform grid/list
│   │   └── LibraryView.axaml     # Phase 2: new; main library view
│   └── Fullscreen/
│       └── FullscreenShell.axaml # Update: add carousel rows
└── Data/
    └── platforms.json            # Must add "aliases" array to entries
```

### Pattern 1: DynamicData SourceCache with Background-Thread Updates

**What:** Maintain the in-memory game collection in a `SourceCache<GameViewModel, int>` keyed by game Id. The scanner thread calls `AddOrUpdate` / `Remove` freely; the UI subscribes through a pipeline that switches to the main thread before binding.

**When to use:** Whenever data is produced on a background thread and must be displayed in Avalonia UI.

```csharp
// Source: DynamicData documentation https://www.reactiveui.net/docs/handbook/collections
// In LibraryViewModel constructor:
private readonly SourceCache<GameViewModel, int> _gamesSource = new(g => g.Id);

// Expose to view (ReadOnlyObservableCollection is thread-safe for read access)
private ReadOnlyObservableCollection<GameViewModel> _games = new([]);
public ReadOnlyObservableCollection<GameViewModel> Games => _games;

// Wire in constructor:
_gamesSource
    .Connect()
    .ObserveOn(RxApp.MainThreadScheduler)   // switch to UI thread before bind
    .Bind(out _games)
    .Subscribe()
    .DisposeWith(_disposables);

// Scanner thread (any thread) calls:
_gamesSource.AddOrUpdate(gameViewModel);
_gamesSource.Remove(gameId);
```

### Pattern 2: BackgroundService with IServiceScopeFactory for Scoped EF DbContext

**What:** The scanner `BackgroundService` is a singleton (registered via `AddHostedService<>`). YarlDbContext is scoped. Create a new scope per scan run using `IServiceScopeFactory`.

**When to use:** Any `BackgroundService` that needs a scoped service (EF DbContext).

```csharp
// Source: Microsoft Learn https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
public class RomScanHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LibraryViewModel _libraryVm;

    public RomScanHostedService(IServiceScopeFactory scopeFactory, LibraryViewModel libraryVm)
    {
        _scopeFactory = scopeFactory;
        _libraryVm = libraryVm;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so host startup is not blocked
        await Task.Yield();

        using var scope = _scopeFactory.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<RomScannerService>();
        await scanner.ScanAllAsync(stoppingToken);
    }
}

// Registration in Program.cs:
services.AddHostedService<RomScanHostedService>();
services.AddScoped<RomScannerService>();
```

### Pattern 3: Incremental Upsert (Add-new / Mark-missing)

**What:** On each scan, collect discovered file paths. Query existing RomFile paths from DB. Compute additions (new paths) and removals (DB paths no longer on disk). Never wipe and reimport.

**When to use:** The scan is incremental by design decision — metadata must be preserved.

```csharp
// Source: EF Core docs https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete
// Step 1: Mark missing files in one SQL UPDATE (no tracking, fast)
var discoveredPaths = new HashSet<string>(discoveredFilePaths);
await db.RomFiles
    .Where(rf => rf.SourceId == source.Id && !discoveredPaths.Contains(rf.FilePath))
    .ExecuteUpdateAsync(s => s
        .SetProperty(rf => rf.Game.Status, GameStatus.Missing),
        stoppingToken);

// Step 2: Restore previously-missing files that reappeared
await db.Games
    .Where(g => g.Status == GameStatus.Missing
             && db.RomFiles.Any(rf => rf.GameId == g.Id
                                   && discoveredPaths.Contains(rf.FilePath)))
    .ExecuteUpdateAsync(s => s
        .SetProperty(g => g.Status, GameStatus.Active),
        stoppingToken);

// Step 3: Insert new files (path as unique key — unique index enforced in migration)
var existingPaths = await db.RomFiles
    .Where(rf => rf.SourceId == source.Id)
    .Select(rf => rf.FilePath)
    .ToHashSetAsync(stoppingToken);

foreach (var newPath in discoveredFilePaths.Where(p => !existingPaths.Contains(p)))
{
    var game = BuildGame(newPath, platformId, source.Id);
    var romFile = BuildRomFile(newPath, game, source.Id);
    db.Games.Add(game);
    db.RomFiles.Add(romFile);
}
await db.SaveChangesAsync(stoppingToken);
```

### Pattern 4: [Reactive] Attribute on LibraryViewModel

**What:** Use ReactiveUI.SourceGenerators `[Reactive]` attribute on `private` fields (or `partial` properties in C# 13) to auto-generate public properties with change notification.

**When to use:** All new LibraryViewModel properties (StatusMessage, IsScanning, ScanProgressText, etc.).

```csharp
// Source: https://github.com/reactiveui/ReactiveUI.SourceGenerators
// Class must be partial and inherit ReactiveObject
public partial class LibraryViewModel : ReactiveObject
{
    [Reactive] private string _statusMessage = "Ready.";
    [Reactive] private bool _isScanning;
    [Reactive] private string _scanProgressText = "";

    // Generator creates: public string StatusMessage { get; set; }
    // with RaiseAndSetIfChanged wired automatically
}
```

### Pattern 5: Multi-Hash Pipeline (CRC32 + MD5 + SHA1)

**What:** Open the file once, stream it through all three hash algorithms simultaneously using buffered reads.

**When to use:** FileHasher service, called after Game/RomFile records are inserted.

```csharp
// Source: System.IO.Hashing docs https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
// Source: System.Security.Cryptography in-box .NET 10
public static async Task<(string crc32, string md5, string sha1)> ComputeHashesAsync(
    string filePath, CancellationToken ct)
{
    using var crc32 = new Crc32();
    using var md5 = MD5.Create();
    using var sha1 = SHA1.Create();
    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
        FileShare.Read, bufferSize: 81920, useAsync: true);

    var buffer = new byte[81920];
    int bytesRead;
    while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
    {
        var span = buffer.AsSpan(0, bytesRead);
        crc32.Append(span);
        md5.TransformBlock(buffer, 0, bytesRead, null, 0);
        sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
    }
    md5.TransformFinalBlock([], 0, 0);
    sha1.TransformFinalBlock([], 0, 0);

    var crc32Hex = crc32.GetCurrentHashAsUInt32().ToString("X8");
    var md5Hex = Convert.ToHexString(md5.Hash!);
    var sha1Hex = Convert.ToHexString(sha1.Hash!);
    return (crc32Hex, md5Hex, sha1Hex);
}
```

### Pattern 6: No-Intro Filename Tag Parsing

**What:** Strip region/version tags from ROM filenames to produce clean display titles. Parse region for the `Region` field.

**When to use:** FilenameParser.CleanTitle() and FilenameParser.ExtractRegion().

```csharp
// No-intro uses parentheses () not square brackets [] for most tags.
// Square brackets [] are used by GoodTools set, but CONTEXT.md specifies both.
// Regex strips everything in brackets/parens after the title.

private static readonly Regex TagPattern =
    new(@"\s*[\(\[][^\)\]]*[\)\]]", RegexOptions.Compiled);

private static readonly Regex RegionPattern =
    new(@"[\(\[](USA|Europe|Japan|World|En|Fr|De|Es|It|Pt)[,\s\)\]]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

public static string CleanTitle(string fileNameWithoutExtension)
    => TagPattern.Replace(fileNameWithoutExtension, "").Trim();

public static string? ExtractRegion(string fileNameWithoutExtension)
{
    var match = RegionPattern.Match(fileNameWithoutExtension);
    return match.Success ? match.Groups[1].Value : null;
}
```

### Pattern 7: .m3u Playlist Parsing

**What:** A `.m3u` file is plain text — each non-comment line is a relative or absolute path to a disc file. Lines starting with `#` are comments.

**When to use:** M3uParser in the scanner, when the file extension is `.m3u`.

```csharp
// .m3u format: plain text, one path per line, # = comment
// Paths are typically relative to the .m3u file's directory.
public static IReadOnlyList<string> ParseDiscPaths(string m3uFilePath)
{
    var dir = Path.GetDirectoryName(m3uFilePath)!;
    return File.ReadAllLines(m3uFilePath)
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
        .Select(line => Path.IsPathRooted(line)
            ? line
            : Path.GetFullPath(Path.Combine(dir, line)))
        .ToList();
}
```

### Pattern 8: Horizontal Carousel Row in Avalonia

**What:** A horizontally-scrolling row of game tiles for "Continue Playing" and "Favorites" sections.

**When to use:** Recently Played and Favorites rows in both shells.

```xml
<!-- Source: Avalonia docs https://docs.avaloniaui.net/docs/reference/controls/scrollviewer -->
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled">
  <ItemsControl ItemsSource="{Binding RecentlyPlayed}"
                x:DataType="vm:LibraryViewModel">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate>
        <StackPanel Orientation="Horizontal" Spacing="8"/>
      </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate x:DataType="vm:GameViewModel">
        <Border Width="120" Height="160" ...>
          <TextBlock Text="{Binding Title}"/>
        </Border>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</ScrollViewer>
```

Note: Avalonia's `Carousel` control is a page-flipper (single item visible), not a multi-item row. Use `ScrollViewer + ItemsControl + StackPanel(Horizontal)` for the "continue playing" row. For virtualized grids (platform grid), use `ItemsRepeater` with `UniformGridLayout` instead of `ItemsControl + WrapPanel`.

### Anti-Patterns to Avoid

- **Direct ObservableCollection updates from background thread:** ObservableCollection is not thread-safe. Use DynamicData SourceCache and ObserveOn(RxApp.MainThreadScheduler).
- **Wipe-and-reimport on rescan:** Destroys favorites, metadata, and play history. Always use incremental add/mark-missing.
- **Hashing before insert:** Large ISOs (Wii: 4.7GB) block the scan pipeline. Hash after insert in a separate background pass.
- **Loading all games into memory for existence check:** Use `ToHashSetAsync` on file paths only (strings), not full entity objects.
- **Blocking ExecuteAsync:** `await Task.Yield()` at the start of ExecuteAsync so host startup completes before scanning begins.
- **Using Avalonia Carousel for multi-item row:** Carousel shows one item at a time. Use ScrollViewer + StackPanel instead.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| CRC32 hash | Custom polynomial loop | `System.IO.Hashing.Crc32` | Edge cases around endianness; `AppendAsync(Stream, CancellationToken)` handles cancellation |
| Thread-safe live collection | `lock()` + ObservableCollection | DynamicData `SourceCache<T, TKey>` | Change-set semantics, filtering, sorting, binding are all built in |
| Platform name → id lookup | Dictionary built at startup manually | `PlatformRegistry` loading platforms.json | Centralizes alias matching; easy to unit test |
| Filename tag stripping | Ad-hoc string.Replace calls | Compiled Regex with named groups | No-intro tag variety (Rev A, v1.1, Disc 2, Proto, Beta) is wide; regex handles all |
| Scan-to-UI progress bridge | Custom event/callback chain | `IProgress<ScanUpdate>` passed into scan service | .NET pattern; Progress<T> marshals to capture context (UI thread) automatically |

**Key insight:** The hardest problems in this domain (thread safety, CRC32 correctness, tag variation) already have well-tested .NET solutions. Custom code here is a liability.

---

## Common Pitfalls

### Pitfall 1: platforms.json Missing "Aliases" Field

**What goes wrong:** The current `platforms.json` has `id`, `name`, `extensions`, `retroarchCoreId` but no `aliases` array. The CONTEXT.md decision requires alias matching. The scanner will fail to match any folder by alias at runtime.

**Why it happens:** The field was specified in the design but not yet added to the data file.

**How to avoid:** Add `"aliases": []` to every entry in platforms.json before implementing the scanner. For example, add `"aliases": ["SNES", "Super Famicom"]` to the `snes` entry.

**Warning signs:** Scanner matches zero platforms during testing despite correct folder names.

### Pitfall 2: IHostedService StartAsync Blocks App Startup

**What goes wrong:** If ExecuteAsync starts synchronous I/O before the first `await`, it blocks the Avalonia UI thread from displaying the window, because `StartAsync` is called before the window appears.

**Why it happens:** BackgroundService.StartAsync fires ExecuteAsync and awaits it if it completes synchronously.

**How to avoid:** Begin ExecuteAsync with `await Task.Yield()` to release the host startup thread immediately.

**Warning signs:** Application window does not appear until after the first scan completes.

### Pitfall 3: EF Core DbContext Registered as Scoped Inside Singleton

**What goes wrong:** `RomScanHostedService` is registered as a hosted service (singleton lifetime). `YarlDbContext` is scoped. Injecting YarlDbContext directly into the hosted service causes a DI scope validation error.

**Why it happens:** Scoped services cannot be directly consumed by singletons.

**How to avoid:** Inject `IServiceScopeFactory` into the hosted service. Create a scope per scan run and resolve YarlDbContext from the scope.

**Warning signs:** `InvalidOperationException: Cannot consume scoped service 'YarlDbContext' from singleton 'RomScanHostedService'` at startup.

### Pitfall 4: FilePath Uniqueness Not Enforced at DB Level

**What goes wrong:** If a scan is interrupted and restarted, the same file paths get inserted twice, producing duplicate Game records.

**Why it happens:** Without a unique index on RomFiles.FilePath, EF Core's `Add` + `SaveChanges` has no constraint to prevent duplicates.

**How to avoid:** Add a unique index on `RomFiles.FilePath` in the EF Core model configuration. The incremental scan's existence check (`ToHashSetAsync(paths)`) is a performance optimization, not a correctness guarantee — the DB constraint is the backstop.

**Warning signs:** Duplicate entries in the game grid after interrupted scans.

### Pitfall 5: MD5/SHA1 HashAlgorithm Instances Are Not Thread-Safe

**What goes wrong:** Sharing a single `MD5.Create()` or `SHA1.Create()` instance across concurrent hash tasks causes data corruption in hash output.

**Why it happens:** `HashAlgorithm` internal state is not thread-safe.

**How to avoid:** Create new MD5/SHA1 instances per file in `FileHasher`. Either `using var md5 = MD5.Create()` per call, or use the static `MD5.HashData(ReadOnlySpan<byte>)` when the full file is small enough to buffer.

**Warning signs:** Intermittent wrong hash values on files processed concurrently.

### Pitfall 6: CRC32 Output Endianness

**What goes wrong:** ScreenScraper expects CRC32 as an 8-character uppercase hex string (big-endian representation of a uint). `System.IO.Hashing.Crc32.GetCurrentHashAsUInt32()` returns a `uint`; `ToString("X8")` on that uint produces the correct 8-char hex.

**Why it happens:** The bytes returned by `GetCurrentHash()` are little-endian; if you hex-encode the byte array directly you get the wrong string.

**How to avoid:** Use `GetCurrentHashAsUInt32().ToString("X8")` rather than `Convert.ToHexString(GetCurrentHash())`.

**Warning signs:** CRC32 values match nothing in ScreenScraper even for known-good dumps.

### Pitfall 7: Avalonia Compiled Bindings Require x:DataType on All Templates

**What goes wrong:** Runtime binding errors or silent failures for game tile DataTemplates because `AvaloniaUseCompiledBindingsByDefault=true` requires explicit `x:DataType` on every `DataTemplate` and `ItemsControl`.

**Why it happens:** Without `x:DataType`, Avalonia compiled bindings cannot resolve the binding path at compile time.

**How to avoid:** Every `DataTemplate` and the root element of every `.axaml` view must declare `x:DataType="vm:GameViewModel"` (or the appropriate VM type). This is established practice from Phase 1.

**Warning signs:** AXAML compilation errors mentioning unresolved binding paths.

---

## Code Examples

### EF Core Model Configuration for Game and RomFile

```csharp
// YarlDbContext.OnModelCreating additions
builder.Entity<Game>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Title).IsRequired().HasMaxLength(500);
    e.Property(x => x.RawTitle).HasMaxLength(500);
    e.Property(x => x.PlatformId).IsRequired().HasMaxLength(50);
    e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    e.Property(x => x.Region).HasMaxLength(100);
    e.HasIndex(x => new { x.PlatformId, x.Status });
    e.HasIndex(x => x.LastPlayedAt);
    e.HasOne<RomSource>().WithMany().HasForeignKey(x => x.SourceId);
});

builder.Entity<RomFile>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.FilePath).IsRequired().HasMaxLength(2048);
    e.HasIndex(x => x.FilePath).IsUnique();  // prevents duplicate imports
    e.HasOne(x => x.Game).WithMany(g => g.RomFiles).HasForeignKey(x => x.GameId);
    e.HasOne<RomSource>().WithMany().HasForeignKey(x => x.SourceId);
});
```

### PlatformRegistry Loading platforms.json

```csharp
// platforms.json must be extended with "aliases" array before use
public class PlatformDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Extensions { get; set; } = [];
    public List<string> Aliases { get; set; } = [];      // NEW — must be added to json
    public string? RetroarchCoreId { get; set; }
}

public class PlatformRegistry
{
    private readonly Dictionary<string, PlatformDefinition> _byName;   // key = lowercase name
    private readonly Dictionary<string, PlatformDefinition> _byAlias;  // key = lowercase alias

    public PlatformRegistry(IEnumerable<PlatformDefinition> platforms)
    {
        _byName  = platforms.ToDictionary(p => p.Name.ToLowerInvariant());
        _byAlias = platforms
            .SelectMany(p => p.Aliases.Select(a => (alias: a.ToLowerInvariant(), platform: p)))
            .ToDictionary(x => x.alias, x => x.platform);
    }

    public PlatformDefinition? Resolve(string folderName)
    {
        var key = folderName.ToLowerInvariant();
        if (_byName.TryGetValue(key, out var p)) return p;
        if (_byAlias.TryGetValue(key, out var pa)) return pa;
        return null;
    }
}
```

### Scan Progress Bridge via IProgress<T>

```csharp
// ScanUpdate record carries progress data to UI
public record ScanUpdate(string PlatformName, int GamesFound, int TotalProcessed);

// In RomScannerService.ScanAllAsync:
public async Task ScanAllAsync(
    IProgress<ScanUpdate> progress,
    CancellationToken ct)
{
    // ... per-platform loop ...
    progress.Report(new ScanUpdate(platform.Name, gamesInPlatform, totalSoFar));
}

// In RomScanHostedService.ExecuteAsync:
var progress = new Progress<ScanUpdate>(update =>
{
    // Progress<T> captures SynchronizationContext at creation (UI thread)
    // so this callback runs on UI thread automatically
    _libraryVm.StatusMessage =
        $"Scanning {update.PlatformName}... {update.GamesFound} games found";
});
await scanner.ScanAllAsync(progress, stoppingToken);
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ObservableCollection + Dispatcher.UIThread.Post | DynamicData SourceCache + ObserveOn(RxApp.MainThreadScheduler) | DynamicData v5+ | Background updates are safe; filtering/sorting built in |
| Manual CRC32 polynomial implementation | System.IO.Hashing.Crc32 | .NET 6 | First-party, hardware-accelerated where available |
| HashAlgorithm.ComputeHash(stream) (sync) | MD5.HashData(ReadOnlySpan) or TransformBlock stream loop | .NET 7 | One-shot static methods avoid allocations for small files |
| EF Core track-all + SaveChanges for bulk update | EF Core 7 ExecuteUpdateAsync | EF Core 7.0 | Bulk status update without loading entities into memory |
| fody/PropertyChanged weaving | ReactiveUI.SourceGenerators [Reactive] attribute | 2023 | No post-build weaving; Roslyn source generator; same output |

**Deprecated/outdated:**
- `ReactiveList<T>`: Removed from ReactiveUI; replaced by DynamicData SourceList/SourceCache.
- `HashAlgorithm.ComputeHash(byte[])` on large files: Forces full file into memory; use streaming approach.
- `Avalonia.Controls.Carousel` for multi-item scrolling rows: It is a single-item page flipper; use ScrollViewer + ItemsControl.

---

## Open Questions

1. **platforms.json aliases: what values should be added?**
   - What we know: The file has no `aliases` array today. The scanner needs it for the matching rule.
   - What's unclear: Which common folder name aliases matter most (e.g., "PSX" for PlayStation, "MD" for Genesis, "GBC" for "Game Boy Color").
   - Recommendation: Add at minimum the common abbreviations used by popular ROM organizers (EmulationStation, Retropie naming conventions). A Wave 0 task should populate the aliases before the scanner is implemented.

2. **Scan concurrency: one platform at a time vs parallel?**
   - What we know: This is Claude's Discretion per CONTEXT.md.
   - What's unclear: Network-mounted ROMs (OsMountedRomSourceProvider) may saturate bandwidth if parallel.
   - Recommendation: Implement sequential per-source scan first (one RomSource at a time). Add parallelism as a tuning option if performance is inadequate. `SemaphoreSlim` can cap concurrency easily.

3. **Hash computation batching: sequential vs parallel?**
   - What we know: This is Claude's Discretion per CONTEXT.md. Large ISOs (4–8 GB for Wii/PS2) take significant time.
   - What's unclear: Whether to use `Channel<string>` producer/consumer or `SemaphoreSlim`-gated `Parallel.ForEachAsync`.
   - Recommendation: Use `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 2` as a safe default. Avoids saturating disk I/O while still hashing concurrently.

4. **Scan report storage: JSON file vs in-memory + DB row?**
   - What we know: Claude's Discretion. AppPaths already provides `DataDir`.
   - Recommendation: Write a single `scan-report.json` to AppPaths.DataDir after each scan. Overwrite on each scan (only last report retained). Simple, no extra DB migration.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xunit 2.9.3 + Avalonia.Headless.XUnit 11.3.12 |
| Config file | none — standard xunit discovery |
| Quick run command | `dotnet test tests/YARL.Tests/ -x` |
| Full suite command | `dotnet test tests/YARL.Tests/` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LIB-01 | ROM sources are read at scan time | unit | `dotnet test tests/YARL.Tests/ --filter "Category=Scanning" -x` | ❌ Wave 0 |
| LIB-02 | Scan does not block UI (BackgroundService + cancellation) | unit | `dotnet test tests/YARL.Tests/ --filter "Category=Scanner" -x` | ❌ Wave 0 |
| LIB-02 | IProgress<ScanUpdate> reports progress updates | unit | `dotnet test tests/YARL.Tests/ --filter "Category=Scanner" -x` | ❌ Wave 0 |
| LIB-02 | CancellationToken stops scan mid-run | unit | `dotnet test tests/YARL.Tests/ --filter "Category=Scanner" -x` | ❌ Wave 0 |
| LIB-03 | Platform grid shows only platforms with ≥1 game | unit | `dotnet test tests/YARL.Tests/ --filter "Category=LibraryViewModel" -x` | ❌ Wave 0 |
| LIB-03 | Title cleaning strips no-intro region tags | unit | `dotnet test tests/YARL.Tests/ --filter "Category=FilenameParser" -x` | ❌ Wave 0 |
| LIB-06 | IsFavorite toggle updates DB and ViewModel | unit | `dotnet test tests/YARL.Tests/ --filter "Category=Favorites" -x` | ❌ Wave 0 |
| LIB-07 | LastPlayedAt records timestamp; top-15 query returns correct order | unit | `dotnet test tests/YARL.Tests/ --filter "Category=RecentlyPlayed" -x` | ❌ Wave 0 |
| — | EF Core migration includes Games + RomFiles tables | integration | `dotnet test tests/YARL.Tests/ --filter "Category=Migration" -x` | ❌ Wave 0 |
| — | Incremental scan: new files added, removed marked Missing | integration | `dotnet test tests/YARL.Tests/ --filter "Category=Scanner" -x` | ❌ Wave 0 |
| — | Platform detection: folder "Super Nintendo" resolves to snes | unit | `dotnet test tests/YARL.Tests/ --filter "Category=PlatformRegistry" -x` | ❌ Wave 0 |
| — | CRC32 output matches known-good value for small test file | unit | `dotnet test tests/YARL.Tests/ --filter "Category=FileHasher" -x` | ❌ Wave 0 |
| — | M3u parser extracts disc paths correctly | unit | `dotnet test tests/YARL.Tests/ --filter "Category=M3uParser" -x` | ❌ Wave 0 |

### Test Design Notes

**Background scanner (non-blocking, progress, cancellation):**
- Use an in-memory file system abstraction (a temp directory with pre-created files) rather than mocking `IRomSourceProvider`
- Test that `Progress<ScanUpdate>` receives expected update count; the callback runs synchronously in xunit (no real UI thread needed)
- Test cancellation: pass a pre-cancelled CancellationToken; assert scanner exits cleanly without throwing `OperationCanceledException` to caller (it should catch and finalize gracefully)

**EF Core incremental upsert:**
- Use in-memory SQLite (`:memory:`) following established `MigrationTests` pattern
- Seed DB with known file paths, run scan with modified paths, assert: new paths added, removed paths have `Status = Missing`, unchanged paths untouched
- Test that FilePath unique index prevents duplicate insertion

**Platform detection (folder matching, extension filtering):**
- Unit test `PlatformRegistry.Resolve()` with exact name, alias, different casing, unknown folder
- Unit test extension filtering: files with allowed extensions pass; others are excluded
- No DB or file system required

**File hash computation:**
- Create a small temp file with known bytes in a test fixture
- Assert CRC32 output matches expected 8-char hex (use a CRC32 reference value computed independently)
- Assert MD5 and SHA1 outputs match known reference values
- Do not use large ISO files in tests — use a 4KB synthetic file

**Favorites and recently played tracking:**
- Use in-memory SQLite; insert test Game records
- Toggle IsFavorite twice; assert final DB state
- Insert games with varying LastPlayedAt; assert top-15 query returns correct order
- Assert that a game with `LastPlayedAt = null` does not appear in recently played results

### Sampling Rate
- **Per task commit:** `dotnet test tests/YARL.Tests/ -x`
- **Per wave merge:** `dotnet test tests/YARL.Tests/`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `tests/YARL.Tests/Scanning/RomScannerTests.cs` — covers LIB-02 (non-blocking, progress, cancellation), incremental upsert
- [ ] `tests/YARL.Tests/Scanning/PlatformRegistryTests.cs` — covers platform detection, alias matching, extension filtering
- [ ] `tests/YARL.Tests/Scanning/FileHasherTests.cs` — covers CRC32/MD5/SHA1 correctness
- [ ] `tests/YARL.Tests/Scanning/FilenameParserTests.cs` — covers title cleaning, region extraction
- [ ] `tests/YARL.Tests/Scanning/M3uParserTests.cs` — covers multi-disc playlist parsing
- [ ] `tests/YARL.Tests/Library/LibraryViewModelTests.cs` — covers LIB-03, LIB-06, LIB-07 (favorites, recently played, platform grid filtering)
- [ ] `tests/YARL.Tests/Foundation/MigrationTests.cs` — extend existing file to verify Games + RomFiles tables after new migration

---

## Sources

### Primary (HIGH confidence)
- [System.IO.Hashing NuGet 10.0.4](https://www.nuget.org/packages/System.IO.Hashing/) — CRC32 class, AppendAsync, GetCurrentHashAsUInt32
- [Crc32 Class — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32?view=net-10.0-pp) — Full method table verified
- [ReactiveUI Collections (DynamicData)](https://www.reactiveui.net/docs/handbook/collections) — SourceCache pattern, ObserveOn, Bind
- [BackgroundService — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) — IServiceScopeFactory pattern, ExecuteAsync signature
- [EF Core ExecuteUpdate — Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) — ExecuteUpdateAsync, EF Core 7+
- [ReactiveUI.SourceGenerators GitHub](https://github.com/reactiveui/ReactiveUI.SourceGenerators) — [Reactive] attribute, partial class requirement, version 2.6.1 matches csproj
- Existing codebase — `YarlDbContext`, `LocalRomSourceProvider`, `Program.cs`, `App.axaml.cs` all read directly

### Secondary (MEDIUM confidence)
- [Avalonia ScrollViewer Docs](https://docs.avaloniaui.net/docs/reference/controls/scrollviewer) — horizontal scroll pattern verified with official docs
- [DynamicData GitHub](https://github.com/reactivemarbles/DynamicData) — thread safety claim verified against official repo README
- [MD5.HashData — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.md5.hashdata?view=net-10.0) — static HashData method, .NET 7+

### Tertiary (LOW confidence)
- No-intro filename tag conventions — derived from multiple community sources (Emuparadise, Igir docs, rom-parse GitHub); regex pattern is researcher-composed based on examples
- .m3u format details — Libretro forums + community guides; format is simple and well-established but no single official spec

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages verified against NuGet and project csproj
- Architecture: HIGH — patterns sourced from official Microsoft Learn and ReactiveUI docs
- Pitfalls: HIGH for Pitfalls 1–5 (verified against official docs); MEDIUM for Pitfalls 6–7 (derived from API behavior)
- Validation architecture: HIGH — test framework and patterns match existing test project

**Research date:** 2026-03-18
**Valid until:** 2026-06-18 (stable ecosystem — Avalonia, EF Core, ReactiveUI all stable releases)
