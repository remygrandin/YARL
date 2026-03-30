---
phase: 04-emulator-integration-and-launch
plan: 04
subsystem: settings-ui
tags: [emulator-config, settings, reactive-ui, avalonia, flatpak]
dependency_graph:
  requires: [04-02]
  provides: [emulator-config-ui, emulator-rows-viewmodel]
  affects: [04-03]
tech_stack:
  added: []
  patterns: [ReactiveObject-WhenAnyValue, ItemsControl-DataTemplate, ContextMenu-flyout]
key_files:
  created:
    - src/YARL/UI/ViewModels/EmulatorRowViewModel.cs
  modified:
    - src/YARL/UI/ViewModels/SettingsViewModel.cs
    - src/YARL/UI/Views/SettingsView.axaml
    - src/YARL/UI/Views/SettingsView.axaml.cs
    - src/YARL/Infrastructure/Scanning/PlatformRegistry.cs
    - src/YARL/Program.cs
    - tests/YARL.Tests/Phase4/EmulatorRowViewModelTests.cs
    - tests/YARL.Tests/Foundation/ViewModelTests.cs
decisions:
  - EmulatorRowViewModel uses WhenAnyValue reactive subscription to compute IsPathValid from ExePath+IsFlatpak — Flatpak app IDs always valid, file paths checked via File.Exists
  - Browse button hidden via IsVisible="{Binding !IsFlatpak}" in DataTemplate — no separate bool property needed
  - Add platform uses Avalonia ContextMenu opened programmatically on button — avoids need for dialog window
  - AllPlatforms property added to PlatformRegistry as IReadOnlyList<PlatformDefinition> — needed for GetAvailablePlatformsForAdd dropdown
  - SaveCommand mutates AppConfig.EmulatorConfigs in-place and calls AppConfigService.Save — AppConfig is singleton, mutation is safe
metrics:
  duration_minutes: 25
  completed_date: "2026-03-30"
  tasks_completed: 2
  files_changed: 8
---

# Phase 04 Plan 04: Settings EMULATORS Section Summary

**One-liner:** Per-platform emulator config UI in Settings with reactive path validity, Flatpak toggle, file picker, and per-row Save — delivers EMU-01.

## What Was Built

### EmulatorRowViewModel (`src/YARL/UI/ViewModels/EmulatorRowViewModel.cs`)

Reactive per-row ViewModel for one emulator configuration entry:
- `ExePath`, `Args`, `IsFlatpak`, `IsPathValid` as `[Reactive]` properties
- `IsFlatpakAvailable` — static bool, `false` on Windows/macOS, `true` on Linux
- Reactive `WhenAnyValue(ExePath, IsFlatpak)` subscription: when `IsFlatpak=true`, `IsPathValid=true` regardless; otherwise `File.Exists(path)` check
- `SaveCommand` mutates `AppConfig.EmulatorConfigs[PlatformId]` and calls `AppConfigService.Save`
- Accepts optional `EmulatorConfig? existingConfig` to pre-populate fields from saved config

### SettingsViewModel additions (`src/YARL/UI/ViewModels/SettingsViewModel.cs`)

- `ObservableCollection<EmulatorRowViewModel> EmulatorRows`
- `LoadEmulatorRows()` — queries DB for platforms with games, unions with existing configured platforms, populates `EmulatorRows`
- `AddEmulatorRow(string platformId, string platformName)` — adds row if not already present
- `GetAvailablePlatformsForAdd()` — returns platforms from `PlatformRegistry.AllPlatforms` not in `EmulatorRows`
- `PlatformRegistry` injected as optional constructor parameter
- `LoadEmulatorRows()` called at end of `LoadSourcesAsync()`

### PlatformRegistry addition (`src/YARL/Infrastructure/Scanning/PlatformRegistry.cs`)

- Added `IReadOnlyList<PlatformDefinition> AllPlatforms` property — needed by `GetAvailablePlatformsForAdd`

### SettingsView EMULATORS section (`src/YARL/UI/Views/SettingsView.axaml`)

Added full EMULATORS section with:
- `ItemsControl` bound to `EmulatorRows` with `x:DataType="vm:EmulatorRowViewModel"` DataTemplate
- Per-row: platform name header, exe/app-ID TextBox, Browse button (hidden when Flatpak), Args TextBox
- Flatpak CheckBox (`IsEnabled="{Binding IsFlatpakAvailable}"` — disabled on Windows)
- Path validity icons: green CheckCircle when valid, orange AlertCircle when invalid
- Per-row Save button bound to `SaveCommand`
- Yellow Flatpak warning border (`IsVisible="{Binding IsFlatpak}"`) explaining filesystem sandbox risk
- "+ Add platform" button opening `ContextMenu` dropdown of unconfigured platforms

### Code-behind additions (`src/YARL/UI/Views/SettingsView.axaml.cs`)

- `OnBrowseEmulatorClicked` — `StorageProvider.OpenFilePickerAsync` sets `row.ExePath`
- `OnAddEmulatorPlatformClicked` — builds `ContextMenu` from `GetAvailablePlatformsForAdd()`, opens on button

## Tests

5 TDD tests in `tests/YARL.Tests/Phase4/EmulatorRowViewModelTests.cs`, all passing:
- `PathValidity_ExistingFile_ShowsValid`
- `PathValidity_NonExistentFile_ShowsInvalid`
- `IsFlatpakAvailable_OnWindows_ReturnsFalse`
- `FlatpakChecked_SkipsFileExistsCheck`
- `SaveCommand_PersistsToAppConfig`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] PlatformRegistry missing AllPlatforms property**
- **Found during:** Task 1 implementation
- **Issue:** `GetAvailablePlatformsForAdd()` in plan referenced `_platformRegistry.AllPlatforms` but `PlatformRegistry` only had `Resolve()` and `IsAllowedExtension()` methods — no collection accessor
- **Fix:** Added `IReadOnlyList<PlatformDefinition> AllPlatforms { get; }` property, stored platforms list in constructor
- **Files modified:** `src/YARL/Infrastructure/Scanning/PlatformRegistry.cs`
- **Commit:** 7bf43e1

**2. [Rule 1 - Bug] ViewModelTests.cs failing to compile**
- **Found during:** Task 1 test run
- **Issue:** `Foundation/ViewModelTests.cs` called `new MainViewModel(libraryVm, new SettingsViewModel())` but `MainViewModel` constructor gained a required `LaunchOverlayViewModel` parameter in Plan 04-03 — pre-existing compile error blocking test build
- **Fix:** Updated `ViewModelTests.cs` to construct `GameLaunchService` + `LaunchOverlayViewModel` and pass to `MainViewModel`
- **Files modified:** `tests/YARL.Tests/Foundation/ViewModelTests.cs`
- **Commit:** 7bf43e1

**3. [Rule 3 - Blocking] `out var` doesn't work with null-conditional `?.TryGetValue`**
- **Found during:** Task 1 build
- **Issue:** `_appConfig?.EmulatorConfigs.TryGetValue(platformId, out var existing)` causes CS0165 — C# doesn't allow `out var` with conditional member access
- **Fix:** Declared `EmulatorConfig? existing = null;` separately before conditional TryGetValue call
- **Files modified:** `src/YARL/UI/ViewModels/SettingsViewModel.cs`
- **Commit:** 7bf43e1

**4. [Rule 3 - Blocking] IObservable.Wait() does not exist**
- **Found during:** Task 1 test build
- **Issue:** Plan suggested `vm.SaveCommand.Execute().Wait()` but ReactiveUI `ReactiveCommand.Execute()` returns `IObservable<T>`, not `Task` — no `Wait()` method
- **Fix:** Used `.Subscribe()` with a short `Thread.Sleep(50)` for reactive pipeline to flush
- **Files modified:** `tests/YARL.Tests/Phase4/EmulatorRowViewModelTests.cs`
- **Commit:** 7bf43e1

## Self-Check: PASSED
