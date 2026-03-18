# Phase 1: Foundation - Research

**Researched:** 2026-03-18
**Domain:** .NET 10 + Avalonia 11 + ReactiveUI + EF Core + GitHub Actions (cross-platform skeleton)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**MVVM Framework**
- ReactiveUI for MVVM — chosen for IScreen/RoutingState routing that maps to the dual-shell navigation model
- Hot-swap mode switching: DesktopShell and FullscreenShell swap in the same window at runtime, sharing the same ViewModel layer
- Auto-detect startup mode: if a controller/gamepad is detected at launch, start in fullscreen mode; otherwise start in desktop mode
- Mode preference is overridable in settings — user can force "always desktop" or "always fullscreen"
- Settings override persists and takes priority over auto-detection

**Solution Structure**
- Single .NET project with folder-based organization (not multi-project)
- Namespace: YARL (all caps — matches branding)
- Layout: src/YARL/ for the app, tests/ for test projects
- Hosted on GitHub

**CI/CD**
- GitHub Actions for CI
- Must run on both Windows and Linux runners (cross-platform verification)

**Database and Config**
- Settings/preferences stored in JSON config file (not SQLite)
- Game data (ROMs, metadata, play history) stored in SQLite via EF Core
- Data files stored in XDG-compliant paths: ~/.local/share/yarl/ on Linux, %AppData%/YARL/ on Windows
- ROM cache directory is fully configurable in settings (important for Steam Deck SD card usage)
- Platform definitions shipped as a default JSON file with user-extensible entries (not hardcoded enum)

### Claude's Discretion

- DI framework choice (Microsoft.Extensions.DI vs Splat — pick what integrates best with ReactiveUI + Generic Host)
- Test framework and project setup (xUnit vs NUnit, whether to include in Phase 1 or defer)
- CI pipeline specifics (build + test, lint checks, what triggers on push vs PR)
- Database schema scope for Phase 1 (minimal skeleton vs fuller domain model — define what's needed for foundation to be useful to Phase 2)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| LIB-08 | User can tag a path as "remote" (OS-mounted network share) | IRomSourceProvider interface defined in Phase 1 enables tagging paths with a SourceType discriminator (Local vs OsMounted); the interface is researched and pattern-documented below |
</phase_requirements>

---

## Summary

Phase 1 establishes the skeleton that every subsequent phase builds on. The main technical work is: .NET 10 solution structure with folder-based layout, Avalonia 11.3.x app shell wired to ReactiveUI IScreen/RoutingState, dual-shell stubs (DesktopShell + FullscreenShell) sharing a LibraryViewModel, EF Core 10 + SQLite with migration framework active, IRomSourceProvider interface for LIB-08, XDG-compliant data path resolution, and a GitHub Actions CI pipeline running on both Windows and Linux.

All packages have been version-verified as of March 2026. Avalonia is at 11.3.12 (stable), ReactiveUI.Avalonia at 11.4.12, and EF Core at 10.0.5. The key DI integration decision (Claude's discretion) resolves to using `ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection` (also 11.4.12) which provides `UseReactiveUIWithMicrosoftDependencyResolver()` — this bridges ReactiveUI's Splat locator to IServiceCollection cleanly and supports the Generic Host pattern.

The database schema for Phase 1 should be minimal but forward-looking: a `RomSources` table (path, SourceType enum, label) and an empty `Roms` table stub is sufficient for Phase 2 to have a schema to extend. Migration framework must be active from commit 1 — this is a hard precondition for any future schema change to be safe.

**Primary recommendation:** Scaffold with `dotnet new avalonia.app` (Avalonia templates), wire in `UseReactiveUIWithMicrosoftDependencyResolver()` for DI, define IRomSourceProvider with SourceType = {Local, OsMounted} immediately (satisfies LIB-08), create EF Core initial migration, and set up the GitHub Actions matrix workflow before writing any UI. Linux CI must be green before the first UI component is merged.

---

## Standard Stack

### Core (Phase 1 packages only)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 10.0 LTS (10.0.5) | Runtime + SDK | Current LTS, 3-year support window; verified March 2026 |
| Avalonia | 11.3.12 | Cross-platform UI framework | Only mature .NET framework with first-class Linux support; verified NuGet March 2026 |
| Avalonia.Desktop | 11.3.12 | Desktop platform backend | Required for Windows + Linux desktop target |
| ReactiveUI.Avalonia | 11.4.12 | MVVM + routing + bindings for Avalonia | Avalonia's own templates default to this; IScreen/RoutingState provide the dual-shell routing model |
| ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection | 11.4.12 | Bridges Splat locator to IServiceCollection | Enables Generic Host + MS DI with ReactiveUI; `UseReactiveUIWithMicrosoftDependencyResolver()` extension |
| ReactiveUI.SourceGenerators | 1.x (latest) | Source-generator for [Reactive] property boilerplate | Reduces manual `RaiseAndSetIfChanged` boilerplate |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.5 | SQLite ORM with migrations | Standard pattern for desktop .NET; verified NuGet March 2026 |
| Microsoft.EntityFrameworkCore.Design | 10.0.5 | dotnet-ef tooling support | Required for migration generation; dev-only reference |
| Microsoft.Extensions.Hosting | 10.0.0 | Generic Host + DI container + lifetime management | Provides IHostedService for background services (scanner will use this in Phase 2) |
| Serilog | 4.x | Structured logging | Critical for diagnosing issues on user machines |
| Serilog.Extensions.Hosting | 8.x | Integrates Serilog with Generic Host | Standard integration point |
| Serilog.Sinks.File | 5.x | Rolling file log sink | Cross-platform, works on Linux |
| Xdg.Directories | 0.1.2 | XDG Base Directory spec for data paths | Small 11KB library; handles ~/.local/share/ on Linux and %AppData% on Windows |

### Test Project (Phase 1 — include now)

| Library | Version | Purpose | Reason |
|---------|---------|---------|--------|
| xUnit | 2.x | Test framework | Standard .NET test framework; Avalonia community uses xUnit |
| Avalonia.Headless.XUnit | 11.3.x | Headless UI test runner | Enables testing Avalonia views on Linux CI with no X11/Wayland display; MUST be same version as Avalonia |
| Avalonia.Themes.Fluent | 11.3.12 | Required by headless test setup | Headless app needs a theme; Fluent is standard Avalonia theme |
| NSubstitute | 5.x | Mocking framework | More ergonomic than Moq for interface mocks |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection | Splat locator directly | Splat alone is simpler to set up but creates friction when Phase 2+ adds IHostedService — MS DI is the better long-term choice |
| ReactiveUI | CommunityToolkit.Mvvm | Simpler but lacks IScreen/RoutingState — the dual-shell navigation model requires ReactiveUI's routing; confirmed locked decision |
| Xdg.Directories | Environment.GetFolderPath | GetFolderPath gives LocalApplicationData but does not set XDG_DATA_HOME on Linux; Xdg.Directories properly honors the XDG spec (respects user overrides) |
| xUnit + Avalonia.Headless.XUnit | NUnit + Avalonia.Headless.NUnit | Both are officially supported; xUnit is more common in Avalonia samples and community; choose xUnit |

**Installation (Phase 1 only):**
```bash
# Install Avalonia templates (once per dev machine)
dotnet new install Avalonia.Templates

# Scaffold project
dotnet new avalonia.app -n YARL -o src/YARL

# Core dependencies
dotnet add src/YARL package Avalonia --version 11.3.12
dotnet add src/YARL package Avalonia.Desktop --version 11.3.12
dotnet add src/YARL package ReactiveUI.Avalonia --version 11.4.12
dotnet add src/YARL package ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection --version 11.4.12
dotnet add src/YARL package ReactiveUI.SourceGenerators

# Database
dotnet add src/YARL package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.5
dotnet add src/YARL package Microsoft.EntityFrameworkCore.Design --version 10.0.5

# Hosting + logging
dotnet add src/YARL package Microsoft.Extensions.Hosting
dotnet add src/YARL package Serilog.Extensions.Hosting
dotnet add src/YARL package Serilog.Sinks.File

# XDG paths
dotnet add src/YARL package Xdg.Directories

# Test project
dotnet new xunit -n YARL.Tests -o tests/YARL.Tests
dotnet add tests/YARL.Tests package Avalonia.Headless.XUnit
dotnet add tests/YARL.Tests package Avalonia.Themes.Fluent
dotnet add tests/YARL.Tests package NSubstitute

# EF Core CLI tool (global, once per dev machine)
dotnet tool install --global dotnet-ef
```

---

## Architecture Patterns

### Recommended Project Structure (single-project, folder-based — locked decision)

```
src/YARL/
├── Program.cs                      # Entry point, Generic Host setup
├── App.axaml / App.axaml.cs        # Avalonia App, DI composition root
├── Domain/
│   ├── Models/
│   │   └── RomSource.cs            # RomSource entity (path, SourceType, label)
│   ├── Interfaces/
│   │   ├── IRomSourceProvider.cs   # LIB-08: provider contract
│   │   └── IPathResolver.cs        # Resolves XDG data paths
│   └── Enums/
│       └── SourceType.cs           # Local | OsMounted
├── Infrastructure/
│   ├── Persistence/
│   │   ├── YarlDbContext.cs         # EF Core DbContext
│   │   └── Migrations/             # EF Core migration files (auto-generated)
│   ├── Providers/
│   │   ├── LocalRomSourceProvider.cs
│   │   └── OsMountedRomSourceProvider.cs
│   └── Config/
│       ├── AppConfig.cs            # JSON config POCO
│       └── AppConfigService.cs     # Read/write JSON config
├── Application/
│   └── Library/
│       └── LibraryService.cs       # Stub — Phase 2 will fill this
├── UI/
│   ├── Desktop/
│   │   ├── DesktopShell.axaml      # Stub shell for desktop mode
│   │   └── DesktopShell.axaml.cs
│   ├── Fullscreen/
│   │   ├── FullscreenShell.axaml   # Stub shell for fullscreen/gamepad mode
│   │   └── FullscreenShell.axaml.cs
│   └── ViewModels/
│       ├── MainViewModel.cs        # Implements IScreen, owns RoutingState
│       └── LibraryViewModel.cs     # Shared by both shells
├── Data/
│   └── platforms.json              # Default platform definitions (shipped)
tests/YARL.Tests/
├── TestApp.cs                      # Headless app builder
├── Foundation/
│   ├── RomSourceProviderTests.cs
│   └── MigrationTests.cs
└── YARL.Tests.csproj
YARL.sln
```

### Pattern 1: ReactiveUI Dual-Shell Setup

**What:** MainViewModel implements IScreen. Program.cs builds the Generic Host. App.axaml.cs resolves MainViewModel from DI, inspects UIMode (gamepad detected or config override), then sets the Window content to DesktopShell or FullscreenShell. Both shells bind to the same MainViewModel / LibraryViewModel.

**When to use:** This is the startup bootstrapping pattern — every subsequent phase depends on this being correct.

**Example:**
```csharp
// Program.cs
var host = Host.CreateDefaultBuilder()
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<IScreen>(sp => sp.GetRequiredService<MainViewModel>());
        services.AddTransient<IRomSourceProvider, LocalRomSourceProvider>();
        services.AddTransient<IRomSourceProvider, OsMountedRomSourceProvider>();
        services.AddDbContext<YarlDbContext>(opts =>
            opts.UseSqlite($"Data Source={AppPaths.DatabasePath}"));
    })
    .Build();

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseReactiveUIWithMicrosoftDependencyResolver(host.Services)
    .StartWithClassicDesktopLifetime(args);
```

```csharp
// App.axaml.cs — OnFrameworkInitializationCompleted
var mainVm = Locator.Current.GetRequiredService<MainViewModel>();
var uiMode = DetermineStartupMode();  // gamepad check + config override
MainWindow = uiMode == UIMode.Fullscreen
    ? new FullscreenShell { DataContext = mainVm }
    : new DesktopShell { DataContext = mainVm };
MainWindow.Show();
```

**Source:** [ReactiveUI Avalonia docs](https://www.reactiveui.net/docs/getting-started/installation/avalonia), [ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection NuGet](https://www.nuget.org/packages/ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection/11.3.0)

### Pattern 2: IRomSourceProvider for LIB-08

**What:** Define the provider interface with a SourceType discriminator in Phase 1. Phase 2 fills in the implementation bodies. This satisfies LIB-08 (user can tag a path as OS-mounted) at the architectural level before features are needed.

**When to use:** Interface + stub implementations go in Phase 1. Real scanning implementation goes in Phase 2.

**Example:**
```csharp
// Domain/Enums/SourceType.cs
public enum SourceType { Local, OsMounted }

// Domain/Models/RomSource.cs
public class RomSource
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public SourceType SourceType { get; set; }
    public string? Label { get; set; }       // user-friendly name
    public bool IsEnabled { get; set; } = true;
}

// Domain/Interfaces/IRomSourceProvider.cs
public interface IRomSourceProvider
{
    SourceType SupportedType { get; }
    bool CanHandle(RomSource source);
    IAsyncEnumerable<string> EnumerateRomsAsync(RomSource source, CancellationToken ct);
}
```

**Source:** Architecture research (ARCHITECTURE.md) — provider pattern; LIB-08 requirement

### Pattern 3: EF Core Migration Lifecycle

**What:** Run `context.Database.MigrateAsync()` at startup — never `EnsureCreated()`. MigrateAsync applies any pending migrations in order, creating the database on first run and upgrading it on subsequent runs. The `__EFMigrationsHistory` table tracks what has run.

**When to use:** Every app startup, before any other database access.

**Example:**
```csharp
// In App startup or a hosted service
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
await db.Database.MigrateAsync();
```

**EF Core 10 note:** When the project targets multiple frameworks, `dotnet ef migrations add` requires `--framework net10.0`. Since this project targets only net10.0, this is not an issue, but document it for CI scripts.

**Source:** [Microsoft.EntityFrameworkCore.Sqlite 10.0.5 NuGet](https://www.nuget.org/packages/microsoft.entityframeworkcore.sqlite), [EF Core Applying Migrations docs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

### Pattern 4: XDG-Compliant Path Resolution

**What:** Use `Xdg.Directories` to resolve data paths. This automatically returns `~/.local/share/yarl/` on Linux (respecting XDG_DATA_HOME override) and `%AppData%\YARL\` on Windows.

**When to use:** Anywhere a data path is needed. Never hardcode paths. Never use `Directory.GetCurrentDirectory()`.

**Example:**
```csharp
// Infrastructure/Config/AppPaths.cs
public static class AppPaths
{
    private static readonly string DataDir =
        Path.Combine(BaseDirectory.DataHome, "yarl");

    public static string DatabasePath => Path.Combine(DataDir, "library.db");
    public static string ConfigPath    => Path.Combine(DataDir, "config.json");
    public static string ArtCacheDir   => Path.Combine(DataDir, "art");

    static AppPaths()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ArtCacheDir);
    }
}
```

**Source:** [Xdg.Directories NuGet](https://www.nuget.org/packages/Xdg.Directories), [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir/basedir-spec-latest.html)

### Pattern 5: Gamepad Detection for Startup Mode

**What:** Check for any connected gamepad using `Avalonia.Input.GamepadDevice` or a simple SDL2/libinput check at launch. Keep this simple — just check if any gamepad is present. The config override (forced desktop/fullscreen) takes priority over auto-detection.

**When to use:** Only at startup, before the window is shown.

**Example:**
```csharp
private UIMode DetermineStartupMode()
{
    // Config override takes priority
    if (_config.UiModeOverride == UIMode.Desktop) return UIMode.Desktop;
    if (_config.UiModeOverride == UIMode.Fullscreen) return UIMode.Fullscreen;

    // Auto-detect: any gamepad connected?
    bool hasGamepad = GamepadDetector.IsAnyGamepadConnected();
    return hasGamepad ? UIMode.Fullscreen : UIMode.Desktop;
}
```

**Source:** Architecture decision in CONTEXT.md; keep simple per the "Specific Ideas" note

### Pattern 6: Avalonia Headless Test Setup

**What:** Use `[AvaloniaFact]` instead of `[Fact]` for any test that touches Avalonia controls. Configure via `[assembly: AvaloniaTestApplication]` attribute pointing to a `TestAppBuilder` class.

**Example:**
```csharp
// tests/YARL.Tests/TestApp.cs
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// Usage in tests
public class ShellSwitchingTests
{
    [AvaloniaFact]
    public void DesktopShell_UsesSharedLibraryViewModel()
    {
        var shell = new DesktopShell();
        // assertions...
    }
}
```

**Source:** [Avalonia Headless Testing with xUnit (official docs)](https://docs.avaloniaui.net/docs/testing/headless-xunit)

### Pattern 7: GitHub Actions Cross-Platform Matrix

**What:** Single workflow with OS matrix: `[ubuntu-latest, windows-latest]`. Both must pass before merge. Linux runner is the primary CI gate (catches Avalonia Linux issues early).

**Example:**
```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --verbosity normal
```

**Source:** [GitHub Actions building and testing .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net), [actions/setup-dotnet](https://github.com/actions/setup-dotnet)

### Anti-Patterns to Avoid

- **EnsureCreated() instead of MigrateAsync():** EnsureCreated creates the schema once but never applies subsequent migrations. Once a user has an existing database, new schema changes silently fail to apply. Always use `MigrateAsync()`.
- **Splat locator without MS DI bridge:** Registering services directly via `Locator.CurrentMutable.Register()` works initially but creates a parallel DI system that fights with Generic Host. Use `UseReactiveUIWithMicrosoftDependencyResolver()` from the start.
- **Hardcoding %AppData% or ~/.config:** Hardcoded paths ignore user overrides and platform conventions. Use `Xdg.Directories` or `Environment.SpecialFolder.LocalApplicationData`.
- **Single adaptive shell instead of dual shells:** Attempting to show/hide panels in one view tree for mode-switching leads to focus management failures with controller input. The locked decision (two separate shells) is architecturally correct.
- **Avalonia.ReactiveUI without the MS.DI bridge package:** `Avalonia.ReactiveUI` (from the AvaloniaUI org, version 11.3.8) is a different and older package from `ReactiveUI.Avalonia` (from the ReactiveUI org, version 11.4.12). Use `ReactiveUI.Avalonia` + the DI bridge.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| XDG path resolution | Custom platform switch + hardcoded paths | `Xdg.Directories` 0.1.2 | XDG spec has edge cases (env var overrides, directory creation); tiny library handles it correctly |
| Migration framework | `CREATE TABLE IF NOT EXISTS` + PRAGMA user_version | EF Core migrations | SQLite has limited ALTER TABLE; EF migrations handle column changes, indexes, data transforms correctly |
| ReactiveUI + MS DI bridge | Manual `Locator.CurrentMutable.Register()` for every service | `UseReactiveUIWithMicrosoftDependencyResolver()` | Manual registration creates two separate service graphs; the extension syncs them |
| Headless CI test setup | Custom X11 virtual framebuffer setup | `Avalonia.Headless.XUnit` | Headless platform runs without any display server; no Xvfb needed on GitHub Actions Linux |
| CRC32 hashing | Pure-managed CRC32 loop | `Force.Crc32` (Phase 2) | Not needed in Phase 1, but don't implement manually in Phase 2 — hardware-accelerated via SSE4.2 |

**Key insight:** The biggest hand-roll risk in this phase is migration management. Many greenfield .NET SQLite apps start with raw SQL schema creation, then discover they cannot safely change the schema without breaking existing user databases. EF Core migrations must be the very first database artifact committed.

---

## Common Pitfalls

### Pitfall 1: Avalonia.ReactiveUI vs ReactiveUI.Avalonia Package Confusion

**What goes wrong:** The Avalonia template (`dotnet new avalonia.app`) installs `Avalonia.ReactiveUI` (published by AvaloniaUI org, version matches Avalonia 11.3.x). The newer `ReactiveUI.Avalonia` (published by ReactiveUI org, version 11.4.x) provides the Microsoft DI bridge. These are two different packages with overlapping functionality. Installing both without understanding the relationship causes double initialization.

**Why it happens:** Search results and documentation reference both packages. The NuGet names are confusingly similar.

**How to avoid:** Use `ReactiveUI.Avalonia` + `ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection`. The AppBuilder extension `.UseReactiveUI()` (from Avalonia.ReactiveUI) must not be called alongside `.UseReactiveUIWithMicrosoftDependencyResolver()` — use only the DI bridge version to avoid double-initialization.

**Warning signs:** Two calls to `.UseReactiveUI*()` in Program.cs; Splat resolver throwing "already registered" exceptions at startup.

### Pitfall 2: Linux CI Skipped Until "It Works on Windows"

**What goes wrong:** Phase 1 develops on Windows, Avalonia CI is "TODO," and Linux-specific rendering/font/fullscreen bugs are discovered in Phase 5 when the UI is fully built — costing 1-2 sprint regressions to fix.

**Why it happens:** Linux CI requires understanding that Avalonia.Headless.XUnit works without a display server. Developers assume "it'll need Xvfb" and defer.

**How to avoid:** Create the GitHub Actions workflow in the first commit. Avalonia.Headless.XUnit runs on `ubuntu-latest` with no display server configuration needed. Linux CI must be green before any UI code is merged.

**Warning signs:** CI workflow file absent or Linux runner commented out in Phase 1.

### Pitfall 3: EF Core Migration Lock on SQLite (EF Core 9+)

**What goes wrong:** In EF Core 9+, a `__EFMigrationsLock` table is created when migrations run. If two app instances start simultaneously (unlikely for desktop, but possible in tests), migrations deadlock. More practically: tests that create/drop the database in parallel can fail due to lock table state.

**Why it happens:** New EF9+ behavior for SQLite; SQLite has no native locking.

**How to avoid:** Use an in-memory SQLite database for unit tests (`options.UseSqlite("Data Source=:memory:")`). Each test gets its own in-memory instance. Never run migrations in parallel against a shared file-based test database.

**Warning signs:** Intermittent test failures on "table __EFMigrationsLock already exists."

### Pitfall 4: Gamepad Detection Using Wrong API

**What goes wrong:** Using SDL2 or raw HID to detect gamepads at startup breaks on Steam Deck where Steam already owns the gamepad. On Linux, some gamepad input goes through `/dev/input/js*` devices which may not be accessible without udev rules.

**Why it happens:** Platform-specific gamepad detection is non-trivial and varies by OS/compositor.

**How to avoid:** Use the simplest possible check: scan `/dev/input/js*` on Linux or check `GetRawInputDeviceList` on Windows. If detection fails or throws, default to desktop mode (fail safe). Never crash on gamepad detection failure. Keep detection code behind a try/catch.

**Warning signs:** App crashes on startup on Linux when no gamepad is present; gamepad detection blocks startup for > 100ms.

### Pitfall 5: MigrateAsync() Called Without Ensuring Data Directory Exists

**What goes wrong:** `MigrateAsync()` succeeds on Windows (AppData exists) but throws on Linux because `~/.local/share/yarl/` does not exist yet. The SQLite connection string points to a non-existent directory.

**Why it happens:** SQLite does not create parent directories — it expects the directory to exist.

**How to avoid:** Always call `Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!)` before opening the SQLite connection. The `AppPaths` static initializer pattern above handles this automatically.

---

## Code Examples

Verified patterns from official sources:

### MainViewModel (IScreen implementation)

```csharp
// Source: https://www.reactiveui.net/docs/getting-started/installation/avalonia
public class MainViewModel : ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}
```

### ReactiveUI + MS DI wiring (AppBuilder)

```csharp
// Source: ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection 11.4.12
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .LogToTrace()
    .UseReactiveUIWithMicrosoftDependencyResolver(services =>
    {
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IScreen>(sp => sp.GetRequiredService<MainViewModel>());
        services.AddSingleton<LibraryViewModel>();
        // Phase 2+ services registered here
    })
    .StartWithClassicDesktopLifetime(args);
```

### EF Core DbContext (Phase 1 minimal schema)

```csharp
// Source: EF Core 10 patterns
public class YarlDbContext : DbContext
{
    public DbSet<RomSource> RomSources => Set<RomSource>();
    // Roms table stub — Phase 2 will expand this
    // public DbSet<RomEntry> Roms => Set<RomEntry>();

    public YarlDbContext(DbContextOptions<YarlDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RomSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Path).IsRequired();
            e.Property(x => x.SourceType).HasConversion<string>();
        });
    }
}
```

### Headless test (xUnit)

```csharp
// Source: https://docs.avaloniaui.net/docs/testing/headless-xunit
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class DesktopShellTests
{
    [AvaloniaFact]
    public void DesktopShell_RendersWithoutException()
    {
        var shell = new DesktopShell();
        shell.Show();
        Assert.True(shell.IsVisible);
    }
}
```

### EF Core migration commands

```bash
# Create the first migration
dotnet ef migrations add InitialCreate --project src/YARL --output-dir Domain/Persistence/Migrations

# Verify migration SQL (do not apply yet)
dotnet ef migrations script --project src/YARL

# Apply migrations (test startup path)
dotnet ef database update --project src/YARL
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Avalonia.ReactiveUI (AvaloniaUI org) | ReactiveUI.Avalonia (ReactiveUI org) + MS DI bridge | 2024-2025 | Cleaner DI integration; single service graph |
| Splat.RegisterConstant for all services | UseReactiveUIWithMicrosoftDependencyResolver() | 2023-2024 | Generic Host compatibility; standard IServiceCollection |
| EnsureCreated() for SQLite schema | Database.MigrateAsync() + migration files | Always correct, but often ignored | Safe schema upgrades for installed apps |
| Hardcoded ~/. paths on Linux | Xdg.Directories library | Ongoing best practice | Honors XDG_DATA_HOME override (power user scenarios) |
| EF Core 9 migration locking | EF Core 10.0.5 migration locking (__EFMigrationsLock table) | EF Core 9 | Explicit, documented; affects test setup |

**Avalonia 12 preview status:** Avalonia 12.0.0-preview1 was published 2/17/2026. Do NOT use this for Phase 1 — it is pre-release and APIs may break. Stay on 11.3.12 stable.

---

## Open Questions

1. **Exact gamepad detection API for startup mode**
   - What we know: SDL2 and raw HID work but are complex; Avalonia itself may expose gamepad device info via `Input.GamepadDevice`
   - What's unclear: Whether Avalonia 11.3.x exposes a synchronous gamepad enumeration API usable before the main loop starts
   - Recommendation: Implement using direct `/dev/input/js*` glob on Linux, `RawInput` on Windows. Keep in a try/catch. Default to desktop mode on any failure. This is deliberately simple per CONTEXT.md "Specific Ideas."

2. **platforms.json schema and default content**
   - What we know: Must ship with the app; user-extensible; not a hardcoded enum
   - What's unclear: Exact schema fields (name, extensions[], defaultEmulator, retroarchCoreId?)
   - Recommendation: Define a minimal PlatformDefinition schema in Phase 1 with the fields Phase 2 will need (id, name, extensions, retroarchCoreId). Phase 4 (Emulator) will extend it.

3. **config.json schema scope for Phase 1**
   - What we know: Settings in JSON, not SQLite; ROM cache directory must be configurable
   - What's unclear: Full set of Phase 1 settings fields
   - Recommendation: Phase 1 config schema needs: `uiModeOverride` (null | "desktop" | "fullscreen"), `romCacheDir` (nullable string — null means auto in data dir), `romSources` (list of paths, deferred to Phase 2 UI). Keep it minimal.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.x + Avalonia.Headless.XUnit 11.3.x |
| Config file | None — xUnit auto-discovers; no xunit.runner.json needed initially |
| Quick run command | `dotnet test tests/YARL.Tests --filter "Category!=Integration" --no-build` |
| Full suite command | `dotnet test tests/YARL.Tests --no-build --verbosity normal` |

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LIB-08 | RomSource can have SourceType = OsMounted and be stored/retrieved via DB | Unit | `dotnet test tests/YARL.Tests --filter "FullyQualifiedName~RomSourceTests" --no-build` | Wave 0 |
| (Phase SC-1) | App launches: DesktopShell renders without exception | Headless UI | `dotnet test tests/YARL.Tests --filter "FullyQualifiedName~DesktopShellTests" --no-build` | Wave 0 |
| (Phase SC-2) | EF Core migration runs cleanly on first launch (in-memory SQLite) | Integration | `dotnet test tests/YARL.Tests --filter "FullyQualifiedName~MigrationTests" --no-build` | Wave 0 |
| (Phase SC-3) | FullscreenShell stub renders without exception | Headless UI | `dotnet test tests/YARL.Tests --filter "FullyQualifiedName~FullscreenShellTests" --no-build` | Wave 0 |
| (Phase SC-5) | Linux CI pipeline passes | CI smoke | GitHub Actions `ubuntu-latest` runner (not local) | Wave 0 |

Note: SC = Success Criteria from the phase spec. LIB-08 is the only formal requirement ID; the others are success criteria tests.

### Sampling Rate

- **Per task commit:** `dotnet test tests/YARL.Tests --filter "Category!=Slow" --no-build`
- **Per wave merge:** `dotnet test tests/YARL.Tests --no-build --verbosity normal`
- **Phase gate:** Full suite green on both Windows and Linux runners before `/gsd:verify-work`

### Wave 0 Gaps (test infrastructure needed before implementation)

- [ ] `tests/YARL.Tests/TestApp.cs` — Headless AppBuilder for `[AvaloniaFact]` tests
- [ ] `tests/YARL.Tests/Foundation/RomSourceTests.cs` — covers LIB-08 (SourceType storage + retrieval)
- [ ] `tests/YARL.Tests/Foundation/MigrationTests.cs` — in-memory SQLite migration smoke test
- [ ] `tests/YARL.Tests/Foundation/DesktopShellTests.cs` — headless render test
- [ ] `tests/YARL.Tests/Foundation/FullscreenShellTests.cs` — headless render test
- [ ] `.github/workflows/ci.yml` — matrix workflow (ubuntu-latest + windows-latest)
- [ ] Framework install: `dotnet add tests/YARL.Tests package Avalonia.Headless.XUnit`

---

## Sources

### Primary (HIGH confidence)

- [NuGet: Avalonia 11.3.12](https://www.nuget.org/packages/avalonia) — version verified March 2026
- [NuGet: ReactiveUI.Avalonia 11.4.12](https://www.nuget.org/packages/ReactiveUI.Avalonia/) — version verified March 2026
- [NuGet: ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection 11.4.12](https://www.nuget.org/packages/ReactiveUI.Avalonia.Microsoft.Extensions.DependencyInjection/11.3.0) — version verified March 2026
- [NuGet: Microsoft.EntityFrameworkCore 10.0.5](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.5) — verified March 2026
- [NuGet: Microsoft.EntityFrameworkCore.Sqlite 10.0.5](https://www.nuget.org/packages/microsoft.entityframeworkcore.sqlite) — verified March 2026
- [Avalonia Headless XUnit Testing (official docs)](https://docs.avaloniaui.net/docs/testing/headless-xunit) — packages, setup, [AvaloniaFact] attribute confirmed
- [ReactiveUI Avalonia installation (official docs)](https://www.reactiveui.net/docs/getting-started/installation/avalonia) — IScreen/RoutingState pattern, UseReactiveUI DI wiring
- [GitHub Actions: Building and testing .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net) — matrix strategy, actions/setup-dotnet@v4
- [EF Core: Applying Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) — MigrateAsync pattern
- [NuGet: Xdg.Directories 0.1.2](https://www.nuget.org/packages/Xdg.Directories) — XDG path resolution

### Secondary (MEDIUM confidence)

- [Splat + MS DI README (GitHub)](https://github.com/reactiveui/splat/blob/main/src/Splat.Microsoft.Extensions.DependencyInjection/README.md) — UseMicrosoftDependencyResolver pattern
- [Avalonia + Generic Host discussion #5241](https://github.com/AvaloniaUI/Avalonia/issues/5241) — known integration pattern; community-verified
- [Avalonia GitHub Actions cross-platform practices](https://www.zettlr.com/post/continuous-cross-platform-deployment-github-actions) — OS matrix strategy

### Tertiary (LOW confidence — flag for validation)

- Gamepad detection API (`/dev/input/js*` on Linux, RawInput on Windows) — training data, not verified against current Avalonia 11.3 InputManager API
- Steam Deck Gamescope behavior with `WindowState.FullScreen` — training data; verify in Phase 6

---

## Metadata

**Confidence breakdown:**
- Standard stack versions: HIGH — all key packages version-verified against NuGet March 2026
- ReactiveUI + MS DI bridge integration: HIGH — official package confirmed, pattern documented from official ReactiveUI docs
- EF Core 10 + SQLite migrations: HIGH — version verified, MigrationLock behavior documented in official EF10 breaking changes
- GitHub Actions matrix: HIGH — official GitHub Docs
- Headless test setup: HIGH — official Avalonia docs fetched and confirmed
- Gamepad detection implementation: LOW — API specifics not verified against Avalonia 11.3 InputManager
- XDG path library: MEDIUM — package exists and is documented; API surface small enough to be low-risk

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (30 days — Avalonia/ReactiveUI release cadence is ~monthly; re-verify versions if planning is delayed)
