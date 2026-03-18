# Project Research Summary

**Project:** YARL — Yet Another ROM Launcher
**Domain:** Cross-platform ROM browser / manager / launcher (.NET + Avalonia UI)
**Researched:** 2026-03-18
**Confidence:** MEDIUM

## Executive Summary

YARL is a cross-platform ROM frontend and manager targeting both Windows desktop and Steam Deck, a combination no existing tool handles well. The competitive landscape is split between Windows-only heavyweights (Playnite, LaunchBox) that miss Linux entirely, and cross-platform frontends (ES-DE, Pegasus) that are browse-only with no emulator management, no dual UI mode, and no recommendation engine. The recommended approach is a layered .NET 10 + Avalonia UI architecture following hexagonal/ports-and-adapters principles: a pure domain core, provider-abstracted infrastructure (storage, scrapers, emulators), and two independent view shells (Desktop, Fullscreen) sharing the same ViewModel layer. EF Core migrations over SQLite handle the catalog, and a background streaming pipeline with `Channel<T>` handles the scan-to-display flow without blocking the UI.

The three genuine differentiators this project can own are: (1) first-class Steam Deck citizenship — meaning controller navigation designed in from day one, Flatpak-aware emulator launch, and storage-budget-aware caching; (2) a recommendation engine based on play history and metadata similarity, which no existing lightweight frontend offers; and (3) a dual-mode UI (desktop grid and fullscreen/gamepad) available for free, while competitors either lack one mode or charge for it. These differentiators are only meaningful if the table-stakes features — ROM scanning, metadata scraping, cover art grid, launch — work reliably first. The risk is building polish before foundation.

The critical risks fall into two categories: architectural decisions that are very expensive to retrofit (controller navigation model, Flatpak launch abstraction, database migration framework, dual view shell separation) and operational traps that only appear at scale (API rate-limit bans, image loading stutter at 500+ items, ROM scanner UI freeze on large collections). Both categories must be addressed in the foundation phase, not deferred. Anything deferred to "polish" in these categories will cost one to two sprint regressions to fix.

---

## Key Findings

### Recommended Stack

The stack is built on .NET 10 LTS (confirmed current as of March 2026) with Avalonia UI 11.x as the only viable cross-platform .NET UI framework with first-class Linux support. WPF and MAUI are both ruled out — WPF is Windows-only, MAUI has no production Linux desktop target. ReactiveUI is the standard MVVM framework for Avalonia and its `IScreen`/`RoutingState` model maps directly to the gamepad navigation stack requirement. EF Core 10 with SQLite provides the catalog database with a migration story essential for a desktop app that upgrades in-place without a server. The HTTP resilience stack (Polly 8.x, typed HttpClient, System.Text.Json) handles the rate-limited metadata API calls. SharpCompress + Force.Crc32 handle ROM archive inspection and hash computation for ScreenScraper's hash-based lookup.

See [STACK.md](.planning/research/STACK.md) for full library table, version constraints, and installation commands.

**Core technologies:**
- **.NET 10 LTS**: Runtime and SDK — confirmed current LTS, 3-year support horizon, C# 14
- **Avalonia UI 11.x**: Cross-platform UI — only mature .NET framework with first-class Linux/Wayland support
- **ReactiveUI 20.x**: MVVM framework — Avalonia's default; `IScreen`/routing maps to fullscreen navigation model
- **FluentAvalonia**: UI theme — `NavigationView` maps directly to sidebar+content layout needed for desktop mode
- **EF Core 10 + SQLite**: Catalog database — migrations essential for in-place upgrades; structured schema for ROM metadata
- **ScreenScraper API (primary) + IGDB (fallback)**: Metadata sources — hash-based ROM lookup (ScreenScraper) is uniquely accurate for retro platforms
- **Polly 8.x**: HTTP resilience — rate limits and transient failures from both APIs require exponential backoff and circuit breakers
- **SharpCompress + Force.Crc32**: Archive and hashing — read ROM archives without extraction; hardware-accelerated CRC32 for ScreenScraper
- **System.Threading.Channels**: Scan pipeline — bounded `Channel<T>` for background scanner to UI without memory runaway
- **Serilog 4.x**: Structured logging — rolling file sink critical for diagnosing emulator launch failures in the field

### Expected Features

See [FEATURES.md](.planning/research/FEATURES.md) for full feature dependency graph, competitor analysis, and prioritization matrix.

**Must have (table stakes — v1):**
- ROM folder scanning (background, non-blocking) — foundation; product is inert without it
- Browse by platform with cover art grid — the core visual promise of any ROM frontend
- Metadata scraping (ScreenScraper primary, IGDB fallback) — transforms filenames into a browsable collection
- Launch game in correct emulator — the single most important user action
- Emulator configuration per platform — prerequisite to launching anything
- Favorites and recently played — minimum expected personalization
- Search by name and filter by genre/platform/year — essential for collections over 50 games
- Manual metadata override — scrapers are wrong; users must be able to fix without re-scrape overwriting
- Custom collections (cross-platform) — curation differentiates from file browsers
- Desktop mode (grid + list + sidebar) and fullscreen/gamepad mode — both required for the dual target audience
- Controller/gamepad navigation — required for Steam Deck; fullscreen mode is unusable without it
- OS-mounted path support — NAS users are a large segment; covers them without SMB complexity
- Recommendation system (genre/developer/publisher similarity) — stated project requirement; genuine differentiator

**Should have (v1.x — add after core validation):**
- One-click emulator install — reduces setup friction; validate demand first
- Recommended emulator per platform — opinionated defaults; most useful paired with emulator install
- ROM cache with LRU eviction — essential for Steam Deck NAS users; validate remote path usage first
- Auto-configure emulator settings — high complexity; validate manual config is the actual friction point first

**Defer (v2+):**
- Direct SMB/CIFS browsing — design provider interface now, implement when demand is clear
- Cloud sync (Google Drive, Dropbox, OneDrive) — separate OAuth complexity per provider
- RetroAchievements integration — delegate to RetroArch layer in v1
- Theme system — high effort, moderate value for YARL's positioning
- Plugin/extension system — Playnite's biggest maintenance burden; avoid for v1

**Anti-features (never build):**
- ROM downloading — legal liability is existential; DMCA risk; document "bring your own ROMs"
- BIOS bundling — copyright violation; guide users to sources and validate BIOS presence with clear errors
- Netplay — emulator responsibility, not frontend responsibility

### Architecture Approach

The architecture follows a strict 5-layer structure: Presentation (Avalonia MVVM), Application (orchestration services), Domain (pure models + interfaces), Infrastructure (external adapters), and Persistence (SQLite + file cache + config). The critical architectural decision is two independent view shells (`DesktopShell`, `FullscreenShell`) sharing the same ViewModel layer — not a single adaptive view with a mode toggle. This is the only approach that makes controller focus management tractable. All external dependencies (storage sources, metadata scrapers, emulator installers) are abstracted behind interfaces (`IRomSourceProvider`, `IMetadataScraper`, `IEmulatorInstaller`) registered at the DI composition root, enabling clean extensibility without core changes.

See [ARCHITECTURE.md](.planning/research/ARCHITECTURE.md) for the full system diagram, data flow charts, component communication map, and recommended project structure.

**Major components:**
1. **ROM Scanner** — background async stream pipeline (`IAsyncEnumerable`) over `IRomSourceProvider`; yields `ScanResult` per file; never blocks UI
2. **Library Service** — orchestrates scan → persist → notify; single source of truth for library state
3. **Metadata Service** — priority cascade: manual override → cached → live scrape; `scraped_at` staleness threshold prevents re-scrape spam
4. **Emulator Manager** — resolves emulator definition from config-driven `emulators.json`; invokes `IProcessLauncher` with CLI template expansion; tracks launched process PID
5. **Cache Manager** — intercepts launch for remote paths; LRU eviction by `last_launched_at`; runs as background service
6. **Collection Engine** — operates on top of populated library; favorites, play history, recommendations
7. **Platform Registry / Emulator Registry** — config-driven seed data (`platforms.json`, `emulators.json`); community-contributable without code changes

### Critical Pitfalls

See [PITFALLS.md](.planning/research/PITFALLS.md) for full pitfall catalog, integration gotchas, performance traps, and security mistakes.

1. **Controller navigation as afterthought** — Must define navigation model (focus scopes, back-action per view, modal stack) in Phase 1. Retrofitting is a 1–2 sprint regression. Every view spec must include "default focus element" and "Back action" as acceptance criteria.

2. **ROM scanner blocking the UI thread** — Use `Directory.EnumerateFiles` (lazy, not `GetFiles`), batch UI updates every 50–100 entries, pass `CancellationToken` everywhere. Benchmark: 10,000 dummy files scanned without UI freeze before merging the scanner.

3. **Metadata scraping rate-limit bans** — Build `RateLimitedHttpClient` with `SemaphoreSlim` (max 1–2 for ScreenScraper) before any scraping integration. Persist all API responses to disk. Read `requeststoday`/`maxrequestsperday` from ScreenScraper response headers. Test HTTP 429 handling before connecting to the real API.

4. **Flatpak emulator launch not abstracted** — Model launch method as a discriminated union (`NativeBinary`, `Flatpak`, `AppImage`, `SystemCommand`) from the initial emulator profile schema. Use `flatpak run --filesystem=host` to bypass sandbox path restrictions. Without this, emulator install works on Windows but silently fails on Steam Deck.

5. **SQLite schema without migration framework** — Configure EF Core migrations before the first feature touches the database. Store a separate `rom_metadata` key-value table for scraper fields rather than columns on the `roms` table — new metadata fields become inserts, not `ALTER TABLE`. Ship a migration-from-v0 integration test.

6. **Image loading stutter at scale** — Generate thumbnails at ingest time (display resolution, e.g., 200×280); never decode full-res images for the grid view. Implement bounded LRU image cache (200 items max). Verify `ItemsRepeater`/`VirtualizingStackPanel` actually virtualizes — Avalonia does not virtualize by default. Benchmark at 500+ items before declaring the grid done.

7. **Avalonia rendering differences between Windows and Linux** — Configure Linux CI (Docker + Xvfb or GitHub Actions Linux runner) before writing any UI code. Test `WindowState.FullScreen` under KDE Plasma, Gamescope (Steam Deck game mode), and X11 separately.

---

## Implications for Roadmap

The feature dependency graph and pitfall-to-phase mapping from research converge on a clear build order. Nothing can be built until the domain models and database exist. Nothing can be validated until the scan → display pipeline works end-to-end. The visual layer (cover art, metadata) is meaningless without ROMs in the library. Fullscreen mode is meaningless without proven ViewModels from desktop mode. The recommendation engine is meaningless without populated metadata and play history.

### Phase 1: Foundation and Infrastructure

**Rationale:** Every other phase depends on this. Domain models, database schema with migrations, project structure, DI composition root, Linux CI, and logging must exist before any feature work begins. Controller navigation model and dual view shell decision must also be made here — these are the two architectural choices that cannot be retrofitted.

**Delivers:** Runnable skeleton app on Windows and Linux; SQLite schema v1 with migration framework; `DesktopShell` and `FullscreenShell` stubs sharing a `LibraryViewModel`; Serilog file logging; Linux CI runner passing.

**Addresses:** OS-mounted path support (provider interface design), basic app navigation model

**Avoids:** Controller navigation as afterthought (Pitfall 1), SQLite schema migration gaps (Pitfall 9), Avalonia Windows/Linux rendering differences (Pitfall 5)

**Research flag:** Standard patterns — well-documented .NET + Avalonia project setup; no phase research needed.

---

### Phase 2: ROM Scanning and Library

**Rationale:** The entire product is inert without a populated ROM library. Scanning must precede everything else. This phase establishes the background pipeline pattern that metadata scraping and cache management will reuse.

**Delivers:** Non-blocking background ROM scanner with `Channel<T>`; `IRomSourceProvider` interface with `LocalStorageProvider` and `MountedPathProvider`; ROM records in SQLite; live-updating grid shell showing filenames (no cover art yet); cancellable scan with progress reporting.

**Addresses:** Browse by platform (filename level), recently played (schema), favorites (schema)

**Avoids:** ROM scanner blocking UI thread (Pitfall 3), `Directory.GetFiles` eager loading performance trap

**Research flag:** Standard patterns — well-documented async streaming in .NET; no phase research needed.

---

### Phase 3: Metadata Scraping and Cover Art

**Rationale:** Cover art and metadata transform a file list into a browsable collection. This is when the product's visual promise becomes real. Must not begin without the rate-limited HTTP client in place — connecting to ScreenScraper without throttling risks a permanent account ban.

**Delivers:** `RateLimitedHttpClient` with Polly backoff; ScreenScraper adapter (hash-based lookup, CRC32/MD5); IGDB adapter (Twitch OAuth, fuzzy name search); metadata cascade (manual override → cached → live scrape); `scraped_at` staleness threshold; cover art stored as files on disk (not BLOBs); thumbnail generation pipeline; async image loading in grid view with LRU cache.

**Addresses:** Metadata scraping, cover art display, manual metadata override, filter by genre/year/developer (enabled by metadata)

**Avoids:** Metadata scraping rate-limit bans (Pitfall 4), image loading stutter at scale (Pitfall 6), cover art stored as database BLOBs (architecture anti-pattern)

**Research flag:** Needs phase research — ScreenScraper API endpoint details, IGDB Twitch OAuth token refresh mechanics, and current rate limit values should be verified before implementation.

---

### Phase 4: Emulator Integration and Game Launch

**Rationale:** Having ROMs with cover art and descriptions but no way to launch them is an incomplete product. Emulator integration depends on having ROM records and platform definitions in place, making this the natural next phase. The Flatpak launch abstraction must be designed here, not added later.

**Delivers:** `EmulatorDefinition` schema in `emulators.json` with `LaunchType` discriminator (`NativeBinary`, `Flatpak`, `AppImage`); `EmulatorManager` with `IProcessLauncher` abstraction; RetroArch + core resolution; launch recording (updates `last_played`, `play_count`); "game is running" state with process PID tracking; versioned emulator profiles; user-editable CLI template escape hatch.

**Addresses:** Launch game, emulator configuration per platform, recently played (populated)

**Avoids:** Emulator CLI instability (Pitfall 2), Flatpak launch not abstracted (Pitfall 8), `Process.Start` without process tracking (technical debt pattern)

**Research flag:** Needs phase research — specific Flatpak `--filesystem` flags per emulator, RetroArch core resolution paths on SteamOS, PCSX2 v2 CLI changes should be verified before implementation.

---

### Phase 5: Desktop Mode UI Completion

**Rationale:** With scanning, metadata, and launch working, this phase completes the desktop experience. ViewModels are proven by this point, making it safe to build full UI features on top. Search and filtering require the metadata from Phase 3 to be meaningful.

**Delivers:** Full grid + list view with virtualized `ItemsRepeater` (500+ items at 60fps); search by name (SQLite FTS5 index); filter by genre/platform/year/developer; favorites toggle; custom collections (cross-platform); game detail view with full metadata display; settings screen (controller-navigable, tested on 1280×800).

**Addresses:** Search/filter, favorites, custom collections, manual metadata override UI, desktop mode completeness

**Avoids:** Non-virtualized grid performance collapse (all benchmarked at 500+ items)

**Research flag:** Standard patterns — Avalonia ItemsRepeater and SQLite FTS5 are well-documented; no phase research needed.

---

### Phase 6: Fullscreen and Gamepad Mode

**Rationale:** `FullscreenShell` stub from Phase 1 gets its full implementation here. Desktop mode ViewModels are proven and stable, so the fullscreen views can bind to the same state without risk. Controller navigation model designed in Phase 1 gets fully exercised across all views.

**Delivers:** Full fullscreen view tree (platform select → game grid → game detail → launch); D-pad navigation across all screens; `WindowState.FullScreen` verified under KDE Plasma, Gamescope, and X11; Steam Input integration documented; on-screen virtual keyboard for search; "back to library" always reachable without mouse/keyboard.

**Addresses:** Fullscreen mode, controller/gamepad navigation, Steam Deck first-class support

**Avoids:** Fullscreen with no controller exit path (UX pitfall), Gamescope compositor quirks

**Research flag:** Needs phase research — Avalonia Wayland/Gamescope support status and Steam Input API integration specifics should be verified before implementation.

---

### Phase 7: Recommendation Engine and Collections Intelligence

**Rationale:** Recommendations require both populated metadata (Phase 3) and accumulated play history (Phase 4+). This is the last core feature and the project's strongest differentiator. No other lightweight frontend has this.

**Delivers:** Similarity scoring based on genre/developer/publisher metadata; "What to play next" suggestions surfaced in both desktop and fullscreen modes; cross-platform smart collections (e.g., auto-"RPGs", auto-"Co-op"); recommendation quality improves with play history depth.

**Addresses:** Recommendation system (stated project requirement), custom collections (enhanced)

**Avoids:** Building this before metadata and play history are populated (would produce meaningless results)

**Research flag:** Needs phase research — similarity scoring algorithms for sparse metadata, cold-start strategy for new installs.

---

### Phase 8: One-Click Emulator Install and ROM Cache (v1.x)

**Rationale:** These are the P2 features — high value but not launch blockers. Emulator install reduces the setup friction that is the biggest barrier for new users. ROM cache enables Steam Deck NAS users. Both build on infrastructure from earlier phases (Flatpak abstraction from Phase 4, storage providers from Phase 2).

**Delivers:** One-click emulator download/install for Windows (binary) and Linux (Flatpak); recommended emulator defaults per platform; ROM cache with LRU eviction (`last_launched_at`-based); cache size display and manual cleanup with confirmation and undo; disk-full detection at launch time.

**Addresses:** One-click emulator install, recommended emulator defaults, ROM cache with eviction, cache cleanup settings

**Avoids:** Cache management silent failures (Pitfall 7), emulator install breaking on Steam Deck (Pitfall 8 recovery)

**Research flag:** Standard patterns for Phase 8 emulator install (Flatpak pattern established in Phase 4). Cache eviction needs phase research — test with mocked disk quota.

---

### Phase Ordering Rationale

- **Foundation before features:** The migration framework, CI, and controller navigation model are architectural decisions that cannot be safely made mid-project. Phase 1 contains them all.
- **Data before display:** Scan (Phase 2) before metadata (Phase 3) before UI completion (Phase 5) follows the feature dependency graph from FEATURES.md exactly.
- **Desktop before fullscreen:** Proven ViewModels from Phase 5 make Phase 6 (fullscreen) lower risk. Building fullscreen before ViewModels are stable leads to two surfaces breaking simultaneously.
- **Infrastructure before differentiators:** Launch (Phase 4) must work before recommendations (Phase 7) are meaningful. Play history has no value without launches recorded.
- **P2 features deferred:** Phases 1–7 constitute a complete v1. Phase 8 adds the high-value friction-reducers that are best validated after core usage patterns are understood.

### Research Flags Summary

**Needs deeper research during planning:**
- **Phase 3 (Metadata Scraping):** ScreenScraper API current rate limits and endpoint details; IGDB Twitch OAuth token refresh mechanics. Verify before implementation.
- **Phase 4 (Emulator Integration):** Flatpak `--filesystem` flags per emulator; RetroArch core resolution on SteamOS; PCSX2 v2 CLI changes. Verify before implementation.
- **Phase 6 (Fullscreen/Gamepad):** Avalonia Wayland/Gamescope support status as of current Avalonia 11.x; Steam Input API specifics. Verify before implementation.
- **Phase 7 (Recommendations):** Similarity algorithm selection for sparse metadata; cold-start strategy. Needs design research.

**Standard patterns (skip phase research):**
- **Phase 1 (Foundation):** .NET 10 + Avalonia project setup is well-documented with official templates.
- **Phase 2 (ROM Scanning):** `IAsyncEnumerable` + `Channel<T>` pipeline is well-established .NET pattern.
- **Phase 5 (Desktop UI):** Avalonia `ItemsRepeater` virtualization and SQLite FTS5 are documented patterns.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | .NET 10 confirmed via official docs. Avalonia 11.x version, ReactiveUI 20.x version, and FluentAvalonia need verification at project start. Core choices (Avalonia over WPF/MAUI, EF Core + SQLite, ScreenScraper as primary) are HIGH confidence. |
| Features | MEDIUM | Table-stakes features are stable (HIGH confidence). Specific competitor feature states (e.g., "Playnite doesn't have recommendations") should be re-verified before use as marketing claims. Feature dependency graph is HIGH confidence — it follows functional logic, not competitor observation. |
| Architecture | MEDIUM | Structural patterns (hexagonal layers, dual view shells, provider pattern, background pipeline) are well-established and consistent across named open-source launchers. Specific API details (ScreenScraper endpoints, IGDB token flow) flagged for phase-level research. |
| Pitfalls | MEDIUM | Derived from training knowledge of Playnite, ES-DE, LaunchBox, RetroArch. No live verification possible. Flatpak sandbox behavior is HIGH confidence (official docs). ScreenScraper rate limits and Avalonia Wayland/Gamescope behavior are MEDIUM and should be validated before those phases. |

**Overall confidence:** MEDIUM

Research is sufficient to design a sound roadmap. The gaps are operational details (exact API behaviors, current library versions) that need spot-verification at the start of each affected phase — not fundamental uncertainties about approach.

### Gaps to Address

- **Avalonia exact version:** Verify current stable Avalonia 11.x minor version at project start before bootstrapping. Use `dotnet new install Avalonia.Templates` and check the installed version.
- **ScreenScraper rate limits:** Confirm current `maxrequestsperday` values for free and developer accounts before Phase 3 begins. Values may have changed since training cutoff.
- **IGDB OAuth token mechanics:** Confirm current Twitch Client Credentials flow and token expiry behavior before Phase 3 begins. Twitch API specifics have changed historically.
- **Avalonia Wayland/Gamescope support:** Check current Avalonia GitHub issues for known Gamescope compositor bugs before Phase 6 begins.
- **RetroArch core paths on SteamOS:** Verify standard Flatpak core locations on SteamOS 3.x before Phase 4 emulator integration.
- **ReactiveUI vs CommunityToolkit.Mvvm decision:** If the team has no Reactive Extensions background, evaluate at project start. CommunityToolkit is simpler but lacks `IScreen`/routing — which matters for fullscreen navigation. Decide before Phase 1 scaffolding.

---

## Sources

### Primary (HIGH confidence)
- `https://dotnet.microsoft.com/en-us/download/dotnet/10.0` — Confirmed .NET 10.0.5 LTS current as of March 12 2026
- `https://dotnet.microsoft.com/en-us/download` — .NET 9 STS and .NET 10 LTS version confirmation
- Flatpak official documentation — Flatpak filesystem sandbox model and `--filesystem=host` flag behavior
- SQLite official documentation — `ALTER TABLE` limitations; FTS5 index design

### Secondary (MEDIUM confidence)
- Avalonia UI documentation and GitHub (training data, August 2025) — API patterns, Wayland/Gamescope behavior, Headless testing
- ReactiveUI documentation (training data, August 2025) — `IScreen`/routing, Avalonia adapter patterns
- ScreenScraper API documentation (training data, August 2025) — hash-based lookup, rate limit fields in response
- EF Core documentation (training data, August 2025) — migration framework, SQLite provider
- Polly documentation (training data, August 2025) — v8 resilience API
- Playnite source (GitHub JosefNemec/Playnite, training data) — feature set and architecture patterns
- EmulationStation-DE (es-de.org, GitLab, training data) — cross-platform frontend architecture patterns
- LaunchBox/BigBox (launchbox-app.com, training data) — feature set reference
- Pegasus Frontend (pegasus-frontend.org, training data) — cross-platform frontend patterns

### Tertiary (LOW confidence)
- Steam Deck / Gamescope compositor behavior — Valve developer resources and community reports (training data); needs live verification
- IGDB Twitch OAuth specifics — training data; token expiry mechanics should be re-verified

---
*Research completed: 2026-03-18*
*Ready for roadmap: yes*
