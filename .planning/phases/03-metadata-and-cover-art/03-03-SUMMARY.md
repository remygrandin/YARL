---
phase: 03-metadata-and-cover-art
plan: 03
subsystem: ui
tags: [reactiveui, dynamicdata, csharp, viewmodel, search, filter, metadata]

requires:
  - phase: 03-01
    provides: "Game domain model with metadata fields, ScrapeStatus enum, GameVersion model"

provides:
  - "GameViewModel with full metadata properties including Aliases, CoverArtPath, ScrapeStatus"
  - "LibraryViewModel search with 250ms debounce, genre/developer/year filters via CombineLatest"
  - "LibraryViewModel ClearFiltersCommand, SelectGameCommand, AvailableGenres/Developers"
  - "GameDetailViewModel with EnterEditMode, Save (persists override), Discard, RevertOverride commands"

affects: [03-05, 03-06, view-binding, ui-search, ui-filters, detail-drawer]

tech-stack:
  added: ["Microsoft.Reactive.Testing 6.1.0 (test project — TestScheduler for debounce tests)"]
  patterns:
    - "CombineLatest 5-predicate filter composition: platform + search + genre + developer + year"
    - "Throttle(250ms, TestScheduler) for debounce-testable search pipelines"
    - "IServiceScopeFactory injection in ViewModel for testable DB access"
    - "DistinctValues + Sort pipelines for filter dropdown population from SourceCache"

key-files:
  created:
    - src/YARL/UI/ViewModels/GameDetailViewModel.cs
    - tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs (replaced stub)
    - tests/YARL.Tests/Phase3/MetadataOverrideTests.cs (replaced stub)
  modified:
    - src/YARL/UI/ViewModels/GameViewModel.cs
    - src/YARL/UI/ViewModels/LibraryViewModel.cs
    - tests/YARL.Tests/YARL.Tests.csproj

key-decisions:
  - "TestScheduler from Microsoft.Reactive.Testing used for debounce tests — requires AdvanceBy beyond 250ms + flush margin to drain ObserveOn scheduled items"
  - "GameDetailViewModel accesses DB via IServiceScopeFactory (not direct DbContext injection) — allows scoped lifetime per save operation and unit-testability with in-memory SQLite"
  - "CombineLatest produces Func<GameViewModel, bool> composed predicate — DynamicData Filter accepts IObservable<Func<T, bool>> directly enabling hot-swap of all 5 predicates"
  - "System.Reactive.Linq using directive required for await IObservable<T>.Execute() pattern in test methods"

patterns-established:
  - "Search+filter composition: WhenAnyValue per property → predicate observable → CombineLatest → Filter(observable)"
  - "TDD flow for reactive VMs: TestScheduler injected via mainThreadScheduler param, AdvanceBy(>debounce) to trigger Throttle then flush ObserveOn queue"
  - "Metadata override pattern: IsMetadataOverridden=true + ScrapeStatus=Override on save; scraper pipeline checks flag before writing"

requirements-completed: [LIB-04, LIB-05, META-04]

duration: 10min
completed: 2026-03-19
---

# Phase 3 Plan 03: ViewModel Search/Filter Pipeline and GameDetailViewModel Summary

**ReactiveUI DynamicData 5-predicate CombineLatest filter pipeline (search + genre + developer + year + platform) with 250ms debounce, and GameDetailViewModel persisting metadata overrides to SQLite**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-19T20:20:04Z
- **Completed:** 2026-03-19T20:29:43Z
- **Tasks:** 2 (TDD — each has test + feat commits)
- **Files modified:** 5

## Accomplishments

- Extended GameViewModel with 11 metadata properties (Description, Genre, ReleaseYear, Developer, Publisher, CoverArtPath, ScrapeStatus, ScraperMatchId, ScraperSource, IsMetadataOverridden, Aliases)
- Replaced LibraryViewModel's single platformFilter with 5-predicate CombineLatest pipeline supporting real-time search with 250ms debounce, genre/developer/year filters with AND logic, ClearFiltersCommand, SelectGameCommand, and AvailableGenres/Developers derived collections
- Created GameDetailViewModel with edit mode (enter/save/discard), metadata override persistence via EF Core, and RevertOverrideCommand to clear the override flag
- 12 new passing tests (8 LibraryViewModelPhase3Tests + 4 MetadataOverrideTests), full suite 58 tests green

## Task Commits

Each task was committed atomically using TDD (test → feat):

1. **Task 1 RED: LibraryViewModel search/filter pipeline tests** - `2abd6ce` (test)
2. **Task 1 GREEN: GameViewModel + LibraryViewModel implementation** - `825529f` (feat)
3. **Task 2 RED: MetadataOverride tests** - `1ee0fe1` (test)
4. **Task 2 GREEN: GameDetailViewModel implementation** - `a3b3068` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `src/YARL/UI/ViewModels/GameViewModel.cs` - Extended with 11 metadata properties + Aliases list from GameVersion
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Added search/filter reactive pipeline, ClearFiltersCommand, SelectGameCommand, AvailableGenres/Developers
- `src/YARL/UI/ViewModels/GameDetailViewModel.cs` - NEW: edit mode ViewModel with save/revert commands
- `tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs` - 8 tests using TestScheduler for debounce testing
- `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs` - 4 tests with in-memory SQLite
- `tests/YARL.Tests/YARL.Tests.csproj` - Added Microsoft.Reactive.Testing 6.1.0

## Decisions Made

- **TestScheduler debounce flush margin:** After setting SearchText and advancing exactly 250ms on TestScheduler, the `ObserveOn(_mainThreadScheduler)` in the Bind pipeline schedules additional work items. Tests need to advance beyond 250ms (e.g., 300ms total) to flush both the Throttle emission and the ObserveOn dispatch.
- **IServiceScopeFactory in ViewModel:** GameDetailViewModel takes `IServiceScopeFactory?` (nullable) rather than `YarlDbContext` directly, so each save creates a fresh scope with proper EF Core lifetime semantics. Tests wire a ServiceCollection with in-memory SQLite to the same connection.
- **System.Reactive.Linq required for await:** `await command.Execute()` in async test methods requires `using System.Reactive.Linq` to pull in the `GetAwaiter()` extension method on `IObservable<T>`.

## Deviations from Plan

None — plan executed exactly as written. The only adjustment was the debounce test needing 300ms (not exactly 250ms) to flush the ObserveOn queue, which is a test-mechanics detail, not a plan deviation.

## Issues Encountered

- TestScheduler debounce test initially failed because `AdvanceBy(250ms)` didn't flush `ObserveOn(TestScheduler)` scheduled work. Fixed by advancing to 300ms.
- `SourceType.LocalFolder` in test seeding — the actual enum has `SourceType.Local`. Auto-fixed (Rule 1).

## Next Phase Readiness

- GameViewModel, LibraryViewModel, and GameDetailViewModel are fully implemented and tested
- Plans 03-05 (search/filter UI) and 03-06 (detail drawer UI) can bind to these ViewModels
- AvailableGenres and AvailableDevelopers collections ready for filter dropdown binding
- ScrapeStatus and IsMetadataOverridden flow from DB through ViewModel to UI

## Self-Check: PASSED

- All 5 key files exist on disk
- All 4 task commits verified in git log (2abd6ce, 825529f, 1ee0fe1, a3b3068)
- 58/58 tests pass (full suite green)

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-19*
