---
phase: 01-foundation
plan: 01
subsystem: infra
tags: [dotnet, avalonia, reactiveui, efcore, sqlite, serilog, xdg-directories, csharp]

# Dependency graph
requires: []
provides:
  - .NET 10 Avalonia solution (YARL.sln + src/YARL/YARL.csproj) with all Phase 1 NuGet packages
  - IRomSourceProvider interface with SourceType discriminator (Local, OsMounted) — satisfies LIB-08
  - YarlDbContext (EF Core DbContext) with DbSet<RomSource>, SourceType stored as string
  - AppPaths with XDG-compliant path resolution using Xdg.Directories (library.db, config.json, art/, logs/)
  - AppConfig + AppConfigService for JSON-based settings (uiModeOverride, romCacheDir)
  - platforms.json with 28 platform definitions shipped as Content/CopyToOutputDirectory
affects: [02-rom-scanning, 03-metadata, 04-emulator-launch, 05-desktop-ui, 06-fullscreen-ui]

# Tech tracking
tech-stack:
  added:
    - Avalonia 11.3.12 (cross-platform UI framework)
    - Avalonia.Desktop 11.3.12 (desktop backend)
    - Avalonia.Themes.Fluent 11.3.12 (FluentTheme)
    - ReactiveUI.Avalonia 11.4.12 (MVVM + routing)
    - ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection 11.4.12 (DI bridge)
    - ReactiveUI.SourceGenerators 2.6.1 (source-gen for Reactive properties)
    - Microsoft.EntityFrameworkCore.Sqlite 10.0.5 (SQLite ORM)
    - Microsoft.EntityFrameworkCore.Design 10.0.5 (migration tooling)
    - Microsoft.Extensions.Hosting 10.0.5 (Generic Host)
    - Serilog.Extensions.Hosting 10.0.0 (Serilog + Generic Host integration)
    - Serilog.Sinks.File 7.0.0 (rolling file log sink)
    - Xdg.Directories 0.1.2 (XDG Base Directory spec)
  patterns:
    - Provider pattern: IRomSourceProvider with SourceType discriminator
    - XDG path resolution via static AppPaths class with directory auto-creation
    - EF Core DbContext with string-stored enum (HasConversion<string>())
    - JSON config service: AppConfigService reads/writes AppConfig POCO
    - ReactiveUI DI bridge: UseReactiveUIWithMicrosoftDependencyResolver() in Program.cs

key-files:
  created:
    - YARL.sln (solution file at repo root)
    - src/YARL/YARL.csproj (.NET 10 project with all Phase 1 packages)
    - src/YARL/Program.cs (temporary entry point — Plan 02 replaces with full DI wiring)
    - src/YARL/App.axaml (minimal Avalonia app with FluentTheme)
    - src/YARL/App.axaml.cs (minimal App stub)
    - src/YARL/Domain/Enums/SourceType.cs (Local, OsMounted)
    - src/YARL/Domain/Enums/UIMode.cs (Desktop, Fullscreen)
    - src/YARL/Domain/Models/RomSource.cs (RomSource entity)
    - src/YARL/Domain/Interfaces/IRomSourceProvider.cs (LIB-08 contract)
    - src/YARL/Infrastructure/Persistence/YarlDbContext.cs (EF Core DbContext)
    - src/YARL/Infrastructure/Config/AppPaths.cs (XDG path resolution)
    - src/YARL/Infrastructure/Config/AppConfig.cs (config POCO)
    - src/YARL/Infrastructure/Config/AppConfigService.cs (JSON read/write)
    - src/YARL/Data/platforms.json (28 platform definitions)
  modified:
    - src/YARL/App.axaml (updated from template to minimal stub)
    - src/YARL/App.axaml.cs (updated from template to minimal stub)

key-decisions:
  - "UseReactiveUIWithMicrosoftDependencyResolver (ReactiveUI.Avalonia.Splat namespace) used in temporary Program.cs stub; Avalonia.ReactiveUI (old AvaloniaUI org package) is NOT present"
  - "Program.cs is intentionally minimal — Plan 02 will replace with Generic Host + full DI wiring"
  - "platforms.json shipped as Content/CopyToOutputDirectory=PreserveNewest (not EmbeddedResource)"
  - "AppPaths uses static initializer to create data directories on first class access"
  - "SourceType stored as string in SQLite (HasConversion<string>()) for readability"

patterns-established:
  - "Provider pattern: IRomSourceProvider interface with SourceType SupportedType property as discriminator"
  - "XDG path resolution: static AppPaths class using Xdg.Directories.BaseDirectory.DataHome"
  - "DI bridge: UseReactiveUIWithMicrosoftDependencyResolver() from ReactiveUI.Avalonia.Splat namespace"
  - "Config: AppConfigService + AppConfig POCO for JSON settings separate from SQLite game data"

requirements-completed: [LIB-08]

# Metrics
duration: 3min
completed: 2026-03-18
---

# Phase 1 Plan 1: Solution Scaffold and Domain Foundation Summary

**.NET 10 Avalonia solution with ReactiveUI DI bridge, EF Core/SQLite domain model (IRomSourceProvider, RomSource, YarlDbContext), XDG-compliant AppPaths, JSON config service, and 28-platform definitions file**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-18T20:11:14Z
- **Completed:** 2026-03-18T20:14:51Z
- **Tasks:** 2
- **Files modified:** 14

## Accomplishments
- .NET 10 Avalonia solution scaffolded with all Phase 1 NuGet packages (ReactiveUI, EF Core, Serilog, Xdg.Directories)
- Domain model established: SourceType enum (Local/OsMounted), UIMode enum, RomSource entity, IRomSourceProvider interface — satisfies LIB-08 at the contract level
- YarlDbContext with DbSet<RomSource> and SourceType stored as string, ready for EF Core migration generation
- XDG-compliant path resolution via AppPaths (library.db, config.json, art/, logs/) with auto-directory creation
- AppConfig + AppConfigService for JSON settings (uiModeOverride, romCacheDir)
- platforms.json with 28 platform definitions shipped with the app

## Task Commits

Each task was committed atomically:

1. **Task 1: Scaffold .NET solution with Avalonia and all Phase 1 packages** - `3cd9356` (feat)
2. **Task 2: Domain model, database context, config system, and platform definitions** - `f053ffe` (feat)

## Files Created/Modified
- `YARL.sln` - Solution file at repo root referencing src/YARL/YARL.csproj
- `src/YARL/YARL.csproj` - .NET 10 project with all Phase 1 packages; platforms.json as Content
- `src/YARL/Program.cs` - Temporary entry point using UseReactiveUIWithMicrosoftDependencyResolver
- `src/YARL/App.axaml` - Minimal Avalonia app with FluentTheme
- `src/YARL/App.axaml.cs` - Minimal App stub (Plan 02 wires DI + shell selection here)
- `src/YARL/Domain/Enums/SourceType.cs` - SourceType enum: Local, OsMounted
- `src/YARL/Domain/Enums/UIMode.cs` - UIMode enum: Desktop, Fullscreen
- `src/YARL/Domain/Models/RomSource.cs` - RomSource entity with SourceType discriminator
- `src/YARL/Domain/Interfaces/IRomSourceProvider.cs` - Provider contract (LIB-08)
- `src/YARL/Infrastructure/Persistence/YarlDbContext.cs` - EF Core DbContext with RomSources DbSet
- `src/YARL/Infrastructure/Config/AppPaths.cs` - XDG-compliant static path resolver
- `src/YARL/Infrastructure/Config/AppConfig.cs` - Settings POCO (uiModeOverride, romCacheDir)
- `src/YARL/Infrastructure/Config/AppConfigService.cs` - JSON read/write for AppConfig
- `src/YARL/Data/platforms.json` - 28 platform definitions (NES through Neo Geo Pocket)

## Decisions Made
- Used `UseReactiveUIWithMicrosoftDependencyResolver()` from `ReactiveUI.Avalonia.Splat` namespace (not `Avalonia.ReactiveUI` which is the older AvaloniaUI org package). The correct namespace was discovered by inspecting the package XML documentation.
- Program.cs is intentionally a temporary stub — Plan 02 replaces it with Generic Host + full DI wiring once ViewModels exist.
- platforms.json shipped as `Content/CopyToOutputDirectory=PreserveNewest` rather than EmbeddedResource, making it easier for users to inspect/extend in the output directory.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed incorrect UseReactiveUI namespace in Program.cs**
- **Found during:** Task 1 (build verification)
- **Issue:** Plan specified `using Avalonia.ReactiveUI` + `.UseReactiveUI()` but `Avalonia.ReactiveUI` package is intentionally absent (replaced by `ReactiveUI.Avalonia`). Build failed with CS0234.
- **Fix:** Inspected the `ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection` package XML docs to find correct namespace (`ReactiveUI.Avalonia.Splat`) and method signature requiring a `containerConfig` action. Used `.UseReactiveUIWithMicrosoftDependencyResolver(_ => {})` as the temporary stub.
- **Files modified:** `src/YARL/Program.cs`
- **Verification:** `dotnet build` exits 0
- **Committed in:** `3cd9356` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug fix)
**Impact on plan:** Fix required to resolve build failure caused by package namespace mismatch. No scope creep.

## Issues Encountered
- The Avalonia template had no `.sln` file — only a `.slnx` (new .NET 10 format). `dotnet new sln --force` updated the `.slnx` instead of creating `.sln`. Created the classic `.sln` format manually to satisfy the plan's `YARL.sln` acceptance criterion.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- All Phase 1 infrastructure in place: solution builds, domain model exists, DbContext ready for migration
- EF Core migration generation can run: `dotnet ef migrations add InitialCreate --project src/YARL`
- Plan 02 can immediately consume IRomSourceProvider, YarlDbContext, AppPaths, and AppConfig
- Program.cs stub will be replaced by Plan 02 with Generic Host + DI wiring + shell selection

---
*Phase: 01-foundation*
*Completed: 2026-03-18*
