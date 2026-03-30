---
phase: 04-emulator-integration-and-launch
plan: 03
subsystem: ui
tags: [avalonia, reactiveui, launch-overlay, state-machine, dispatcher-timer, usercontrol, tdd]

# Dependency graph
requires:
  - phase: 04-02
    provides: GameLaunchService, LaunchOverlayState enum, SetStateCallback, BringToFront
  - phase: 03-06
    provides: GameDetailDrawer, GameDetailViewModel, DesktopShell layout patterns

provides:
  - LaunchOverlayViewModel with Hidden/Launching/Running/Failed state machine
  - LaunchOverlay UserControl with blurred cover art background and 3-state panels
  - Play button in GameDetailDrawer wired to LaunchCommand via GameLaunchService
  - Inline unconfigured emulator prompt with Configure in Settings navigation
  - LaunchOverlay as 3rd Grid child in DesktopShell (highest Z-order, IsVisible-gated)

affects:
  - 04-04 (EmulatorRowViewModel settings UI — IsLaunchConfigured check already wired to AppConfig)
  - 04-05 (play time display — LaunchOverlay running state timer already tracks elapsed time)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - DispatcherTimer for elapsed play time within LaunchOverlayViewModel (avoids Avalonia headless in tests)
    - Code-behind UpdatePanelVisibility pattern for state-driven panel visibility (LaunchOverlay)
    - FindAncestorOfType<DesktopShell>() in GameDetailDrawer for cross-component nav
    - IsVisible="{Binding IsVisible}" on child UserControl whose DataContext is bound to sub-VM

key-files:
  created:
    - src/YARL/UI/ViewModels/LaunchOverlayViewModel.cs
    - src/YARL/UI/Controls/LaunchOverlay.axaml
    - src/YARL/UI/Controls/LaunchOverlay.axaml.cs
    - tests/YARL.Tests/Phase4/LaunchOverlayViewModelTests.cs (fleshed out from stub)
  modified:
    - src/YARL/UI/ViewModels/MainViewModel.cs
    - src/YARL/UI/ViewModels/GameDetailViewModel.cs
    - src/YARL/UI/Desktop/DesktopShell.axaml
    - src/YARL/UI/Desktop/DesktopShell.axaml.cs
    - src/YARL/UI/Views/GameDetailDrawer.axaml
    - src/YARL/UI/Views/GameDetailDrawer.axaml.cs
    - src/YARL/Program.cs
    - tests/YARL.Tests/Foundation/ViewModelTests.cs

key-decisions:
  - "IsVisible='{Binding IsVisible}' used on LaunchOverlay in DesktopShell — when DataContext is bound to sub-VM, IsVisible must resolve against sub-VM's own DataType, not parent MainViewModel"
  - "DispatcherTimer not tested in unit tests — requires Avalonia headless infrastructure; only state transitions tested"
  - "FindAncestorOfType<DesktopShell>() requires Avalonia.VisualTree namespace — added to GameDetailDrawer.axaml.cs"
  - "NavigateToSettings() public method added to DesktopShell to allow Configure in Settings cross-component navigation"

patterns-established:
  - "LaunchOverlay: code-behind UpdatePanelVisibility driven by PropertyChanged on State — avoids converter complexity"
  - "IsLaunchConfigured: checked in LoadGame() by looking up game.PlatformId in AppConfig.EmulatorConfigs"

requirements-completed: [EMU-02]

# Metrics
duration: 8min
completed: 2026-03-30
---

# Phase 4 Plan 03: Launch Overlay UI Summary

**Full-page launch overlay with blurred cover art, live timer, and state-driven panels wired end-to-end from Play button through GameLaunchService**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-30T20:28:50Z
- **Completed:** 2026-03-30T20:36:50Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- LaunchOverlayViewModel state machine (Hidden/Launching/Running/Failed) with DismissCommand and SwitchToGameCommand
- LaunchOverlay UserControl with blurred cover art background, ProgressBar for launching, elapsed timer for running, alert panel for failed
- Play button in GameDetailDrawer fully wired to LaunchCommand — only visible when emulator configured; inline prompt with Configure in Settings navigation when not
- App builds cleanly (0 errors), all 18 Phase4 tests pass

## Task Commits

1. **Task 1: Create LaunchOverlayViewModel with state machine, timer, and commands** - `ee9027d` (feat)
2. **Task 2: Create LaunchOverlay UserControl, wire Play button, add overlay to DesktopShell** - `8ee27d2` (feat)

**Plan metadata:** _(docs commit follows)_

_Note: Task 1 used TDD — tests written first (RED), then implementation (GREEN)._

## Files Created/Modified
- `src/YARL/UI/ViewModels/LaunchOverlayViewModel.cs` - State machine VM with DispatcherTimer, Dismiss/SwitchToGame commands
- `src/YARL/UI/Controls/LaunchOverlay.axaml` - Full-page UserControl with 3 state panels and blurred cover art
- `src/YARL/UI/Controls/LaunchOverlay.axaml.cs` - Code-behind UpdatePanelVisibility driven by State PropertyChanged
- `src/YARL/UI/ViewModels/MainViewModel.cs` - Added LaunchOverlay property, updated constructor
- `src/YARL/UI/ViewModels/GameDetailViewModel.cs` - Added LaunchCommand, IsLaunchConfigured, LaunchGameAsync
- `src/YARL/UI/Desktop/DesktopShell.axaml` - LaunchOverlay added as 3rd Grid child with DataContext binding
- `src/YARL/UI/Desktop/DesktopShell.axaml.cs` - Added NavigateToSettings() public method
- `src/YARL/UI/Views/GameDetailDrawer.axaml` - Play button wired to LaunchCommand, unconfigured emulator prompt added
- `src/YARL/UI/Views/GameDetailDrawer.axaml.cs` - OnConfigureEmulatorClicked handler using FindAncestorOfType
- `src/YARL/Program.cs` - LaunchOverlayViewModel DI registration; GameDetailViewModel updated with new dependencies
- `tests/YARL.Tests/Phase4/LaunchOverlayViewModelTests.cs` - 6 real tests replacing Wave 0 stub
- `tests/YARL.Tests/Foundation/ViewModelTests.cs` - Updated for new 3-param MainViewModel constructor

## Decisions Made
- `IsVisible="{Binding IsVisible}"` (not `LaunchOverlay.IsVisible`) on the LaunchOverlay element in DesktopShell — compiled bindings resolve against the UserControl's own `x:DataType` once DataContext is re-bound, not against the parent's type
- DispatcherTimer excluded from unit tests (requires Avalonia headless app); only state transitions tested via `HandleStateChanged`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed ViewModelTests for new 3-parameter MainViewModel constructor**
- **Found during:** Task 1 (creating LaunchOverlayViewModel and updating MainViewModel)
- **Issue:** `ViewModelTests.cs` called `new MainViewModel(libraryVm, new SettingsViewModel())` — build error after adding required `launchOverlayViewModel` parameter
- **Fix:** Updated ViewModelTests to use a factory helper `CreateMainVm()` that creates a real `LaunchOverlayViewModel` with a mocked `IServiceScopeFactory`
- **Files modified:** `tests/YARL.Tests/Foundation/ViewModelTests.cs`
- **Verification:** All Foundation tests pass
- **Committed in:** `ee9027d` (Task 1 commit)

**2. [Rule 3 - Blocking] Fixed AVLN2000 compiled binding error on LaunchOverlay IsVisible**
- **Found during:** Task 2 (adding LaunchOverlay to DesktopShell)
- **Issue:** `IsVisible="{Binding LaunchOverlay.IsVisible}"` on the `<controls:LaunchOverlay>` element caused AVLN2000 — Avalonia compiled bindings resolves `IsVisible` against `LaunchOverlayViewModel` (the control's own DataType) rather than parent `MainViewModel`
- **Fix:** Changed to `IsVisible="{Binding IsVisible}"` since DataContext is already bound to the `LaunchOverlayViewModel` instance
- **Files modified:** `src/YARL/UI/Desktop/DesktopShell.axaml`
- **Verification:** Build succeeds (0 errors)
- **Committed in:** `8ee27d2` (Task 2 commit)

**3. [Rule 1 - Bug] Restored EmulatorRowViewModelTests.cs — pre-existing test fleshed out without implementation**
- **Found during:** Task 1 (first build of tests)
- **Issue:** `EmulatorRowViewModelTests.cs` had been fleshed out (no `#if false` guard) referencing `EmulatorRowViewModel` which is planned for Plan 04-04, causing CS0246 compile error
- **Fix:** Noted that `EmulatorRowViewModel` already exists in `src/YARL/UI/ViewModels/EmulatorRowViewModel.cs` (created outside of tracked plan commits). The linter restored the full test body. The `.Wait()` on `IObservable<Unit>` was also fixed to `.Subscribe()` by the linter.
- **Files modified:** `tests/YARL.Tests/Phase4/EmulatorRowViewModelTests.cs`
- **Verification:** All tests pass
- **Committed in:** `ee9027d` (Task 1 commit)

---

**Total deviations:** 3 auto-fixed (2 bugs, 1 blocking)
**Impact on plan:** All fixes necessary for compile correctness. No scope creep.

## Issues Encountered
- `ProgressRing` from the plan's AXAML template is not available in Avalonia 11 without an extra package — replaced with `ProgressBar IsIndeterminate="True"` which achieves the same visual effect using only built-in Avalonia controls.

## Next Phase Readiness
- Launch overlay UI complete end-to-end: Play button triggers service which drives overlay state
- Plan 04-04 (EmulatorRowViewModel settings UI) can now implement the emulator config screens that `IsLaunchConfigured` already depends on
- Plan 04-05 (play time display) can use the elapsed timer already running in LaunchOverlayViewModel

---
*Phase: 04-emulator-integration-and-launch*
*Completed: 2026-03-30*
