---
phase: 02-rom-scanning-and-library
plan: 03
subsystem: ui
tags: [dynamicdata, reactiveui, sourcecache, viewmodel, csharp, avalonia]

# Dependency graph
requires:
  - phase: 02-rom-scanning-and-library
    provides: RomScannerService, RomScanHostedService, PlatformRegistry, ScanUpdate, Game domain model
  - phase: 01-foundation
    provides: YarlDbContext, GameStatus enum, ReactiveObject base

provides:
  - GameViewModel (wraps Game entity with reactive IsFavorite and LastPlayedAt)
  - PlatformViewModel (reactive GameCount updated by DynamicData group)
  - LibraryViewModel with SourceCache<GameViewModel, int> as live data hub
  - AllGames, Platforms, RecentlyPlayed, Favorites derived collections bound to ROOC
  - RescanCommand, CancelScanCommand, ToggleFavoriteCommand wired with canExecute guards
  - ReactiveUITestInit module initializer for unit test bootstrapping

affects: [03-metadata-and-scraping, 04-launch-and-navigation, plan-04-ui]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - SourceCache<GameViewModel, int> as single source of truth for game library
    - DynamicData Group() pipeline for platform grouping (vs GroupOn which is list-based)
    - IScheduler injected as constructor param for testability (RxApp removed in ReactiveUI v23)
    - ModuleInitializer + RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp() for unit test ReactiveUI bootstrap

key-files:
  created:
    - src/YARL/UI/ViewModels/GameViewModel.cs
    - src/YARL/UI/ViewModels/PlatformViewModel.cs
    - tests/YARL.Tests/ReactiveUITestInit.cs
  modified:
    - src/YARL/UI/ViewModels/LibraryViewModel.cs
    - src/YARL/Program.cs
    - src/YARL/Infrastructure/Scanning/RomScanHostedService.cs
    - tests/YARL.Tests/Phase2/LibraryViewModelTests.cs

key-decisions:
  - "ReactiveUI v23 removed RxApp static class — IScheduler injected as constructor param instead of RxApp.MainThreadScheduler"
  - "DynamicData Group() used for cache-based grouping (vs GroupOn which operates on list changesets)"
  - "RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp() called via ModuleInitializer for unit test setup"
  - "DisposeWith extension removed in ReactiveUI v23 — replaced with CompositeDisposable.Add() directly"

patterns-established:
  - "SourceCache pattern: SourceCache<TObject, TKey> as backing store, Connect() for all derived pipelines"
  - "Scheduler injection: pass IScheduler as nullable ctor param, default to CurrentThreadScheduler.Instance"

requirements-completed: [LIB-03, LIB-06, LIB-07]

# Metrics
duration: 8min
completed: 2026-03-19
---

# Phase 2 Plan 03: Library ViewModel + DynamicData Live Collections Summary

**LibraryViewModel rewritten as a DynamicData SourceCache hub with reactive AllGames, Platforms, RecentlyPlayed, and Favorites derived collections, plus RescanCommand wired to scanner**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-19T00:01:06Z
- **Completed:** 2026-03-19T00:09:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- GameViewModel and PlatformViewModel created as ReactiveObject wrappers with source-generator [Reactive] properties
- LibraryViewModel rewritten with SourceCache<GameViewModel, int> and four derived ReadOnlyObservableCollection bindings (AllGames, Platforms, RecentlyPlayed, Favorites)
- RescanCommand/CancelScanCommand/ToggleFavoriteCommand wired with canExecute guards
- RomScanHostedService updated to call LoadGamesFromDbAsync after scan completes
- 5 LibraryViewModel unit tests all green (favorites toggle, top-15 recently played, null exclusion, platform filtering, scan state)
- All 36 tests pass (Phase 1 + Phase 2)

## Task Commits

1. **Task 2-03-01: GameViewModel, PlatformViewModel, LibraryViewModel SourceCache** - `943e3d7` (feat)
2. **Task 2-03-02: LibraryViewModel Unit Tests** - `2592b82` (feat)

## Files Created/Modified
- `src/YARL/UI/ViewModels/GameViewModel.cs` - Wraps Game entity; IsFavorite and LastPlayedAt are reactive
- `src/YARL/UI/ViewModels/PlatformViewModel.cs` - Wraps platform with reactive GameCount
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Full rewrite with SourceCache and four derived collections
- `src/YARL/Program.cs` - DI registration updated to inject PlatformRegistry and IServiceScopeFactory
- `src/YARL/Infrastructure/Scanning/RomScanHostedService.cs` - Calls LoadGamesFromDbAsync after scan
- `tests/YARL.Tests/Phase2/LibraryViewModelTests.cs` - 5 tests implemented (was stubs)
- `tests/YARL.Tests/ReactiveUITestInit.cs` - ModuleInitializer for ReactiveUI bootstrap in tests

## Decisions Made
- **ReactiveUI v23 removed RxApp:** `RxApp.MainThreadScheduler` no longer exists. IScheduler injected as constructor param; defaults to `CurrentThreadScheduler.Instance` for tests.
- **DynamicData Group() not GroupOn():** SourceCache.Connect() returns a cache changeset (`IChangeSet<TObject, TKey>`); `GroupOn` is for list changesets. Correct method is `Group<TObject, TKey, TGroupKey>()`.
- **DisposeWith not available:** In ReactiveUI v23, the `DisposeWith` extension method was removed. Replaced with direct `CompositeDisposable.Add()` calls.
- **ReactiveUI test initialization:** `RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp()` called via `[ModuleInitializer]` satisfies `EnsureInitialized()` for WhenAnyValue in plain .NET tests. This supersedes the Phase 1 decision that "ReactiveObject works without init" — it still does for PropertyChanged, but WhenAnyValue/WhenAny requires initialization in ReactiveUI v23.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] RxApp.MainThreadScheduler does not exist in ReactiveUI v23**
- **Found during:** Task 1 (LibraryViewModel rewrite)
- **Issue:** Plan specified `ObserveOn(RxApp.MainThreadScheduler)` but ReactiveUI v23 removed the `RxApp` static class entirely; compiler error CS0103
- **Fix:** Added `IScheduler? mainThreadScheduler` constructor parameter; defaults to `CurrentThreadScheduler.Instance`; in production DI resolved via Avalonia's scheduler
- **Files modified:** src/YARL/UI/ViewModels/LibraryViewModel.cs
- **Verification:** Build passes; tests pass with `Scheduler.Immediate` passed explicitly
- **Committed in:** 943e3d7 (Task 1 commit)

**2. [Rule 1 - Bug] DynamicData GroupOn() is list-only; cache changesets use Group()**
- **Found during:** Task 1 (LibraryViewModel rewrite)
- **Issue:** Plan specified `.GroupOn(g => g.PlatformId)` but this overload only exists for `IObservable<IChangeSet<TObject>>` (list); SourceCache.Connect() produces `IObservable<IChangeSet<TObject, TKey>>` (cache); compiler error CS0411
- **Fix:** Used `Group<GameViewModel, int, string>(g => g.PlatformId)` with explicit type args and Transform overload `(group, groupKey)` to access both group cache and group key
- **Files modified:** src/YARL/UI/ViewModels/LibraryViewModel.cs
- **Verification:** Build passes; Platforms collection populates correctly in tests
- **Committed in:** 943e3d7 (Task 1 commit)

**3. [Rule 1 - Bug] DisposeWith extension method removed in ReactiveUI v23**
- **Found during:** Task 1 (LibraryViewModel rewrite)
- **Issue:** Plan used `.DisposeWith(_disposables)` but no extension method found; compiler error CS1061
- **Fix:** Replaced all `.DisposeWith(_disposables)` with `_disposables.Add(...)` wrapping the entire pipeline
- **Files modified:** src/YARL/UI/ViewModels/LibraryViewModel.cs
- **Verification:** Build passes; all subscriptions properly disposed
- **Committed in:** 943e3d7 (Task 1 commit)

**4. [Rule 2 - Missing Critical] ReactiveUI v23 requires explicit initialization for WhenAnyValue in tests**
- **Found during:** Task 2 (Unit tests)
- **Issue:** Plan specified `RxApp.MainThreadScheduler = Scheduler.Immediate` but RxApp doesn't exist; all tests failed with "ReactiveUI has not been initialized" from `EnsureInitialized()`
- **Fix:** Created `ReactiveUITestInit.cs` with `[ModuleInitializer]` that calls `RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp()` before any test runs
- **Files modified:** tests/YARL.Tests/ReactiveUITestInit.cs (created)
- **Verification:** All 36 tests pass including 5 new LibraryViewModel tests
- **Committed in:** 2592b82 (Task 2 commit)

---

**Total deviations:** 4 auto-fixed (3 bugs from ReactiveUI/DynamicData API changes, 1 missing critical for test infrastructure)
**Impact on plan:** All fixes necessary because ReactiveUI v23 (used via ReactiveUI.Avalonia 11.4.12) made breaking API changes vs v21/v22 assumptions in the plan. No scope creep.

## Issues Encountered
- MSB3492 file lock error on AssemblyInfoInputs.cache when running `dotnet test` immediately after `dotnet build` — transient Windows file lock; resolved by building with `--no-build` flag or retrying

## Next Phase Readiness
- LibraryViewModel is the live data hub ready for UI binding in Plan 04
- SourceCache populated by scanner; platforms/favorites/recently played update reactively
- No blockers for Plan 04 (UI bindings to LibraryViewModel)

---
*Phase: 02-rom-scanning-and-library*
*Completed: 2026-03-19*
