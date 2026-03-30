---
phase: 04-emulator-integration-and-launch
plan: 02
subsystem: infra
tags: [emulator, launch, sqlite, ef-core, process, flatpak, playtime]

# Dependency graph
requires:
  - phase: 04-01
    provides: Wave 0 test stubs for EmulatorConfig and GameLaunchService
  - phase: 03-metadata-and-cover-art
    provides: YarlDbContext, Game entity, AppConfig/AppConfigService patterns

provides:
  - EmulatorConfig record (ExePath, Args, IsFlatpak) in AppConfig with JSON persistence
  - AppConfig.EmulatorConfigs dictionary keyed by platformId
  - Game.TotalPlayTime (TimeSpan stored as INTEGER via HasConversion<long>)
  - EF Core migration AddTotalPlayTime adding INTEGER column with default 0
  - GameLaunchService singleton with BuildStartInfo, LaunchGameAsync, UpdatePlayTimeAsync, BringToFront
  - LaunchOverlayState enum (Hidden/Launching/Running/Failed)

affects:
  - 04-03 (launch overlay UI uses GameLaunchService and LaunchOverlayState)
  - 04-04 (settings emulator config UI persists EmulatorConfigs to AppConfig)
  - 04-05 (play time display uses Game.TotalPlayTime)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ProcessStartInfo construction delegated to BuildStartInfo for testability
    - IServiceScopeFactory scoped DB access pattern (matching ScraperHostedService)
    - Dispatcher.UIThread.Post for fire-and-forget process exit -> UI state update
    - DllImport P/Invoke guarded by RuntimeInformation.IsOSPlatform check

key-files:
  created:
    - src/YARL/Infrastructure/Launch/GameLaunchService.cs
    - src/YARL/Infrastructure/Persistence/Migrations/20260330202325_AddTotalPlayTime.cs
    - tests/YARL.Tests/Phase4/EmulatorConfigTests.cs
    - tests/YARL.Tests/Phase4/GameLaunchServiceTests.cs
  modified:
    - src/YARL/Infrastructure/Config/AppConfig.cs
    - src/YARL/Domain/Models/Game.cs
    - src/YARL/Infrastructure/Persistence/YarlDbContext.cs
    - src/YARL/Infrastructure/Persistence/Migrations/YarlDbContextModelSnapshot.cs
    - src/YARL/Program.cs

key-decisions:
  - "HasConversion<long>().HasDefaultValue(TimeSpan.Zero) — HasDefaultValue must receive a TimeSpan (not 0L) to avoid EF Core design-time exception; migration correctly emits defaultValue: 0L"
  - "BuildStartInfo is public on GameLaunchService for direct unit testability without process spawning"
  - "UpdatePlayTimeAsync is public for direct DB update testing with in-memory SQLite"
  - "BringToFront P/Invoke guarded by RuntimeInformation.IsOSPlatform(OSPlatform.Windows) for Linux/SteamOS compatibility"
  - "Test 5 (OnProcessExited_NonZeroExitCodeWithinGrace) left as xUnit Skip stub — requires process mocking infrastructure not available in this plan"

patterns-established:
  - "ProcessStartInfo building: rompath substitution wraps path in double-quotes for argument safety on all platforms"
  - "Flatpak launch pattern: FileName=flatpak, Arguments=run {ExePath} {substituted-args}"

requirements-completed: [EMU-01, EMU-02]

# Metrics
duration: 4min
completed: 2026-03-30
---

# Phase 04 Plan 02: Emulator Config and Game Launch Service Summary

**EmulatorConfig record + AppConfig dictionary + TotalPlayTime EF migration + GameLaunchService singleton with native/Flatpak process launch and play time DB tracking**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-30T20:02:06Z
- **Completed:** 2026-03-30T20:06:15Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- EmulatorConfig record with ExePath/Args/IsFlatpak added to AppConfig with full JSON round-trip persistence
- Game.TotalPlayTime (TimeSpan) stored as INTEGER in SQLite via EF Core HasConversion<long>, migration generated
- GameLaunchService singleton created with BuildStartInfo (native + Flatpak), LaunchGameAsync, process exit monitoring, UpdatePlayTimeAsync, BringToFront P/Invoke
- Registered in DI as singleton in Program.cs
- 7 tests passing (3 EmulatorConfig, 4 GameLaunchService)

## Task Commits

Each task was committed atomically:

1. **Task 1: EmulatorConfig record, TotalPlayTime, EF migration** - `a13041f` (feat)
2. **Task 2: GameLaunchService with BuildStartInfo, exit monitoring, DB update** - `3155793` (feat)

## Files Created/Modified
- `src/YARL/Infrastructure/Config/AppConfig.cs` - Added EmulatorConfig record and EmulatorConfigs dictionary
- `src/YARL/Domain/Models/Game.cs` - Added TotalPlayTime (TimeSpan)
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` - HasConversion<long> for TotalPlayTime
- `src/YARL/Infrastructure/Persistence/Migrations/20260330202325_AddTotalPlayTime.cs` - New migration (INTEGER column, default 0)
- `src/YARL/Infrastructure/Persistence/Migrations/YarlDbContextModelSnapshot.cs` - Updated snapshot
- `src/YARL/Infrastructure/Launch/GameLaunchService.cs` - New: process launch + exit monitoring + play time tracking
- `src/YARL/Program.cs` - Added using + singleton registration for GameLaunchService
- `tests/YARL.Tests/Phase4/EmulatorConfigTests.cs` - 3 tests: round-trip, default args, empty dict
- `tests/YARL.Tests/Phase4/GameLaunchServiceTests.cs` - 4 tests: native PSI, flatpak PSI, spaces quoting, DB update

## Decisions Made
- `HasDefaultValue(TimeSpan.Zero)` required instead of `HasDefaultValue(0L)` — EF Core design-time validator rejects non-matching types; migration correctly emits `defaultValue: 0L`
- `BuildStartInfo` and `UpdatePlayTimeAsync` made `public` for direct unit testability
- BringToFront P/Invoke guarded by `RuntimeInformation.IsOSPlatform` for cross-platform safety
- Test 5 (exit code grace period) kept as `[Fact(Skip=...)]` stub — process mocking requires infrastructure beyond this plan scope

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] HasDefaultValue(0L) rejected by EF Core design-time**
- **Found during:** Task 1 (EF Core migration generation)
- **Issue:** `HasConversion<long>().HasDefaultValue(0L)` causes design-time exception: "Default value '0' of type 'long' cannot be set on property 'TotalPlayTime' of type 'System.TimeSpan'"
- **Fix:** Changed to `HasDefaultValue(TimeSpan.Zero)` — EF Core converts correctly in migration output
- **Files modified:** src/YARL/Infrastructure/Persistence/YarlDbContext.cs
- **Verification:** Migration generated successfully with `defaultValue: 0L`
- **Committed in:** a13041f (Task 1 commit)

**2. [Rule 1 - Bug] Test assertion for spaces used filename suffix instead of full path**
- **Found during:** Task 2 (GameLaunchServiceTests - BuildStartInfo_RomPathWithSpaces)
- **Issue:** Test asserted `"Super Mario World.sfc"` but BuildStartInfo wraps the full path in quotes
- **Fix:** Updated assertion to `"\"/home/user/my roms/Super Mario World.sfc\""` — the correct full quoted path
- **Files modified:** tests/YARL.Tests/Phase4/GameLaunchServiceTests.cs
- **Verification:** 4/4 GameLaunchService tests pass
- **Committed in:** 3155793 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bugs)
**Impact on plan:** Both auto-fixes required for correctness. No scope creep.

## Issues Encountered
- App process was running during Task 1 test run attempt — worked around by completing implementation first then running tests

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- GameLaunchService singleton registered and ready for 04-03 (launch overlay UI)
- EmulatorConfigs dictionary ready for 04-04 (settings emulator config UI)
- Game.TotalPlayTime ready for 04-05 (play time display)
- Migration AddTotalPlayTime is Pending — will apply automatically on next app startup via MigrateAsync()

---
*Phase: 04-emulator-integration-and-launch*
*Completed: 2026-03-30*
