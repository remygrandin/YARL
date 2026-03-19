---
phase: 03-metadata-and-cover-art
plan: 01
subsystem: database
tags: [ef-core, sqlite, domain-models, screenscraper, platforms-json, wave0-stubs]

# Dependency graph
requires:
  - phase: 02-rom-scanning-and-library
    provides: Game entity, RomFile entity, YarlDbContext, platforms.json structure
provides:
  - ScrapeStatus enum (Pending, Matched, Unmatched, Override)
  - Game entity extended with 10 metadata fields and GameVersion navigation property
  - GameVersion entity for regional ROM variant grouping (FK to Game and optional RomFile)
  - YarlDbContext configured with GameVersions DbSet and new entity config blocks
  - EF Core migration AddMetadataAndGameVersion
  - platforms.json enriched with screenscraperSystemId and themeColor for all 28 platforms
  - 22 Wave 0 test stubs in tests/YARL.Tests/Phase3/ covering META-01..04 and LIB-04..05
affects:
  - 03-02 (ScreenScraper client uses ScrapeStatus and Game metadata fields)
  - 03-03 (IGDB fallback pipeline uses same domain models)
  - 03-04 (metadata override service uses IsMetadataOverridden and ScrapeStatus)
  - 03-05 (library search/filter uses Genre, Developer, ReleaseYear on Game)
  - 03-06 (cover art display uses CoverArtPath)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ScrapeStatus stored as string via HasConversion<string>() matching GameStatus/SourceType convention
    - GameVersion composite index on (GameId, Region) for efficient regional variant lookups
    - Wave 0 test stubs use [Fact(Skip = "Wave 0 stub")] + [Trait("Category", "Phase3")] for filtered test runs

key-files:
  created:
    - src/YARL/Domain/Enums/ScrapeStatus.cs
    - src/YARL/Domain/Models/GameVersion.cs
    - src/YARL/Infrastructure/Persistence/Migrations/20260319201431_AddMetadataAndGameVersion.cs
    - tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs
    - tests/YARL.Tests/Phase3/ScraperPipelineTests.cs
    - tests/YARL.Tests/Phase3/MetadataOverrideTests.cs
    - tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs
  modified:
    - src/YARL/Domain/Models/Game.cs
    - src/YARL/Infrastructure/Persistence/YarlDbContext.cs
    - src/YARL/Data/platforms.json

key-decisions:
  - "ScrapeStatus stored as string via HasConversion<string>() for human-readable SQLite rows, matching GameStatus/SourceType convention"
  - "GameVersion composite index on (GameId, Region) enables efficient regional variant lookups per game"
  - "MAME and NGPC have null screenscraperSystemId — these platforms have no standard ScreenScraper mapping"

patterns-established:
  - "Wave 0 stubs: [Fact(Skip = \"Wave 0 stub\")] + [Trait(\"Category\", \"Phase3\")] for phased TDD"
  - "New EF Core entities follow: HasKey, IsRequired+HasMaxLength on strings, HasConversion<string> on enums, explicit FK navigation config"

requirements-completed: [META-01, META-02, META-03, META-04, LIB-04, LIB-05]

# Metrics
duration: 4min
completed: 2026-03-19
---

# Phase 3 Plan 01: Data Foundation for Metadata and Cover Art Summary

**Game entity extended with 10 metadata fields and ScrapeStatus, GameVersion entity for regional ROM grouping, EF Core migration, platforms.json enriched with ScreenScraper system IDs and theme colors, and 22 Wave 0 test stubs for all Phase 3 requirements**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-03-19T20:13:02Z
- **Completed:** 2026-03-19T20:16:43Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Extended Game entity with Description, Genre, ReleaseYear, Developer, Publisher, CoverArtPath, ScraperMatchId, ScraperSource, IsMetadataOverridden, and ScrapeStatus fields
- Created GameVersion entity linking regional ROM variants to a canonical Game, with optional RomFile FK and composite index on (GameId, Region)
- Generated EF Core migration `AddMetadataAndGameVersion` with all new columns, the GameVersions table, and indexes
- Enriched all 28 platforms in platforms.json with screenscraperSystemId and themeColor (MAME and NGPC get null IDs as no standard ScreenScraper mapping exists)
- Created 22 Wave 0 test stubs across 4 files in tests/YARL.Tests/Phase3/ — all skipped, full suite green (36 passing + 22 skipped)

## Task Commits

Each task was committed atomically:

1. **Task 1: Domain models, enum, EF Core migration, platforms.json ScreenScraper IDs** - `df49ab7` (feat)
2. **Task 2: Wave 0 test stubs for all Phase 3 requirements** - `bb15a1f` (test)

**Plan metadata:** _(docs commit hash added after state update)_

## Files Created/Modified

- `src/YARL/Domain/Enums/ScrapeStatus.cs` - New enum: Pending, Matched, Unmatched, Override
- `src/YARL/Domain/Models/Game.cs` - Extended with 10 metadata fields and Versions navigation list
- `src/YARL/Domain/Models/GameVersion.cs` - New entity for regional ROM variant grouping
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` - GameVersions DbSet + entity configuration for both new entities
- `src/YARL/Infrastructure/Persistence/Migrations/20260319201431_AddMetadataAndGameVersion.cs` - EF Core migration
- `src/YARL/Data/platforms.json` - All 28 platforms enriched with screenscraperSystemId and themeColor
- `tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs` - 4 stubs: CRC32 match, no match, title search, cover art selection
- `tests/YARL.Tests/Phase3/ScraperPipelineTests.cs` - 6 stubs: pipeline flow, fallback, unmatched, metadata population, cover download
- `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs` - 4 stubs: skip override, set override, persist, revert
- `tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs` - 8 stubs: search, aliases, debounce, genre/year/developer filter, multi-filter, clear

## Decisions Made

- ScrapeStatus stored as string via `HasConversion<string>()`, consistent with GameStatus and SourceType conventions for human-readable SQLite rows
- GameVersion composite index on `(GameId, Region)` enables efficient regional variant lookups per game
- MAME and NGPC receive `null` screenscraperSystemId — no standard ScreenScraper system IDs exist for these platforms in the reference mapping

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Domain models ready: Game, GameVersion, ScrapeStatus all defined and migrated
- platforms.json system IDs ready for ScreenScraper API calls in plan 03-02
- Wave 0 stubs ready to be filled in as implementation plans land
- No blockers for 03-02 (ScreenScraper client implementation)

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-19*
