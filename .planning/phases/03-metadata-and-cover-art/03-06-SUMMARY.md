---
phase: 03-metadata-and-cover-art
plan: 06
subsystem: ui
tags: [avalonia, axaml, drawer, slide-animation, settings, scraping-progress, compiled-bindings]

# Dependency graph
requires:
  - phase: 03-03
    provides: GameDetailViewModel with LoadGame(), IsEditMode, Save/Discard/EnterEditMode commands
  - phase: 03-04
    provides: ScrapingStatusViewModel with ProgressPercent, StatusText, RescrapeLibraryCommand

provides:
  - GameDetailDrawer.axaml: 320px right-side slide-in drawer with metadata display and inline edit mode
  - InteractiveSearchDialog.axaml: modal Window for manual scraper match selection
  - SettingsView.axaml: enhanced with METADATA SCRAPING section (progress bar, status, re-scrape button)
  - DesktopShell.axaml: updated to host GameDetailDrawer overlay on ContentArea
  - SettingsViewModel: ScrapingStatusViewModel property added, DI registration updated

affects: [phase-04-emulator-launch, visual-verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - TransformOperationsTransition for slide-in drawer animation (translateX 200ms CubicEaseOut)
    - DockPanel.DataContext override (ScrapingStatus) to delegate binding scope within SettingsView
    - InteractiveSearchDialogModel implements INotifyPropertyChanged as lightweight dialog binding target
    - GameDetailDrawer.drawerPanel.drawerOpen CSS class toggle triggers slide animation

key-files:
  created:
    - src/YARL/UI/Views/GameDetailDrawer.axaml
    - src/YARL/UI/Views/GameDetailDrawer.axaml.cs
    - src/YARL/UI/Views/InteractiveSearchDialog.axaml
    - src/YARL/UI/Views/InteractiveSearchDialog.axaml.cs
  modified:
    - src/YARL/UI/Desktop/DesktopShell.axaml
    - src/YARL/UI/Desktop/DesktopShell.axaml.cs
    - src/YARL/UI/Views/SettingsView.axaml
    - src/YARL/UI/ViewModels/SettingsViewModel.cs
    - src/YARL/Program.cs

key-decisions:
  - "GameDetailDrawer uses CSS class toggle (drawerOpen) on inner Border for slide animation — allows TransformOperationsTransition to trigger on class change"
  - "InteractiveSearchDialogModel implements INotifyPropertyChanged directly (not ReactiveObject) to avoid circular namespace and keep the dialog self-contained"
  - "ScrapingStatusViewModel injected into SettingsViewModel as optional parameter (nullable) to preserve testability without DI"
  - "DrawerBorder drawerOpen class applied from DesktopShell.axaml.cs via FindControl — avoids compiled binding complexity on Window-scoped state"

patterns-established:
  - "Slide drawer pattern: UserControl wraps a Border.drawerPanel with translateX default + TransformOperationsTransition; parent toggles .drawerOpen class"
  - "Dialog binding model: lightweight INotifyPropertyChanged class in same file as dialog code-behind for simple modal state"

requirements-completed: [META-01, META-04]

# Metrics
duration: 3min
completed: 2026-03-20
---

# Phase 3 Plan 6: Complete Phase 3 UI Surfaces Summary

**Right-side game detail drawer (320px slide-in), interactive search dialog, and settings scraping progress panel completing the Phase 3 UI layer**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-03-20T00:16:22Z
- **Completed:** 2026-03-20T00:19:57Z
- **Tasks:** 2 of 3 (Task 3 is a human-verify checkpoint)
- **Files modified:** 9

## Accomplishments
- Game detail drawer (GameDetailDrawer.axaml): 320px slide-in from right with 200ms CubicEaseOut animation, view and edit modes, boxart, aliases expander, metadata grid, description, Play + Edit buttons
- Interactive search dialog (InteractiveSearchDialog.axaml): 640x480 modal Window with search box, results list, Select button, empty state — AVLN3001-compliant parameterless constructor
- Settings METADATA SCRAPING section: progress bar (accent fill, hidden when idle), status text, unmatched count (red), re-scrape button
- DesktopShell updated to host drawer overlay on ContentArea Grid, wiring SelectedGame to GameDetailViewModel.LoadGame()
- SettingsViewModel and Program.cs updated: ScrapingStatusViewModel injected into SettingsViewModel

## Task Commits

1. **Task 1: GameDetailDrawer, InteractiveSearchDialog, DesktopShell wiring** - `e93e338` (feat)
2. **Task 2: Settings scraping progress section and DI wiring** - `1cb3a67` (feat)

## Files Created/Modified
- `src/YARL/UI/Views/GameDetailDrawer.axaml` - Right-side 320px slide-in detail drawer with slide animation, view/edit modes
- `src/YARL/UI/Views/GameDetailDrawer.axaml.cs` - Code-behind; Escape key closes drawer
- `src/YARL/UI/Views/InteractiveSearchDialog.axaml` - Modal Window for manual match selection, x:DataType on DataTemplate
- `src/YARL/UI/Views/InteractiveSearchDialog.axaml.cs` - Search delegate, parameterless ctor, Select closes dialog
- `src/YARL/UI/Desktop/DesktopShell.axaml` - ContentArea wrapped in Grid with GameDetailDrawer overlay
- `src/YARL/UI/Desktop/DesktopShell.axaml.cs` - SelectedGame subscription → LoadGame() + drawerOpen class toggle
- `src/YARL/UI/Views/SettingsView.axaml` - METADATA SCRAPING section added with ProgressBar, StatusText, UnmatchedCount, Re-scrape button
- `src/YARL/UI/ViewModels/SettingsViewModel.cs` - ScrapingStatusViewModel property + constructor parameter
- `src/YARL/Program.cs` - SettingsViewModel DI registration updated to inject ScrapingStatusViewModel

## Decisions Made
- CSS class toggle pattern for drawer animation: `drawerPanel.drawerOpen` triggers TransformOperationsTransition from translateX(320px) to translateX(0px)
- InteractiveSearchDialogModel as plain INotifyPropertyChanged for lightweight dialog binding (avoids ReactiveObject dependency in dialog layer)
- ScrapingStatusViewModel nullable in SettingsViewModel constructor to preserve unit testability

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed InteractiveSearchDialog.axaml compiled binding errors**
- **Found during:** Task 1 (first build attempt)
- **Issue:** DataTemplate for SearchResultItem lacked `DataType="local:SearchResultItem"` and Window had no `x:DataType`, causing AVLN2100/AVLN2000 errors
- **Fix:** Added `x:DataType="local:InteractiveSearchDialogModel"` on Window, `DataType="local:SearchResultItem"` on DataTemplate, namespace alias `xmlns:local`
- **Files modified:** src/YARL/UI/Views/InteractiveSearchDialog.axaml
- **Verification:** dotnet build exits 0
- **Committed in:** e93e338 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 binding error)
**Impact on plan:** Fix required for Avalonia compiled bindings compliance. No scope creep.

## Issues Encountered
- Avalonia compiled bindings require explicit x:DataType on all AXAML files and DataTemplates — fixed inline during Task 1 build iteration

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All Phase 3 UI surfaces are code-complete and build-verified
- Task 3 (human-verify checkpoint) requires running the app and visually confirming: drawer slide animation, edit mode, search/filters, scraping progress in Settings
- Phase 4 (emulator launch) can proceed after visual verification is approved

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-20*
