---
phase: 02-rom-scanning-and-library
plan: 02
subsystem: scanning
tags: [ef-core, sqlite, system-io-hashing, crc32, md5, sha1, backgroundservice, ihostedservice, m3u, incremental-scan]

requires:
  - phase: 02-rom-scanning-and-library
    provides: Game + RomFile EF Core entities, migration, platforms.json, IRomSourceProvider implementations

provides:
  - PlatformRegistry: folder-name to PlatformDefinition resolution via name and alias lookup
  - FilenameParser: regex-based ROM title cleaning and region extraction
  - M3uParser: disc path resolution from .m3u playlists
  - RomScannerService: incremental scan (add new, mark missing, restore reappeared) with m3u multi-disc support
  - FileHasher: CRC32+MD5+SHA1 in single streaming pass
  - RomScanHostedService: BackgroundService wrapper wiring scanner into app startup
  - LibraryViewModel: IsScanning + ScanProgressText reactive properties
  - 20 passing Phase2 tests covering scanner, platform detection, incremental scan, and hashing

affects: [03-metadata-scraping, 04-emulator-launch, 05-library-ui]

tech-stack:
  added: [System.IO.Hashing.Crc32, Parallel.ForEachAsync, IHostedService/BackgroundService, Progress<T>]
  patterns:
    - IHostedService wraps scoped service via IServiceScopeFactory to avoid DI lifetime conflicts
    - ExecuteUpdateAsync used for bulk status updates (bypasses change tracker)
    - ChangeTracker.Clear() called after ExecuteUpdateAsync to ensure subsequent reads see fresh DB state
    - Pre-collect m3u disc paths before main scan loop to avoid UNIQUE constraint conflicts on disc files

key-files:
  created:
    - src/YARL/Infrastructure/Scanning/PlatformDefinition.cs
    - src/YARL/Infrastructure/Scanning/PlatformRegistry.cs
    - src/YARL/Infrastructure/Scanning/FilenameParser.cs
    - src/YARL/Infrastructure/Scanning/M3uParser.cs
    - src/YARL/Infrastructure/Scanning/ScanUpdate.cs
    - src/YARL/Infrastructure/Scanning/ScanReport.cs
    - src/YARL/Infrastructure/Scanning/RomScannerService.cs
    - src/YARL/Infrastructure/Scanning/RomScanHostedService.cs
    - src/YARL/Infrastructure/Scanning/FileHasher.cs
  modified:
    - src/YARL/Program.cs
    - src/YARL/UI/ViewModels/LibraryViewModel.cs
    - tests/YARL.Tests/Phase2/ScannerTests.cs
    - tests/YARL.Tests/Phase2/PlatformDetectionTests.cs
    - tests/YARL.Tests/Phase2/IncrementalScanTests.cs
    - tests/YARL.Tests/Phase2/HashingTests.cs

key-decisions:
  - "M3u disc files must be pre-collected before main scan loop: without this pass, disc files are also enumerated as standalone ROMs causing UNIQUE constraint violations on RomFile.FilePath"
  - "ChangeTracker.Clear() required after ExecuteUpdateAsync: EF Core change tracker retains in-memory state after bulk updates, causing stale Active status on subsequent FirstAsync() calls"
  - "CRC32 uses GetCurrentHashAsUInt32().ToString(X8) not Convert.ToHexString(GetCurrentHash()): the latter produces wrong endianness (System.IO.Hashing stores as little-endian bytes)"
  - "IsCancellationRequested check at ScanAllAsync entry point: pre-cancelled tokens throw OperationCanceledException in ToListAsync before reaching the loop guard"
  - "FileHasher created in Task 2-02-01 rather than 2-02-02: RomScannerService.HashNewRomFilesAsync references FileHasher; creating it in the same commit was required for compilation"

patterns-established:
  - "Pattern: IServiceScopeFactory.CreateScope() in BackgroundService.ExecuteAsync() to get scoped EF Core DbContext from singleton-lifetime hosted service"
  - "Pattern: await Task.Yield() at start of ExecuteAsync to return control to startup pipeline before scanning begins"
  - "Pattern: Pre-cancelled CT guard at service method entry point before any async DB calls"
  - "Pattern: Bulk status updates via ExecuteUpdateAsync + ChangeTracker.Clear() for incremental scan state transitions"

requirements-completed: [LIB-01, LIB-02]

duration: 8min
completed: 2026-03-19
---

# Phase 2 Plan 02: ROM Scanner Engine Summary

**Background scanner with PlatformRegistry folder matching, incremental add/mark-missing/restore logic, m3u multi-disc support, and CRC32+MD5+SHA1 hashing via a single streaming pass**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-03-18T23:50:02Z
- **Completed:** 2026-03-18T23:57:38Z
- **Tasks:** 2
- **Files modified:** 15

## Accomplishments
- PlatformRegistry loads platforms.json and resolves folder names to PlatformDefinition by name or alias (case-insensitive)
- RomScannerService: incremental scan against EF Core SQLite — adds new games, marks removed as Missing, restores reappeared to Active
- M3u multi-disc: pre-collect disc paths before main loop so disc files aren't also imported as standalone games
- FileHasher: single-pass CRC32+MD5+SHA1 streaming computation using Parallel.ForEachAsync with MaxDegreeOfParallelism=2
- RomScanHostedService: await Task.Yield() on entry, IServiceScopeFactory for scoped DbContext lifetime, Progress<T> captures SynchronizationContext for UI thread safety
- 20 Phase2 tests passing (5 LibraryViewModel stubs remain skipped — Wave 0, filled in a later plan)

## Task Commits

Each task was committed atomically:

1. **Task 2-02-01: Scanner Engine + Tests** - `acb1a89` (feat)
2. **Task 2-02-02: FileHasher + Hashing Tests** - `1463694` (feat)

## Files Created/Modified

- `src/YARL/Infrastructure/Scanning/PlatformDefinition.cs` - DTO for platforms.json deserialization
- `src/YARL/Infrastructure/Scanning/PlatformRegistry.cs` - Folder-name to platform resolution with LoadFromJson factory
- `src/YARL/Infrastructure/Scanning/FilenameParser.cs` - CleanTitle and ExtractRegion via compiled regex
- `src/YARL/Infrastructure/Scanning/M3uParser.cs` - ParseDiscPaths resolves relative disc paths
- `src/YARL/Infrastructure/Scanning/ScanUpdate.cs` - Progress report record
- `src/YARL/Infrastructure/Scanning/ScanReport.cs` - Scan summary record
- `src/YARL/Infrastructure/Scanning/RomScannerService.cs` - Core scan loop with incremental logic and hash pipeline
- `src/YARL/Infrastructure/Scanning/RomScanHostedService.cs` - BackgroundService wrapper
- `src/YARL/Infrastructure/Scanning/FileHasher.cs` - CRC32+MD5+SHA1 hashing utility
- `src/YARL/Program.cs` - Added PlatformRegistry, RomScannerService, RomScanHostedService registrations
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Added IsScanning + ScanProgressText properties
- `tests/YARL.Tests/Phase2/ScannerTests.cs` - 5 scanner tests implemented
- `tests/YARL.Tests/Phase2/PlatformDetectionTests.cs` - 6 platform detection tests implemented
- `tests/YARL.Tests/Phase2/IncrementalScanTests.cs` - 5 incremental scan tests implemented
- `tests/YARL.Tests/Phase2/HashingTests.cs` - 4 hashing tests with known reference values

## Decisions Made

- M3u disc files pre-collected before scan loop to prevent UNIQUE constraint violations
- ChangeTracker.Clear() after ExecuteUpdateAsync to ensure fresh DB state on next read
- CRC32 formatted via GetCurrentHashAsUInt32().ToString("X8") per RESEARCH.md Pitfall 6 guidance
- FileHasher created in Task 2-02-01 because RomScannerService depends on it for compilation

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] FileHasher created in Task 2-02-01 instead of 2-02-02**
- **Found during:** Task 2-02-01 (RomScannerService implementation)
- **Issue:** RomScannerService.HashNewRomFilesAsync calls FileHasher.ComputeHashesAsync — compiling Task 1 code required FileHasher to exist
- **Fix:** Created FileHasher.cs (the full Task 2-02-02 implementation) during Task 2-02-01 and included it in the same commit
- **Files modified:** src/YARL/Infrastructure/Scanning/FileHasher.cs
- **Verification:** Build succeeded with no errors
- **Committed in:** acb1a89 (Task 2-02-01 commit)

**2. [Rule 1 - Bug] Pre-cancelled CancellationToken caused OperationCanceledException**
- **Found during:** Task 2-02-01 (ScannerTests.Scanner_respects_cancellation_token)
- **Issue:** Pre-cancelled token throws OperationCanceledException in ToListAsync before the ct.IsCancellationRequested guard in the foreach loop
- **Fix:** Added early return at start of ScanAllAsync when ct.IsCancellationRequested is true
- **Files modified:** src/YARL/Infrastructure/Scanning/RomScannerService.cs
- **Verification:** Scanner_respects_cancellation_token passes
- **Committed in:** acb1a89 (Task 2-02-01 commit)

**3. [Rule 1 - Bug] M3u disc files triggered UNIQUE constraint violation**
- **Found during:** Task 2-02-01 (ScannerTests.Scanner_handles_m3u_multi_disc_games)
- **Issue:** Disc files (Disc1.smc, Disc2.smc) enumerated by provider as standalone .smc ROMs AND added as child RomFiles under the .m3u game, causing duplicate FilePath inserts
- **Fix:** Added pre-collection pass over .m3u files to build m3uDiscPaths set; disc paths in this set are skipped during the main file loop
- **Files modified:** src/YARL/Infrastructure/Scanning/RomScannerService.cs
- **Verification:** Scanner_handles_m3u_multi_disc_games passes: 1 game, 3 RomFiles (1 m3u + 2 discs)
- **Committed in:** acb1a89 (Task 2-02-01 commit)

**4. [Rule 1 - Bug] ExecuteUpdateAsync left stale Active status in change tracker**
- **Found during:** Task 2-02-01 (IncrementalScanTests.Reappeared_files_are_restored_to_Active)
- **Issue:** ExecuteUpdateAsync bypasses EF change tracker; subsequent db.Games.FirstAsync() returned tracked in-memory entity still showing Active (not Missing)
- **Fix:** Switched to two-step query (get game IDs via RomFiles, then ExecuteUpdateAsync on games), added ChangeTracker.Clear() after SaveChangesAsync
- **Files modified:** src/YARL/Infrastructure/Scanning/RomScannerService.cs
- **Verification:** All 5 incremental scan tests pass including Reappeared_files_are_restored_to_Active
- **Committed in:** acb1a89 (Task 2-02-01 commit)

---

**Total deviations:** 4 auto-fixed (1 blocking compile error, 3 bugs)
**Impact on plan:** All fixes necessary for correctness. No scope creep — FileHasher content matches plan spec exactly.

## Issues Encountered

- MSBuild stale cache files (YARL.csproj.CoreCompileInputs.cache) required `dotnet clean` before incremental builds would succeed — this is a Windows MSBuild issue with concurrent file access, not a code issue.

## Next Phase Readiness

- Scanner engine complete and tested; ready for Phase 3 metadata scraping (which needs scanned Game records)
- LibraryViewModel IsScanning/ScanProgressText properties ready for Phase 5 UI binding
- No blockers for Phase 3

---
*Phase: 02-rom-scanning-and-library*
*Completed: 2026-03-19*

## Self-Check: PASSED

- FOUND: src/YARL/Infrastructure/Scanning/PlatformRegistry.cs
- FOUND: src/YARL/Infrastructure/Scanning/RomScannerService.cs
- FOUND: src/YARL/Infrastructure/Scanning/FileHasher.cs
- FOUND: src/YARL/Infrastructure/Scanning/RomScanHostedService.cs
- FOUND: .planning/phases/02-rom-scanning-and-library/02-02-SUMMARY.md
- FOUND commit: acb1a89 (Task 2-02-01)
- FOUND commit: 1463694 (Task 2-02-02)
