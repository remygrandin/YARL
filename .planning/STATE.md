---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
stopped_at: Completed 02-02-PLAN.md
last_updated: "2026-03-18T23:59:22.231Z"
progress:
  total_phases: 7
  completed_phases: 1
  total_plans: 7
  completed_plans: 5
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-18)

**Core value:** Users can browse their ROM collection visually and launch any game on any platform with one click, using the right emulator, on both desktop and Steam Deck.
**Current focus:** Phase 02 — rom-scanning-and-library

## Current Position

Phase: 02 (rom-scanning-and-library) — EXECUTING
Plan: 1 of 4

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

### Pending Todos

None yet.

### Blockers/Concerns

- [Pre-Phase 1]: Avalonia exact version must be verified at project start (dotnet new install Avalonia.Templates)
- [Pre-Phase 1]: ReactiveUI vs CommunityToolkit.Mvvm decision must be made before Phase 1 scaffolding — ReactiveUI IScreen/routing is needed for fullscreen navigation model
- [Pre-Phase 3]: ScreenScraper rate limits and IGDB OAuth token mechanics must be verified before Phase 3 begins
- [Pre-Phase 4]: RetroArch core paths on SteamOS and Flatpak --filesystem flags per emulator must be verified before Phase 4 begins
- [Pre-Phase 6]: Avalonia Wayland/Gamescope support status must be checked before Phase 6 begins

## Session Continuity

Last session: 2026-03-18T23:59:22.228Z
Stopped at: Completed 02-02-PLAN.md
Resume file: None
