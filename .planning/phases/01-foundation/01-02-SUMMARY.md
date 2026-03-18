---
phase: 01-foundation
plan: 02
subsystem: ui-shell
tags: [dotnet, avalonia, reactiveui, efcore, sqlite, serilog, csharp, viewmodels, di]

# Dependency graph
requires:
  - Phase 01 Plan 01 (solution scaffold, domain model, YarlDbContext, AppPaths, AppConfig)
provides:
  - MainViewModel (IScreen + RoutingState + LibraryViewModel property)
  - LibraryViewModel (shared ViewModel with StatusMessage)
  - DesktopShell (1280x720 windowed stub, compiled bindings to MainViewModel)
  - FullscreenShell (fullscreen/no-decorations stub, compiled bindings to MainViewModel)
  - LocalRomSourceProvider (IRomSourceProvider for SourceType.Local)
  - OsMountedRomSourceProvider (IRomSourceProvider for SourceType.OsMounted, LIB-08)
  - Program.cs with UseReactiveUIWithMicrosoftDependencyResolver DI wiring
  - App.axaml.cs with startup mode detection (gamepad + config override) and shell selection
  - EF Core InitialCreate migration (RomSources table with string-stored SourceType)
  - YarlDbContextFactory (IDesignTimeDbContextFactory for dotnet-ef tooling)
affects: [02-rom-scanning, 03-metadata, 04-emulator-launch, 05-desktop-ui, 06-fullscreen-ui]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dual-shell startup: App.axaml.cs resolves MainViewModel from Splat locator, selects DesktopShell or FullscreenShell based on DetermineStartupMode()"
    - "Compiled AXAML bindings: x:DataType=MainViewModel on shell windows enables compiled binding to nested LibraryViewModel.StatusMessage"
    - "Design-time factory: YarlDbContextFactory (IDesignTimeDbContextFactory) enables dotnet-ef without running the full Avalonia host"
    - "EF Core migration: InitialCreate creates RomSources table, SourceType stored as string"

key-files:
  created:
    - src/YARL/UI/ViewModels/MainViewModel.cs (IScreen + RoutingState + LibraryViewModel injection)
    - src/YARL/UI/ViewModels/LibraryViewModel.cs (shared ViewModel with StatusMessage)
    - src/YARL/UI/Desktop/DesktopShell.axaml (desktop shell with compiled bindings)
    - src/YARL/UI/Desktop/DesktopShell.axaml.cs (code-behind)
    - src/YARL/UI/Fullscreen/FullscreenShell.axaml (fullscreen shell with compiled bindings)
    - src/YARL/UI/Fullscreen/FullscreenShell.axaml.cs (code-behind)
    - src/YARL/Infrastructure/Providers/LocalRomSourceProvider.cs (IRomSourceProvider for Local)
    - src/YARL/Infrastructure/Providers/OsMountedRomSourceProvider.cs (IRomSourceProvider for OsMounted, LIB-08)
    - src/YARL/Infrastructure/Persistence/YarlDbContextFactory.cs (design-time factory)
    - src/YARL/Infrastructure/Persistence/Migrations/20260318202840_InitialCreate.cs
    - src/YARL/Infrastructure/Persistence/Migrations/20260318202840_InitialCreate.Designer.cs
    - src/YARL/Infrastructure/Persistence/Migrations/YarlDbContextModelSnapshot.cs
  modified:
    - src/YARL/Program.cs (replaced temporary stub with full DI wiring + Serilog)
    - src/YARL/App.axaml.cs (replaced stub with startup mode detection + shell selection + DB migration)

key-decisions:
  - "x:DataType=MainViewModel added to both shell AXAML files; Avalonia compiled bindings require explicit DataType for binding path resolution"
  - "YarlDbContextFactory added as IDesignTimeDbContextFactory; required because dotnet-ef cannot invoke BuildAvaloniaApp() (it blocks for 5 minutes attempting Generic Host resolution)"
  - "LibraryViewModel registered before MainViewModel in DI (MainViewModel takes LibraryViewModel in constructor)"
  - "IServiceProvider resolved via Locator.Current.GetService<IServiceProvider>() in RunMigrations(); MS DI bridge registers IServiceProvider in Splat automatically"

requirements-completed: [LIB-08]

# Metrics
duration: 12min
completed: 2026-03-18
---

# Phase 1 Plan 2: Application Startup Wiring Summary

**ReactiveUI DI bridge wired with dual shells (DesktopShell/FullscreenShell) sharing MainViewModel + LibraryViewModel, Serilog logging, gamepad startup detection, EF Core InitialCreate migration applied via MigrateAsync, and IRomSourceProvider implementations for Local and OsMounted sources**

## Performance

- **Duration:** 12 min
- **Started:** 2026-03-18T20:17:40Z
- **Completed:** 2026-03-18T20:29:25Z
- **Tasks:** 2
- **Files modified:** 14

## Accomplishments

- MainViewModel implements IScreen with RoutingState; exposes LibraryViewModel as a DI-injected property for shell binding
- LibraryViewModel shared across both shells via DataContext chain (shells bind to `{Binding LibraryViewModel.StatusMessage}`)
- DesktopShell (1280x720, MinSize 800x600) and FullscreenShell (FullScreen/NoDecorations) created with Avalonia compiled bindings
- LocalRomSourceProvider and OsMountedRomSourceProvider implement IRomSourceProvider; OsMounted has LIB-08 comment explaining Phase 7 cache behavior
- Program.cs: full DI wiring via `UseReactiveUIWithMicrosoftDependencyResolver`, registering AppConfigService, YarlDbContext (SQLite + AppPaths.DatabasePath), ViewModels, and both IRomSourceProvider implementations
- Program.cs: Serilog configured with rolling daily file logs to AppPaths.LogDir, retaining 7 days
- App.axaml.cs: `DetermineStartupMode()` checks AppConfig.UiModeOverride first, then scans `/dev/input/js*` on Linux for gamepad presence, falls back to Desktop on any failure
- App.axaml.cs: `RunMigrations()` resolves YarlDbContext from scoped IServiceProvider and calls `Database.Migrate()` — creates DB on first run, upgrades on subsequent runs
- InitialCreate EF Core migration generated for RomSources table; SourceType stored as string

## Task Commits

Each task was committed atomically:

1. **Task 1: ViewModels, dual shells, and IRomSourceProvider implementations** - `4f4f9be` (feat)
2. **Task 2: Program.cs DI bridge, App.axaml.cs startup, EF Core migration** - `081428d` (feat)

## Files Created/Modified

- `src/YARL/UI/ViewModels/MainViewModel.cs` - IScreen + RoutingState + LibraryViewModel constructor injection
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` - Shared ViewModel, StatusMessage reactive property
- `src/YARL/UI/Desktop/DesktopShell.axaml` - Desktop shell stub, x:DataType=MainViewModel for compiled bindings
- `src/YARL/UI/Desktop/DesktopShell.axaml.cs` - Code-behind (InitializeComponent only)
- `src/YARL/UI/Fullscreen/FullscreenShell.axaml` - Fullscreen shell stub, x:DataType=MainViewModel
- `src/YARL/UI/Fullscreen/FullscreenShell.axaml.cs` - Code-behind (InitializeComponent only)
- `src/YARL/Infrastructure/Providers/LocalRomSourceProvider.cs` - SourceType.Local provider, async file enumeration
- `src/YARL/Infrastructure/Providers/OsMountedRomSourceProvider.cs` - SourceType.OsMounted provider (LIB-08)
- `src/YARL/Infrastructure/Persistence/YarlDbContextFactory.cs` - IDesignTimeDbContextFactory for dotnet-ef
- `src/YARL/Infrastructure/Persistence/Migrations/` - InitialCreate migration (3 files)
- `src/YARL/Program.cs` - Full DI bridge setup with Serilog, replaces temporary stub
- `src/YARL/App.axaml.cs` - Startup orchestration (migrate, detect mode, show shell)

## Decisions Made

- Added `x:DataType="vm:MainViewModel"` to both shell AXAML files. Avalonia's compiled bindings (enabled by `AvaloniaUseCompiledBindingsByDefault=true` in the csproj) require an explicit DataType directive to resolve the binding path at compile time. Without it, error AVLN2100 is thrown.
- Created `YarlDbContextFactory` (IDesignTimeDbContextFactory) because `dotnet ef` attempts to resolve the IHost via HostFactoryResolver, which timed out waiting for the Avalonia entry point (5 minutes). The factory bypasses host resolution entirely and creates the DbContext directly using AppPaths.DatabasePath.
- LibraryViewModel is registered in DI before MainViewModel since `MainViewModel(LibraryViewModel)` requires it as a constructor argument.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Avalonia compiled binding missing x:DataType directive**
- **Found during:** Task 1 (build verification)
- **Issue:** Build error AVLN2100 on both DesktopShell.axaml and FullscreenShell.axaml — `{Binding LibraryViewModel.StatusMessage}` requires x:DataType when `AvaloniaUseCompiledBindingsByDefault=true` is set in the project
- **Fix:** Added `xmlns:vm="clr-namespace:YARL.UI.ViewModels"` and `x:DataType="vm:MainViewModel"` to both shell windows
- **Files modified:** `src/YARL/UI/Desktop/DesktopShell.axaml`, `src/YARL/UI/Fullscreen/FullscreenShell.axaml`
- **Commit:** `4f4f9be`

**2. [Rule 1 - Bug] Missing ReactiveUI.Avalonia.Splat using directive in Program.cs**
- **Found during:** Task 2 (build verification)
- **Issue:** CS1061 — AppBuilder does not contain definition for UseReactiveUIWithMicrosoftDependencyResolver without the namespace import
- **Fix:** Added `using ReactiveUI.Avalonia.Splat;` to Program.cs
- **Files modified:** `src/YARL/Program.cs`
- **Commit:** `081428d`

**3. [Rule 2 - Missing Critical Functionality] YarlDbContextFactory required for dotnet-ef**
- **Found during:** Task 2 (migration generation)
- **Issue:** `dotnet ef migrations add` timed out after 5 minutes attempting to invoke the Avalonia host entry point via HostFactoryResolver. Without the factory, EF tools cannot create a YarlDbContext at design time.
- **Fix:** Created `YarlDbContextFactory : IDesignTimeDbContextFactory<YarlDbContext>` that builds DbContext options directly using AppPaths.DatabasePath — standard EF Core design-time pattern
- **Files modified:** `src/YARL/Infrastructure/Persistence/YarlDbContextFactory.cs`
- **Commit:** `081428d`

---

**Total deviations:** 3 auto-fixed (2 bugs, 1 missing critical functionality)
**Impact on plan:** All fixes were required for correct build and migration generation; no scope creep.

## Self-Check: PASSED

Verified:
- `src/YARL/UI/ViewModels/MainViewModel.cs` — FOUND
- `src/YARL/UI/ViewModels/LibraryViewModel.cs` — FOUND
- `src/YARL/UI/Desktop/DesktopShell.axaml` — FOUND
- `src/YARL/UI/Fullscreen/FullscreenShell.axaml` — FOUND
- `src/YARL/Infrastructure/Providers/LocalRomSourceProvider.cs` — FOUND
- `src/YARL/Infrastructure/Providers/OsMountedRomSourceProvider.cs` — FOUND
- `src/YARL/Infrastructure/Persistence/Migrations/20260318202840_InitialCreate.cs` — FOUND
- `src/YARL/Program.cs` (DI bridge) — FOUND
- `src/YARL/App.axaml.cs` (startup wiring) — FOUND
- Commits `4f4f9be` and `081428d` — verified in git log
- `dotnet build src/YARL --configuration Release` — exits 0

## Next Phase Readiness

- App is launchable: `dotnet run --project src/YARL` opens DesktopShell (or FullscreenShell on Linux with gamepad)
- EF Core migration framework active: future schema changes use `dotnet ef migrations add`
- DI container wired: Phase 2 adds services to the `UseReactiveUIWithMicrosoftDependencyResolver` callback in Program.cs
- Both shells have DataContext=MainViewModel with LibraryViewModel accessible for additional bindings
- Phase 2 (ROM scanning) can immediately extend YarlDbContext schema and add IHostedService scanner

---
*Phase: 01-foundation*
*Completed: 2026-03-18*
