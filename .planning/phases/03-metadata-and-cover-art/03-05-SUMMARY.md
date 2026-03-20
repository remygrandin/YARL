---
phase: 03-metadata-and-cover-art
plan: 05
subsystem: ui
tags: [avalonia, itemsrepeater, asyncimageloader, cover-art, search, filters, virtualization]

# Dependency graph
requires:
  - phase: 03-03
    provides: LibraryViewModel with SearchText, filter predicates, SelectGameCommand, GameViewModel.CoverArtPath/ScrapeStatus
  - phase: 03-04
    provides: ScraperHostedService populating CoverArtPath on games after background scraping
provides:
  - "Virtualized game grid using ItemsRepeater+UniformGridLayout (60fps with 500+ items)"
  - "Cover art display via AsyncImageLoader AdvancedImage with placeholder fallback"
  - "No art badge for ScrapeStatus=Unmatched games"
  - "Play button overlay (44x44px) on grid tile hover"
  - "Search bar with two-way SearchText binding and debounce"
  - "Search status line showing local/global match counts with search globally link"
  - "Filter chip row with Genre and Developer ComboBoxes (AND logic)"
  - "Load more button (50-result cap per LIB-04) bound to LoadMoreCommand"
  - "PathToImageConverter, NullToPlaceholderConverter, ScrapeStatusToVisibilityConverter"
affects: [03-06, 04-emulator-launch]

# Tech tracking
tech-stack:
  added: [AsyncImageLoader.Avalonia AdvancedImage control, Avalonia.Controls.ItemsRepeater 11.1.5]
  patterns: [ItemsRepeater+UniformGridLayout for virtualized grid, AdvancedImage for async art loading, irLayout namespace alias for Avalonia.Layout types from ItemsRepeater assembly]

key-files:
  created:
    - src/YARL/UI/Converters/NullToPlaceholderConverter.cs
    - src/YARL/UI/Converters/ScrapeStatusToVisibilityConverter.cs
    - src/YARL/UI/Converters/PathToImageConverter.cs
  modified:
    - src/YARL/App.axaml
    - src/YARL/UI/Views/GameListView.axaml
    - src/YARL/UI/Views/GameListView.axaml.cs
    - src/YARL/UI/ViewModels/LibraryViewModel.cs

key-decisions:
  - "UniformGridLayout is in Avalonia.Layout namespace (not Avalonia.Controls) in Avalonia.Controls.ItemsRepeater 11.1.5 — requires separate xmlns:irLayout alias pointing to same assembly"
  - "HasMoreResults/LoadMoreCommand/DisplayLimit added to LibraryViewModel — missing from 03-03 plan but required by GameListView AXAML bindings (Rule 2 auto-fix)"
  - "CollectionChanged event pattern used for HasMoreResults wiring — matches existing FavoritesFilterEmptyVisible wiring in LibraryViewModel"

patterns-established:
  - "Dual namespace alias for ItemsRepeater package: xmlns:ir for Avalonia.Controls (ItemsRepeater control), xmlns:irLayout for Avalonia.Layout (UniformGridLayout)"
  - "AdvancedImage with IsVisible bound to StringConverters.IsNotNullOrEmpty for conditional cover art display"
  - "NullToPlaceholder + IsNotNullOrEmpty pair: placeholder Border visible when null, AdvancedImage visible when non-null"

requirements-completed: [LIB-04, LIB-05, META-03]

# Metrics
duration: 15min
completed: 2026-03-20
---

# Phase 03 Plan 05: Enhanced GameListView with Virtualized Grid and Cover Art Summary

**ItemsRepeater+UniformGridLayout virtualized game grid with AsyncImageLoader cover art, search bar, genre/developer filter chips, and Load more pagination**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-20T00:00:00Z
- **Completed:** 2026-03-20T00:13:37Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments

- Migrated game grid from ItemsControl+WrapPanel to ItemsRepeater+UniformGridLayout (136x176px items) for virtualized 60fps scrolling with 500+ tiles
- Implemented cover art display via AsyncImageLoader AdvancedImage with NullToPlaceholder fallback, ScrapeStatus-based "No art" badge, and 44x44px play button overlay on hover
- Added search bar (SearchText), search status line (match counts + search globally link), Genre/Developer filter chips, Clear filters button, and Load more pagination (50-item cap per LIB-04)

## Task Commits

Each task was committed atomically:

1. **Task 1: Converters and App.axaml AsyncImageLoader registration** - `2ec2348` (feat)
2. **Task 2: GameListView ItemsRepeater grid, search, filters, cover art, Load more** - `2ba1349` (feat)

**Plan metadata:** `(pending docs commit)` (docs: complete plan)

## Files Created/Modified

- `src/YARL/App.axaml` - Added StyleInclude for AsyncImageLoader AdvancedImage.axaml
- `src/YARL/UI/Converters/NullToPlaceholderConverter.cs` - Returns true when value is null/empty (shows placeholder)
- `src/YARL/UI/Converters/ScrapeStatusToVisibilityConverter.cs` - Returns true when ScrapeStatus is Unmatched (shows No art badge)
- `src/YARL/UI/Converters/PathToImageConverter.cs` - Converts file path to Bitmap for list-mode thumbnails
- `src/YARL/UI/Views/GameListView.axaml` - Full enhanced view with ItemsRepeater grid, search, filter chips, cover art tiles, Load more
- `src/YARL/UI/Views/GameListView.axaml.cs` - Added OnSearchGloballyClicked handler
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Added DisplayLimit, HasMoreResults, LoadMoreCommand

## Decisions Made

- `UniformGridLayout` is in `Avalonia.Layout` namespace (not `Avalonia.Controls`) in the ItemsRepeater 11.1.5 package — requires a separate `xmlns:irLayout` alias pointing to the same assembly
- `HasMoreResults`, `LoadMoreCommand`, and `DisplayLimit` were missing from LibraryViewModel (not implemented in plan 03-03) — added as Rule 2 auto-fix since they are required for AXAML bindings
- Used `CollectionChanged` event handler pattern (matching existing `FavoritesFilterEmptyVisible` wiring) to update `HasMoreResults` reactively

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Functionality] Added DisplayLimit, HasMoreResults, LoadMoreCommand to LibraryViewModel**
- **Found during:** Task 2 (GameListView AXAML needs `LoadMoreCommand` and `HasMoreResults` bindings)
- **Issue:** Plan 03-03 did not implement `HasMoreResults`, `LoadMoreCommand`, or `DisplayLimit` in LibraryViewModel, yet plan 03-05 requires them in AXAML bindings. Without these, the AXAML would fail compiled binding validation or render a broken Load more button.
- **Fix:** Added `[Reactive] private int _displayLimit = 50`, `[Reactive] private bool _hasMoreResults`, `ReactiveCommand<Unit, Unit> LoadMoreCommand`, wired CollectionChanged to update `HasMoreResults`, wired `WhenAnyValue(DisplayLimit)` to recalculate.
- **Files modified:** src/YARL/UI/ViewModels/LibraryViewModel.cs
- **Verification:** Build passed, all 58 tests pass
- **Committed in:** `2ba1349` (Task 2 commit)

**2. [Rule 1 - Bug] Used separate irLayout namespace alias for UniformGridLayout**
- **Found during:** Task 2 build — `AVLN2000: Unable to resolve type UniformGridLayout from namespace clr-namespace:Avalonia.Controls`
- **Issue:** Plan specified `xmlns:ir="clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls.ItemsRepeater"` for both `ItemsRepeater` and `UniformGridLayout`. However in version 11.1.5, `UniformGridLayout` is in `Avalonia.Layout` namespace, not `Avalonia.Controls`.
- **Fix:** Added `xmlns:irLayout="clr-namespace:Avalonia.Layout;assembly=Avalonia.Controls.ItemsRepeater"` and used `irLayout:UniformGridLayout` instead of `ir:UniformGridLayout`.
- **Files modified:** src/YARL/UI/Views/GameListView.axaml
- **Verification:** Build succeeded (0 errors)
- **Committed in:** `2ba1349` (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 missing critical, 1 bug)
**Impact on plan:** Both fixes required for correct functionality and build. No scope creep.

## Issues Encountered

- ItemsRepeater 11.1.5 splits control types across two namespaces: `Avalonia.Controls` (ItemsRepeater itself) and `Avalonia.Layout` (UniformGridLayout, StackLayout, etc.) — requires two xmlns aliases despite being in the same assembly

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- GameListView now renders a virtualized cover art grid ready for real game data from the scraper pipeline
- Cover art tiles will automatically populate as ScraperHostedService processes games and updates CoverArtPath
- Plan 03-06 (detail drawer / game detail view) can use the SelectGameCommand wiring already in place

## Self-Check: PASSED

All files found:
- FOUND: src/YARL/App.axaml
- FOUND: src/YARL/UI/Converters/NullToPlaceholderConverter.cs
- FOUND: src/YARL/UI/Converters/ScrapeStatusToVisibilityConverter.cs
- FOUND: src/YARL/UI/Converters/PathToImageConverter.cs
- FOUND: src/YARL/UI/Views/GameListView.axaml
- FOUND: src/YARL/UI/Views/GameListView.axaml.cs
- FOUND: src/YARL/UI/ViewModels/LibraryViewModel.cs

All commits found:
- FOUND: 2ec2348 (feat: converters and App.axaml)
- FOUND: 2ba1349 (feat: GameListView enhanced)
- FOUND: 226eab0 (docs: plan metadata)

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-20*
