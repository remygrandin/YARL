---
phase: 02-rom-scanning-and-library
plan: 01
subsystem: database
tags: [efcore, sqlite, domain-models, game-library, platform-detection, system-io-hashing, test-stubs]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: YarlDbContext with RomSource/RomSources, EF Core migrations infrastructure, platforms.json with 28 platforms
provides:
  - Game entity with full Phase 2 fields (title, platformId, sourceId, favorites, lastPlayedAt, status, region, romFiles)
  - RomFile entity with hashing fields (CRC32, MD5, SHA1), multi-disc support (discNumber, isM3uPlaylist)
  - GameStatus enum (Active, Missing, Hidden)
  - AddGameAndRomFile EF Core migration
  - platforms.json enriched with aliases arrays for all 28 platforms
  - System.IO.Hashing 10.0.4 package in both main and test projects
  - Wave 0 test stubs: 25 skipped xUnit tests across 5 Phase2 test files
affects: [02-02-scanner, 02-03-platform-detection, 02-04-library-viewmodel, 02-05-incremental-scan]

# Tech tracking
tech-stack:
  added:
    - System.IO.Hashing 10.0.4 (CRC32, MD5, SHA1 hashing for ROM identification)
  patterns:
    - GameStatus stored as string in SQLite via HasConversion<string>() — consistent with SourceType pattern
    - Composite index on (PlatformId, Status) for efficient library queries by platform
    - Unique index on RomFile.FilePath to enforce deduplication at DB level
    - Wave 0 stub pattern: [Fact(Skip = "Wave 0 stub")] with [Trait("Category", "Phase2")] for all stub tests

key-files:
  created:
    - src/YARL/Domain/Enums/GameStatus.cs
    - src/YARL/Domain/Models/Game.cs
    - src/YARL/Domain/Models/RomFile.cs
    - src/YARL/Infrastructure/Persistence/Migrations/20260318234511_AddGameAndRomFile.cs
    - src/YARL/Infrastructure/Persistence/Migrations/20260318234511_AddGameAndRomFile.Designer.cs
    - tests/YARL.Tests/Phase2/ScannerTests.cs
    - tests/YARL.Tests/Phase2/HashingTests.cs
    - tests/YARL.Tests/Phase2/PlatformDetectionTests.cs
    - tests/YARL.Tests/Phase2/LibraryViewModelTests.cs
    - tests/YARL.Tests/Phase2/IncrementalScanTests.cs
  modified:
    - src/YARL/Infrastructure/Persistence/YarlDbContext.cs
    - src/YARL/Infrastructure/Persistence/Migrations/YarlDbContextModelSnapshot.cs
    - src/YARL/Data/platforms.json
    - src/YARL/YARL.csproj
    - tests/YARL.Tests/YARL.Tests.csproj

key-decisions:
  - "GameStatus stored as string via HasConversion<string>() — matches SourceType convention for human-readable SQLite rows"
  - "Composite index (PlatformId, Status) enables efficient library filtering by platform without full table scan"
  - "Unique index on RomFile.FilePath enforces deduplication at DB layer — scanner upsert logic relies on this constraint"
  - "aliases array added to all 28 platforms in platforms.json using EmulationStation/RetroPie folder name conventions"
  - "Wave 0 stubs use [Fact(Skip = 'Wave 0 stub')] + [Trait('Category', 'Phase2')] to enable category-filtered test runs"

patterns-established:
  - "Wave 0 stub pattern: all Phase2 test files created as skipped stubs before any implementation begins"
  - "Platform aliases follow EmulationStation/RetroPie conventions for broad folder name compatibility"

requirements-completed: [LIB-01]

# Metrics
duration: 2min
completed: 2026-03-18
---

# Phase 2 Plan 01: Data Foundation + Wave 0 Test Stubs Summary

**Game and RomFile EF Core entities with migration, platforms.json alias data for all 28 platforms, System.IO.Hashing 10.0.4, and 25 Wave 0 test stubs across 5 Phase2 test files**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-18T23:44:09Z
- **Completed:** 2026-03-18T23:46:38Z
- **Tasks:** 2
- **Files modified:** 15

## Accomplishments

- Game entity with full Phase 2 fields and RomFile entity with hashing + multi-disc support persisted via EF Core migration
- platforms.json enriched with aliases arrays for all 28 platforms using EmulationStation/RetroPie conventions
- 25 Wave 0 test stubs created across 5 Phase2 test files, all compile and skip cleanly

## Task Commits

Each task was committed atomically:

1. **Task 2-01-01: Game + RomFile Entities, EF Core Migration, platforms.json Aliases** - `d548cea` (feat)
2. **Task 2-01-02: Wave 0 Test Stubs (All 5 Files)** - `4d42a55` (feat)

## Files Created/Modified

- `src/YARL/Domain/Enums/GameStatus.cs` - GameStatus enum (Active, Missing, Hidden)
- `src/YARL/Domain/Models/Game.cs` - Game entity with all Phase 2 fields
- `src/YARL/Domain/Models/RomFile.cs` - RomFile entity with hashing fields and disc support
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` - Added DbSet<Game>, DbSet<RomFile>, OnModelCreating config
- `src/YARL/Infrastructure/Persistence/Migrations/20260318234511_AddGameAndRomFile.cs` - EF Core migration
- `src/YARL/Infrastructure/Persistence/Migrations/YarlDbContextModelSnapshot.cs` - Updated snapshot
- `src/YARL/Data/platforms.json` - Added aliases array to all 28 platform entries
- `src/YARL/YARL.csproj` - Added System.IO.Hashing 10.0.4
- `tests/YARL.Tests/Phase2/ScannerTests.cs` - 5 scanner test stubs
- `tests/YARL.Tests/Phase2/HashingTests.cs` - 4 hashing test stubs
- `tests/YARL.Tests/Phase2/PlatformDetectionTests.cs` - 6 platform detection test stubs
- `tests/YARL.Tests/Phase2/LibraryViewModelTests.cs` - 5 library ViewModel test stubs
- `tests/YARL.Tests/Phase2/IncrementalScanTests.cs` - 5 incremental scan test stubs
- `tests/YARL.Tests/YARL.Tests.csproj` - Added System.IO.Hashing 10.0.4

## Decisions Made

- GameStatus stored as string via HasConversion<string>() to match the SourceType convention — human-readable SQLite rows
- Composite index on (PlatformId, Status) for efficient library filtering without full table scan
- Unique index on RomFile.FilePath enforces deduplication at the DB layer; the scanner upsert logic (Plan 02-03) will rely on this constraint
- Platform aliases follow EmulationStation/RetroPie folder naming conventions for maximum compatibility with existing ROM library structures

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

`dotnet test` with `--filter "Category=Phase2"` returned exit code 1 because VSTest exits non-zero when all selected tests are skipped (no tests executed). Verified stubs are correctly skipped by running the pre-built DLL directly — all 25 tests show as "Ignored/SKIP". This is expected behavior for Wave 0 stubs, not a failure.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Data contracts complete: Game, RomFile, GameStatus entities are available for scanner implementation
- platforms.json aliases ready for platform detection logic (Plan 02-02)
- Wave 0 stubs ready to be filled in as each plan implements its functionality
- System.IO.Hashing available in both main and test projects for CRC32/MD5/SHA1 hashing

---
*Phase: 02-rom-scanning-and-library*
*Completed: 2026-03-18*
