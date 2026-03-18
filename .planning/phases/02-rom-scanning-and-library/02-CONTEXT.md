# Phase 2: ROM Scanning and Library - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Users configure ROM folder paths, a background scanner discovers games organized by platform, and the library becomes browsable with live updates during scanning. Favorites and recently played are tracked. The product is no longer inert after this phase.

This phase does NOT include: metadata scraping (Phase 3), emulator launch (Phase 4), or ROM cache (Phase 7).

</domain>

<decisions>
## Implementation Decisions

### Platform Detection
- **Folder-based**: parent folder name is matched against `platforms.json` — this is the primary and only detection method
- **Matching rule**: exact match (case-insensitive) against `Name` field OR any entry in the `Aliases` array in platforms.json
- **Non-matching folders**: skip silently during scanning but record them in a scan report the user can review after a scan completes
- **Scan depth**: recursive — scan all subdirectories within a platform folder (supports collections like `/ROMs/SNES/RPGs/`)
- **Extension filtering**: `platforms.json` is authoritative for allowed extensions per platform; scanner reads the `Extensions` array from each platform entry at runtime
- **Multi-disc games**: `.m3u` playlist files are treated as the game entry; the individual disc files (.iso, .bin, etc.) referenced inside the .m3u are that game's RomFiles with DiscNumber set. Non-.m3u disc files in the same folder are imported as separate RomFiles linked to their own Game record (Phase 3 metadata will group them)
- **Unrecognized extensions**: excluded — only files matching a platform's Extensions allowlist are imported

### Library Browse Model
- **Two view modes**: platform grid view AND flat list view, switchable by the user at runtime
- **Platform grid**: tiles showing only platforms with ≥1 scanned ROM (empty platforms are hidden)
- **Placeholder art**: platform logo image used as placeholder before Phase 3 cover art arrives; platform logo assets must be bundled (or generated from platform color + name as fallback if logo assets are missing)
- **Game tile content**: cleaned title (strip region/version tags from filename) + placeholder art + platform badge. No cover art, genre, or year until Phase 3
- **Title cleaning**: strip common no-intro tags from filenames — region tags `[USA]`, `[Europe]`, `[Japan]`, `[World]`, `(v1.1)`, `(Rev A)`, `(Disc 1)`, file extension
- **Recently Played (LIB-07)**: horizontal carousel row at the top of the main library view ("Continue Playing"); shows last 10–15 games by LastPlayedAt timestamp
- **Favorites (LIB-06)**: horizontal carousel row on home view (top, above Recently Played or below it) AND a favorites toggle/filter within each platform's game list
- **Empty state**: if no ROM sources configured → "Add your ROM folders to get started" prompt with CTA to add a source; if sources configured but nothing scanned yet → show scanning progress

### Scan Triggers and Progress
- **Auto-scan on app startup** (background, non-blocking) + **on-demand Rescan button** in the UI
- **Progress indicator**: status bar at the bottom of the window — text updates like "Scanning SNES... 142 games found" as the scanner moves through platforms; disappears or shows "Scan complete — 1,847 games" when finished
- **Live library updates**: platform tiles and game lists update in real-time as games are discovered during scanning; the UI is never blocked (satisfies LIB-02)
- **Incremental rescan**: compare file paths on disk vs database — add new files, mark removed files as Missing, leave existing records and their metadata untouched. Never wipe and reimport.
- **Missing files**: when a scanned file is no longer found on disk, mark its Game record as `Status = Missing` and grey it out in the library. If the file reappears on a future scan, restore to `Status = Active`. This preserves favorites, metadata, and play history.
- **File hashing**: compute **CRC32, MD5, and SHA1** for every RomFile at scan time (in background). CRC32 is the ScreenScraper primary; MD5/SHA1 cover No-Intro and other scraping sources. Hash computation happens after the file is imported so the UI isn't delayed waiting for hashing large ISOs.
- **Scan report**: after each scan, store a lightweight summary of: platforms found, games added, games removed (marked missing), unmatched folders (skipped). User can view this from the UI.

### ROM Data Model
- **3-tier eventual model**: `Game → GameVersion → RomFile` is the target architecture. Phase 2 implements `Game → RomFile` only; `GameVersion` is introduced in Phase 3 when the scraper can assign canonical game IDs and group regional variants.
- **At scan time**: each file (or .m3u playlist) creates its own `Game` record — no automatic grouping across files. Phase 3 metadata will link multiple Games that are regional variants of the same title by introducing a `GameVersion` join layer.
- **Region detection at scan time**: parse region/version tags from filename (`[USA]`, `[Europe]`, `[Japan]`, `[World]`, `(Rev A)`, etc.) into a `Region` field on the Game. Phase 3 metadata overwrites/enriches this with authoritative region data from the scraper.

**`Game` entity fields (Phase 2):**
```
Id             int PK
Title          string (cleaned filename — strip tags)
RawTitle       string (original filename without extension)
PlatformId     string (FK to platforms.json Id field)
SourceId       int FK → RomSources.Id
IsFavorite     bool default false
LastPlayedAt   DateTime? nullable
CreatedAt      DateTime
Status         enum: Active | Missing | Hidden
Region         string? (parsed from filename tags — e.g. "USA", "Europe")
```

**`RomFile` entity fields (Phase 2):**
```
Id             int PK
GameId         int FK → Games.Id
FilePath       string (full absolute path)
FileName       string
FileSize       long (bytes)
CRC32Hash      string? (computed async after import)
MD5Hash        string? (computed async after import)
SHA1Hash       string? (computed async after import)
DiscNumber     int? (1-based; null for single-disc/non-disc games)
IsM3uPlaylist  bool (true if this record represents a .m3u file)
SourceId       int FK → RomSources.Id
CreatedAt      DateTime
```

### Claude's Discretion
- Exact platform logo asset format and storage location (embedded resources vs file-based)
- Precise title-cleaning normalization rules beyond common no-intro tags
- Scan concurrency level (how many platforms scan in parallel)
- Hash computation batching strategy (how many files hashed concurrently)
- Exact status bar update frequency
- How the scan report is stored and displayed

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — Phase 2 requirements: LIB-01, LIB-02, LIB-03, LIB-06, LIB-07

### Phase 1 foundation (integration points)
- `src/YARL/Domain/Interfaces/IRomSourceProvider.cs` — Scanner must use EnumerateRomsAsync from this interface; extend, don't replace
- `src/YARL/Domain/Models/RomSource.cs` — ROM sources entity (Path, SourceType, Label, IsEnabled); Game.SourceId FKs to this
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` — Add Game and RomFile DbSets here; run EF Core migration
- `src/YARL/Infrastructure/Config/AppPaths.cs` — Use AppPaths for any new file-based storage (scan reports, etc.)
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` — Phase 2 expands this stub with ROM collection, scan state, filtering
- `src/YARL/Data/platforms.json` — Authoritative source for platform Names, Aliases, and Extensions allowlists; scanner reads this at runtime

### Phase 1 decisions in STATE.md
- `.planning/STATE.md` — Key decisions: ReactiveUI.SourceGenerators available for [Reactive] property generation; DesktopShell/FullscreenShell both bind to LibraryViewModel

No external specs — requirements are fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IRomSourceProvider.EnumerateRomsAsync(RomSource, CancellationToken)` — async enumerable of file paths; scanner calls this per RomSource rather than using Directory.EnumerateFiles directly
- `LocalRomSourceProvider` + `OsMountedRomSourceProvider` — already registered in DI; scanner just needs to resolve `IEnumerable<IRomSourceProvider>`
- `ReactiveUI.SourceGenerators` — use `[Reactive]` attribute on LibraryViewModel properties instead of manual `RaiseAndSetIfChanged` boilerplate
- `YarlDbContext` with `DbSet<RomSource>` — add `DbSet<Game>` and `DbSet<RomFile>` here; EF Core migration already exists as baseline

### Established Patterns
- EF Core + SQLite: string conversion via `HasConversion<string>()` (used for SourceType; use same for Game.Status enum)
- XDG paths via `AppPaths` static class — any new directories (e.g. scan report cache) should go through AppPaths
- Compiled Avalonia bindings (`AvaloniaUseCompiledBindingsByDefault=true`) — AXAML views require `x:DataType` declarations
- ReactiveUI IScreen + routing via MainViewModel — new views navigate through `MainViewModel.Router`

### Integration Points
- `LibraryViewModel` — Phase 2 hub: add `ObservableCollection<PlatformViewModel>`, `ObservableCollection<GameViewModel>`, scanning status string, rescan command
- `YarlDbContext` — add Game and RomFile entities, new EF Core migration
- `App.axaml.cs` startup — scanner should be triggered here after DI is wired, or via a startup service registered with Generic Host
- `Program.cs` DI registration — register scanner service, background scan host service

</code_context>

<specifics>
## Specific Ideas

- Game entity has a `Status` enum (Active | Missing | Hidden) — missing files are greyed out, not deleted
- 3-tier data model target: Game → GameVersion → RomFile (Phase 3 introduces GameVersion when scraper groups regional variants)
- Region parsing from no-intro filename tags (`[USA]`, `[Europe]`, `[Japan]`, `[World]`) gives useful data before metadata arrives
- Hash all three: CRC32 (ScreenScraper primary), MD5 and SHA1 (No-Intro, other databases) — compute async in background after import
- Scan report captures unmatched folders so user knows which folders didn't map to any platform

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-rom-scanning-and-library*
*Context gathered: 2026-03-18*
