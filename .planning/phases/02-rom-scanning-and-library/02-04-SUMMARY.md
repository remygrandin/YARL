---
phase: 02-rom-scanning-and-library
plan: 04
subsystem: ui
tags: [avalonia, axaml, material-icons, dynamicdata, mvvm, viewmodel, usercontrol]

# Dependency graph
requires:
  - phase: 02-rom-scanning-and-library
    provides: LibraryViewModel with DynamicData SourceCache, PlatformViewModel, GameViewModel, scan pipeline
  - phase: 01-foundation
    provides: DesktopShell Window stub, MainViewModel, DI wiring, YarlDbContext
provides:
  - Full Library UI: sidebar, platform grid, game list (grid+list), carousels, scan status bar, empty state, Add ROM Source dialog
  - Avalonia AXAML views bound to LibraryViewModel via compiled bindings
  - Navigation wiring in DesktopShell between LibraryView and GameListView
affects:
  - Phase 04 (game launch) — GameListView tile interaction wires into launch
  - Phase 03 (metadata scraping) — platform grid tiles will show scraped artwork
  - Phase 06 (fullscreen shell) — same ViewModel layer reused by FullscreenShell

# Tech tracking
tech-stack:
  added:
    - Material.Icons.Avalonia 3.0.0 (icon library for Avalonia)
  patterns:
    - ItemsControl + WrapPanel as grid layout pattern (replaces non-existent ItemsRepeater)
    - Compiled bindings (x:DataType) on all UserControls and DataTemplates
    - Code-behind navigation (DesktopShell switches ContentControl.Content between LibraryView and GameListView)
    - Separate UserControl per view area (LibraryView, PlatformGridView, GameListView, ScanStatusBar, EmptyStateView)
    - Dialog instantiated from code-behind with DI dependencies via constructor parameter

key-files:
  created:
    - src/YARL/UI/Views/LibraryView.axaml(.cs) — Main library content: empty/scanning/content states, carousels, platform grid
    - src/YARL/UI/Views/PlatformGridView.axaml(.cs) — Platform tile grid (WrapPanel-based)
    - src/YARL/UI/Views/GameListView.axaml(.cs) — Game list with grid/list toggle, favorites filter, back nav
    - src/YARL/UI/Views/ScanStatusBar.axaml(.cs) — Bottom scan progress bar with spinner and cancel
    - src/YARL/UI/Views/EmptyStateView.axaml(.cs) — Empty library state with Add ROM Source CTA
    - src/YARL/UI/Dialogs/AddRomSourceDialog.axaml(.cs) — Modal dialog for adding ROM source with folder browser
    - src/YARL/UI/Converters/BoolToOpacityConverter.cs — 0.5 opacity for missing games
    - src/YARL/UI/Converters/GameStatusToVisibilityConverter.cs — Shows warning icon for Missing status
    - src/YARL/UI/Converters/BoolToHeartColorConverter.cs — Accent/muted color for favorite heart icon
    - src/YARL/UI/Converters/BoolToHeartIconConverter.cs — Filled/outline heart icon based on IsFavorite
  modified:
    - src/YARL/YARL.csproj — Added Material.Icons.Avalonia 3.0.0 package reference
    - src/YARL/UI/Desktop/DesktopShell.axaml — Full sidebar + ContentControl layout replacing stub
    - src/YARL/UI/Desktop/DesktopShell.axaml.cs — Navigation wiring: sidebar buttons, content switching
    - src/YARL/UI/ViewModels/LibraryViewModel.cs — Added computed visibility properties, FilteredGames, ShowFavoritesOnly

key-decisions:
  - "ItemsControl+WrapPanel used for grid layouts: ItemsRepeater and UniformGridLayout do not exist in Avalonia 11.3.12 (they were WinUI3 concepts mistakenly included in original plan)"
  - "Kind='ViewGrid' used for grid icon: MaterialIconKind.GridView does not exist in Material.Icons 3.0.0; ViewGrid is the correct enum value"
  - "AddRomSourceDialog has explicit parameterless constructor alongside DI constructor: AXAML compiler AVLN3001 requires visible parameterless ctor even though dialog is instantiated from code-behind"
  - "WrapPanel ItemWidth/ItemHeight set to tile size + gutter (196x216 for 180x200 tiles, 136x176 for 120x160 tiles) to achieve consistent spacing"

patterns-established:
  - "Grid of tiles: use ItemsControl with WrapPanel ItemsPanel (ItemWidth/ItemHeight for uniform sizing)"
  - "Horizontal carousel: ItemsControl with StackPanel(Horizontal) in horizontal ScrollViewer"
  - "MaterialIcon Kind values: use exact MaterialIconKind enum names (ViewGrid, not GridView; FormatListBulleted; Sync; Heart; History; Warning; FolderOpen)"
  - "Dialog windows: always add explicit public parameterless constructor to avoid AVLN3001"

requirements-completed: [LIB-03, LIB-06, LIB-07]

# Metrics
duration: 30min
completed: 2026-03-19
---

# Phase 2 Plan 4: Library UI Views (Avalonia AXAML) Summary

**Avalonia AXAML library UI with sidebar navigation, WrapPanel platform grid, dual-mode game list, DynamicData-bound carousels, scan status bar, empty state CTA, and Add ROM Source dialog using Material.Icons.Avalonia**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-03-19T09:00:00Z
- **Completed:** 2026-03-19T09:30:00Z
- **Tasks:** 1 of 2 committed (Task 2 pending human visual inspection)
- **Files modified:** 20

## Accomplishments

- Full desktop Library UI bound to LibraryViewModel via Avalonia compiled bindings
- Platform grid with 180x200 tiles using WrapPanel, hover states, click-to-navigate
- Game list with grid/list toggle, favorites filter, per-game missing status indicator
- Sidebar with 4 nav items, app logo, Rescan button with material icons
- Add ROM Source dialog with StorageProvider folder picker and DI-based DB persistence
- All AXAML compiles clean: 0 errors, 0 warnings in both Debug and Release

## Task Commits

Each task was committed atomically:

1. **Task 2-04-01: Avalonia Views** - `9a634c6` (feat)

**Plan metadata:** pending (after checkpoint approval)

## Files Created/Modified

- `src/YARL/YARL.csproj` - Added Material.Icons.Avalonia 3.0.0
- `src/YARL/UI/Desktop/DesktopShell.axaml` - Full sidebar + ContentControl layout
- `src/YARL/UI/Desktop/DesktopShell.axaml.cs` - Navigation wiring between views
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Added FilteredGames, ShowFavoritesOnly, computed visibility props
- `src/YARL/UI/Views/LibraryView.axaml(.cs)` - Main library: empty/scanning/content states + carousels
- `src/YARL/UI/Views/PlatformGridView.axaml(.cs)` - Platform tile grid (WrapPanel)
- `src/YARL/UI/Views/GameListView.axaml(.cs)` - Game list: grid+list modes, favorites filter
- `src/YARL/UI/Views/ScanStatusBar.axaml(.cs)` - Scan progress bar at window bottom
- `src/YARL/UI/Views/EmptyStateView.axaml(.cs)` - Empty state with Add ROM Source button
- `src/YARL/UI/Dialogs/AddRomSourceDialog.axaml(.cs)` - Modal ROM source dialog
- `src/YARL/UI/Converters/BoolToOpacityConverter.cs` - 0.5 opacity for missing game overlay
- `src/YARL/UI/Converters/GameStatusToVisibilityConverter.cs` - Warning icon visibility for Missing
- `src/YARL/UI/Converters/BoolToHeartColorConverter.cs` - Favorite heart color (accent vs muted)
- `src/YARL/UI/Converters/BoolToHeartIconConverter.cs` - Filled vs outline heart icon

## Decisions Made

- ItemsControl + WrapPanel as grid layout: ItemsRepeater/UniformGridLayout don't exist in Avalonia 11
- Kind="ViewGrid" not Kind="GridView": correct MaterialIconKind enum value in 3.0.0
- Explicit parameterless constructor on AddRomSourceDialog: required for AVLN3001 compliance

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Replaced ItemsRepeater+UniformGridLayout with ItemsControl+WrapPanel**
- **Found during:** Task 2-04-01 (Build verification)
- **Issue:** ItemsRepeater and UniformGridLayout are WinUI3 controls not present in Avalonia 11.3.12; AXAML compiler reported AVLN2000 errors
- **Fix:** Replaced all ItemsRepeater with ItemsControl using ItemsPanel/ItemsPanelTemplate; replaced UniformGridLayout with WrapPanel (ItemWidth/ItemHeight for uniform tile sizing); replaced StackLayout with StackPanel
- **Files modified:** src/YARL/UI/Views/PlatformGridView.axaml, src/YARL/UI/Views/GameListView.axaml
- **Verification:** dotnet build src/YARL -c Debug reports 0 errors
- **Committed in:** 9a634c6 (Task 2-04-01 commit)

**2. [Rule 1 - Bug] Fixed invalid MaterialIconKind.GridView — replaced with ViewGrid**
- **Found during:** Task 2-04-01 (Build verification)
- **Issue:** MaterialIconKind.GridView does not exist in Material.Icons 3.0.0; AXAML compiler reported AVLN3000 because the string could not be converted to the enum type
- **Fix:** Replaced Kind="GridView" with Kind="ViewGrid" in DesktopShell.axaml and GameListView.axaml
- **Files modified:** src/YARL/UI/Desktop/DesktopShell.axaml, src/YARL/UI/Views/GameListView.axaml
- **Verification:** dotnet build src/YARL -c Debug reports 0 errors
- **Committed in:** 9a634c6 (Task 2-04-01 commit)

**3. [Rule 1 - Bug] Added explicit parameterless constructor to AddRomSourceDialog**
- **Found during:** Task 2-04-01 (Build verification)
- **Issue:** AXAML compiler reported AVLN3001 warning: "no public constructor was found" because optional parameter constructor is not recognized as parameterless by the AXAML loader
- **Fix:** Added explicit `public AddRomSourceDialog() : this(null) { }` alongside the DI constructor
- **Files modified:** src/YARL/UI/Dialogs/AddRomSourceDialog.axaml.cs
- **Verification:** dotnet build src/YARL -c Debug reports 0 warnings
- **Committed in:** 9a634c6 (Task 2-04-01 commit)

---

**Total deviations:** 3 auto-fixed (3 x Rule 1 — bug/incompatibility)
**Impact on plan:** All auto-fixes necessary for build correctness. No scope creep. Visual result equivalent to plan specification.

## Issues Encountered

None beyond the auto-fixed AXAML issues above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Library UI complete and bound to LibraryViewModel; ready for Phase 3 (metadata scraping)
- Game launch (Phase 4) can wire into GameListView tile tap events
- Fullscreen shell (Phase 6) can reuse the same LibraryViewModel without modification
- Pending: Task 2-04-02 visual inspection must be approved before this plan is fully closed

---
*Phase: 02-rom-scanning-and-library*
*Completed: 2026-03-19*
