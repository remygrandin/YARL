# Roadmap: YARL — Yet Another Retro Launcher

## Overview

YARL is built in seven phases that follow the natural dependency order of its features. Phase 1 establishes the architectural foundation that every other phase builds on — the wrong choice here costs one or two phases of rework. Phases 2 through 4 deliver the core product loop: scan ROM collection, display it with cover art, launch a game. Phases 5 and 6 complete the two UI modes (desktop and fullscreen/gamepad) using the proven ViewModels from earlier phases. Phase 7 adds the friction-reducers — one-click emulator install and ROM caching — that are best validated after the core loop is working.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation** - Project scaffolding, architecture decisions, dual view shells, CI, database migration framework (completed 2026-03-18)
- [x] **Phase 2: ROM Scanning and Library** - Background ROM scanner, library browsing by platform, favorites and recently played (completed 2026-03-19)
- [x] **Phase 3: Metadata and Cover Art** - Rate-limited metadata scraping, cover art, search and filters (completed 2026-03-20)
- [ ] **Phase 4: Emulator Integration and Launch** - Emulator configuration, ROM launch with Flatpak abstraction
- [ ] **Phase 5: Desktop UI Completion** - Full desktop mode, custom collections, controller-navigable settings
- [ ] **Phase 6: Fullscreen and Gamepad Mode** - Full fullscreen view tree, D-pad navigation, Gamescope verification
- [ ] **Phase 7: Emulator Management and ROM Cache** - One-click emulator install, recommended defaults, ROM cache with LRU eviction

## Phase Details

### Phase 1: Foundation
**Goal**: A runnable skeleton app on both Windows and Linux with the architectural decisions made that cannot be safely retrofitted later
**Depends on**: Nothing (first phase)
**Requirements**: LIB-08
**Success Criteria** (what must be TRUE):
  1. App launches on Windows and on Linux (tested on Steam Deck or any Debian/Arch Linux)
  2. SQLite database initializes with EF Core migrations on first run, and migration runs cleanly on upgrade from a previous schema version
  3. Both DesktopShell and FullscreenShell stubs exist, share a LibraryViewModel, and are navigable via a debug toggle
  4. A remote path can be tagged as "OS-mounted" and treated through the IRomSourceProvider interface without special-casing
  5. Linux CI pipeline runs and passes on every commit
**Plans:** 3/3 plans complete

Plans:
- [x] 01-01-PLAN.md — Solution scaffold, domain model, database context, config system
- [x] 01-02-PLAN.md — Dual shells, ViewModels, DI wiring, EF Core migration
- [x] 01-03-PLAN.md — Test project, foundation tests, GitHub Actions CI

### Phase 2: ROM Scanning and Library
**Goal**: Users can point YARL at their ROM folders and browse their collection organized by platform — the product is no longer inert
**Depends on**: Phase 1
**Requirements**: LIB-01, LIB-02, LIB-03, LIB-06, LIB-07
**Success Criteria** (what must be TRUE):
  1. User can add a folder path and YARL scans it in the background without freezing the UI, even for collections of 10,000+ files
  2. User can browse ROMs organized by platform in a cover art grid (filenames shown; cover art comes in Phase 3)
  3. Scan progress is visible and the scan can be cancelled mid-run
  4. User can mark a game as a favorite and see their favorites filtered separately
  5. App tracks the last-launched time for each game and shows a "recently played" list
**Plans:** 4/4 plans complete

Plans:
- [ ] 02-01-PLAN.md — Data foundation: Game + RomFile entities, EF Core migration, platforms.json aliases, Wave 0 test stubs
- [ ] 02-02-PLAN.md — ROM scanner engine: PlatformRegistry, FilenameParser, incremental scan, FileHasher
- [ ] 02-03-PLAN.md — Library ViewModel: DynamicData SourceCache, PlatformViewModel, GameViewModel, favorites + recently played
- [ ] 02-04-PLAN.md — Library UI views: sidebar, platform grid, game list, carousels, status bar, empty state, dialog

### Phase 3: Metadata and Cover Art
**Goal**: The ROM library displays cover art, descriptions, and metadata — transforming a file list into a browsable collection
**Depends on**: Phase 2
**Requirements**: META-01, META-02, META-03, META-04, LIB-04, LIB-05
**Success Criteria** (what must be TRUE):
  1. Selecting "scrape metadata" fills in cover art, description, genre, year, developer, and publisher for games found on ScreenScraper or IGDB
  2. User can manually edit any metadata field for a game and the override survives a re-scrape
  3. User can search games by name and see results filter in real time
  4. User can filter the game list by genre, year, and developer
  5. Cover art grid loads at 60fps with 500+ items (no stutter from image decoding)
**Plans:** 6/6 plans complete

Plans:
- [ ] 03-01-PLAN.md — Data foundation: Game metadata fields, GameVersion entity, ScrapeStatus enum, EF Core migration, platforms.json enrichment, Wave 0 test stubs
- [ ] 03-02-PLAN.md — Scraping infrastructure: ScreenScraperClient, IgdbClient, ScraperPipeline, ArtCacheService, NuGet packages
- [ ] 03-03-PLAN.md — Search/filter ViewModel: GameViewModel metadata, DynamicData search + multi-filter pipeline, GameDetailViewModel
- [ ] 03-04-PLAN.md — Scraper integration: ScraperHostedService, DI wiring, ScrapingStatusViewModel, auto-scrape after scan
- [ ] 03-05-PLAN.md — Game list UI: ItemsRepeater virtualized grid, cover art tiles, search bar, filter chips
- [ ] 03-06-PLAN.md — Detail drawer + Settings: GameDetailDrawer, InteractiveSearchDialog, scraping progress panel

### Phase 4: Emulator Integration and Launch
**Goal**: Users can launch any game in the correct emulator on both Windows and Linux, including via Flatpak on Steam Deck
**Depends on**: Phase 3
**Requirements**: EMU-01, EMU-02
**Success Criteria** (what must be TRUE):
  1. User can configure an emulator path and launch arguments per platform
  2. Clicking "Launch" on a game opens it in the configured emulator with the correct ROM path passed as an argument
  3. Launching a Flatpak emulator on Linux works without path permission errors
  4. The app records the launch time and updates the recently played list after a game is launched
**Plans:** 1/5 plans executed

Plans:
- [ ] 04-01-PLAN.md — Wave 0 test stubs for Phase 4 (EmulatorConfig, GameLaunchService, LaunchOverlay)
- [ ] 04-02-PLAN.md — Data model + config (EmulatorConfig record, TotalPlayTime migration) + GameLaunchService
- [ ] 04-03-PLAN.md — Launch overlay VM + UserControl, Play button wiring, DesktopShell integration
- [ ] 04-04-PLAN.md — Settings EMULATORS section (EmulatorRowViewModel, per-row config, file picker, Flatpak)
- [ ] 04-05-PLAN.md — Full test suite regression check + human verification of end-to-end flow

### Phase 5: Desktop UI Completion
**Goal**: The desktop experience is complete — grid/list views, sidebar navigation, filters, custom collections, and settings all work end-to-end
**Depends on**: Phase 4
**Requirements**: UI-01, UI-03, UI-04, LIB-09
**Success Criteria** (what must be TRUE):
  1. User can switch between grid view and list view from the desktop UI
  2. User can navigate the entire desktop UI (sidebar, game grid, game detail, settings) using only a controller/gamepad
  3. User can switch between desktop mode and fullscreen mode at runtime without restarting the app
  4. User can create a custom collection containing games from multiple platforms
**Plans**: TBD

### Phase 6: Fullscreen and Gamepad Mode
**Goal**: The fullscreen/gamepad experience is complete and usable on Steam Deck — all screens reachable by D-pad, no mouse required
**Depends on**: Phase 5
**Requirements**: UI-02
**Success Criteria** (what must be TRUE):
  1. User can navigate from platform select to game grid to game detail to launch using only D-pad and face buttons
  2. User can always return to the library from any screen without using a mouse or keyboard
  3. Fullscreen mode runs correctly under KDE Plasma, Gamescope (Steam Deck game mode), and X11
  4. Cover art is displayed large and the text content is minimal and readable from couch distance
**Plans**: TBD

### Phase 7: Emulator Management and ROM Cache
**Goal**: Users can install emulators with one click and remote ROM collections are transparently cached for offline/fast launch on Steam Deck
**Depends on**: Phase 4
**Requirements**: EMU-03, EMU-04, EMU-05, CACHE-01, CACHE-02, CACHE-03, CACHE-04, CACHE-05
**Success Criteria** (what must be TRUE):
  1. User can install a recommended emulator for a platform with one click on both Windows (binary) and Linux (Flatpak)
  2. YARL shows a recommended emulator for each platform and pre-fills configuration when one is installed
  3. Launching a remote-path ROM transparently copies it to the local cache before launching — user sees progress and the game launches
  4. User can set a maximum disk usage for the ROM cache and see current space used per-game
  5. User can clean up cached ROMs by last-launched time threshold (1mo / 3mo / 6mo / 1yr) with a confirmation step
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7
Note: Phase 7 depends on Phase 4, not Phase 6 — it can begin as soon as Phase 4 is complete if Phase 6 is parallelized.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 3/3 | Complete   | 2026-03-18 |
| 2. ROM Scanning and Library | 4/4 | Complete   | 2026-03-19 |
| 3. Metadata and Cover Art | 6/6 | Complete   | 2026-03-20 |
| 4. Emulator Integration and Launch | 1/5 | In Progress|  |
| 5. Desktop UI Completion | 0/TBD | Not started | - |
| 6. Fullscreen and Gamepad Mode | 0/TBD | Not started | - |
| 7. Emulator Management and ROM Cache | 0/TBD | Not started | - |

---
*Roadmap created: 2026-03-18*
*Last updated: 2026-03-30 after Phase 4 planning complete*
