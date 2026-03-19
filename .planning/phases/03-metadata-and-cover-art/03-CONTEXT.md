# Phase 3: Metadata and Cover Art - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Enrich the ROM library with metadata and cover art: scrape cover art, description, genre, year, developer, and publisher from ScreenScraper (primary) and IGDB (fallback); cache art locally; introduce `GameVersion` to group regional variants; add real-time per-platform search (LIB-04); add genre/year/developer filter chips (LIB-05); add a right-side game detail drawer with metadata and launch; achieve 60fps grid with 500+ items.

This phase does NOT include: emulator launch (Phase 4), fullscreen/gamepad mode (Phase 6), or custom collections (Phase 5).

</domain>

<decisions>
## Implementation Decisions

### Scraping Trigger and Scope
- **Auto-scrape after scan**: scraping kicks off automatically for new/unscraped games whenever a scan completes (background, non-blocking)
- **Manual re-scrape**: per-game re-scrape button in the detail drawer; global "Re-scrape library" option in Settings
- **Matching pipeline**: CRC32 hash match → title-search fallback → mark as unmatched with "No art" badge
- **Interactive search**: unmatched games have an "Interactive search" option (like Radarr/Sonarr) — opens a search dialog showing API results with boxart thumbnails; user picks the correct match manually
- **Progress lives in Settings**: a dedicated scraping status panel in Settings shows progress (X/Y scraped), rate-limit pauses, and any unmatched games. Main UI is uncluttered during scraping
- **Unmatched badge**: games that failed all auto-matching get a subtle "No art" badge on their tile; badge disappears once art is found (manually or via re-scrape)

### Cover Art Display
- **Tile layout**: cover art fills the dominant area of the tile, game title as caption below — classic launcher look
- **Platform-themed border**: each tile has a decorative border/accent colored or styled to the game's platform (e.g. SNES purple, PS1 grey). The border surrounds the cover art area
- **Play button overlay**: a play button is overlaid directly on the cover art, positioned bottom-right. Visible on hover/focus. Clicking it launches the game (using preferred region ROM)
- **Art type priority**: box front cover first; gameplay screenshot as fallback when no box art available
- **Placeholder tiles**: existing `#0f3460` placeholder border remains until art is fetched; "No art" badge shown for confirmed unmatched games

### Game Detail Drawer
- **Right-side drawer**: clicking a game tile slides in a right-side drawer over the game list (not a new page, not a full modal). Drawer shows:
  - Large boxart
  - Title (English name by default)
  - Aliases (regional names, e.g. Japanese title) — shown in a collapsible/expandable list
  - Description, genre, year, developer, publisher
  - All available versions/regional variants with flag icons — user can see which ROMs are available
  - Play button (launches preferred-region ROM)
  - Edit button (triggers inline edit mode for META-04)
  - Per-game preferred region override picker
- **Manual metadata override (META-04)**: Edit button in the drawer makes fields editable inline. User can edit title, description, genre, year, developer, publisher. Cover art can be replaced by picking a local image file. Saves to DB as user overrides (not overwritten by future re-scrapes)

### Search (LIB-04)
- **Per-platform inline search**: search box in the GameListView header bar; filters games within the current platform view
- **Result cap**: results capped at 50; "Load more" button at the bottom to show next 50
- **Status text**: above the results area, show: `X matches in [Platform Name] / Y matches globally`
- **Global search pivot**: a small button/link next to the status text lets the user remove the platform filter and search globally across all platforms
- **Searchable fields**: game title (English) + all regional name aliases must be matched — e.g. searching "Kirby no" finds a Japanese-titled game even when browsing from the English view

### Filters (LIB-05)
- **Filter chips below search bar**: a chip row beneath the search box shows active filters; Genre dropdown, Year range, Developer dropdown. Selecting a filter adds it as a dismissible chip
- **Chips are combinable**: all active filters apply together (AND logic)
- **Clear all**: a "Clear filters" link or ✕ button removes all active chips at once

### Regional Variant Grouping (GameVersion)
- **One tile per canonical game**: the game grid shows one tile per game title. When multiple ROMs exist for the same game (USA, Europe, Japan variants), they are grouped under one `GameVersion` record. The tile shows the canonical English title
- **English name default**: the English name is shown as the tile title and in the detail drawer. Regional/Japanese names shown as aliases in an expandable list inside the detail drawer
- **All aliases searchable**: all regional name aliases count for search matching (see Search section above)
- **Detail drawer versions**: the drawer shows all available versions/ROMs with flag icons (🇺🇸 USA, 🇪🇺 Europe, 🇯🇵 Japan, etc.). User can see which files are present
- **Play button picks preferred region**: the tile's play button auto-selects the ROM matching the global preferred region setting (configurable in Settings). Default: USA → Europe → Japan → first available
- **Per-game region override**: in the detail drawer, the user can set a preferred region specifically for this game (overrides the global setting). Stored in DB

### Claude's Discretion
- Image storage format and directory structure for cached cover art (e.g. `{AppData}/yarl/art/{platformId}/{gameId}.jpg`)
- Image loading async pattern in Avalonia (background thread → UI update; placeholder while loading)
- ScreenScraper and IGDB API client implementation details (rate limiting, retry policy, auth token storage)
- Exact drawer slide-in animation style
- How GameVersion grouping handles edge cases (no ScreenScraper ID found, conflicting hash matches)
- Search debounce timing (typically 200–300ms after last keystroke)
- Filter chip visual styling (inherits existing `#7C6FF7` accent, matching nav active state)

</decisions>

<specifics>
## Specific Ideas

- Interactive search UX modeled after Radarr/Sonarr's "Interactive Search" — shows a results list with thumbnails and metadata from the API; user clicks a result to assign it to the game
- Platform-themed tile border is a design detail the user specifically wants — the accent color/style should visually signal which platform a game belongs to (important for "All Games" and "Favorites" cross-platform views where platform context is otherwise lost)
- Play button overlay on tile bottom-right — should be large enough to tap (Steam Deck touch consideration) but not dominate the art
- Regional alias list in detail drawer is collapsible ("Also known as: ▶") — avoids cluttering the default view
- Search result count format: `X matches in [Platform] / Y total globally` with a link-style "search globally" action

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — Phase 3 requirements: META-01, META-02, META-03, META-04, LIB-04, LIB-05

### Phase 2 foundation (integration points)
- `src/YARL/Domain/Models/Game.cs` — Extend with metadata fields (Description, Genre, Year, Developer, Publisher, CoverArtPath, ScraperMatchId, IsMetadataOverridden). Add EF Core migration
- `src/YARL/UI/ViewModels/GameViewModel.cs` — Extend with metadata properties (CoverArtPath, Description, Genre, Year, Developer, Publisher); ImageSource reactive property for loaded art
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` — Add SearchText, SearchResults, ActiveFilters reactive properties; GameVersion grouping logic; scraping commands
- `src/YARL/UI/Views/GameListView.axaml` — Add search bar + filter chip row to header; grid tiles need cover art Image control + play button overlay + platform border
- `src/YARL/UI/Views/PlatformGridView.axaml` — Platform tiles still use placeholder; Phase 3 adds optional platform logo image if available
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` — Add GameVersion entity, new EF Core migration for metadata fields

### Prior context
- `.planning/phases/02-rom-scanning-and-library/02-CONTEXT.md` — 3-tier model spec, hash strategy (CRC32/MD5/SHA1), ROM data model, Game entity fields baseline
- `.planning/STATE.md` — Key decisions log including ScreenScraper CRC32 format, EF Core HasConversion pattern, DynamicData pipeline patterns

### Project context
- `.planning/PROJECT.md` — Metadata sources context: ScreenScraper primary, IGDB fallback, TheGamesDB candidate
- `.planning/ROADMAP.md` — Phase 3 goal, success criteria (60fps with 500+ items)

No external API specs available locally — researcher should verify ScreenScraper API v2 and IGDB API v4 rate limits, auth models, and artwork endpoint structures.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BoolToHeartColorConverter`, `BoolToOpacityConverter`, `GameStatusToVisibilityConverter` — existing converters in `src/YARL/UI/Converters/`; add `NullToPlaceholderConverter` and `PathToImageConverter` for cover art
- `[Reactive]` source generator pattern — already used across all ViewModels; CoverArtPath and metadata fields follow same pattern
- `LibraryViewModel._gamesSource` SourceCache — search and filter pipelines chain off the existing `Connect()` observable
- `ScanStatusBar` pattern — scraping progress panel in Settings can follow the same status/cancel pattern

### Established Patterns
- DynamicData `Filter()` + `WhenAnyValue()` — search/filter reactive pipeline extends the existing platform filter in LibraryViewModel; `SearchText` and active filters drive additional `Filter()` operators
- EF Core `HasConversion<string>()` — use same pattern for any new enum fields
- Compiled Avalonia bindings (`x:DataType`) — all new AXAML views need `x:DataType` declarations
- `ItemsControl + WrapPanel` for grid layout — already proven; game tiles extend existing pattern (no ItemsRepeater/UniformGrid — not in Avalonia 11.3.12)

### Integration Points
- `LibraryViewModel.FilteredGames` → extend with SearchText + filter chip predicates stacked on top of existing platform/favorites filter
- `GameListView.axaml` header bar → add search TextBox + filter chip row (currently: Back | Title | Count | FavToggle | GridToggle)
- `Game` entity → add metadata columns; EF Core migration builds on `20260318234511_AddGameAndRomFile`
- `Program.cs` DI → register scraper service, scraping background service, image cache service

</code_context>

<deferred>
## Deferred Ideas

- None — discussion stayed within Phase 3 scope

</deferred>

---

*Phase: 03-metadata-and-cover-art*
*Context gathered: 2026-03-19*
