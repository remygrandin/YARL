# Stack Research

**Domain:** Cross-platform ROM browser/manager/launcher (.NET + Avalonia UI)
**Researched:** 2026-03-18
**Overall Confidence:** MEDIUM — .NET runtime version confirmed via official Microsoft docs. Avalonia version is estimated from training data (cutoff August 2025) plus known 11.x release trajectory; exact latest minor version needs verification at project start. All NuGet package minor versions flagged where unverified.

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET | 10.0 LTS (10.0.5) | Runtime and SDK | Confirmed current LTS as of March 2026. Use LTS over 9.0 STS for a long-lived desktop app — 3 years of support vs running out in Nov 2026. C# 14 included. |
| Avalonia UI | 11.x (verify latest at project start) | Cross-platform UI framework | The only mature .NET UI framework with first-class Linux support. WPF is Windows-only. MAUI has poor Linux story. Avalonia 11.x (stable since 2023) added a major rendering rework (Composition renderer) and significantly improved gamepad/input handling. Ships with MVVM toolkit support and control themes. Confidence: HIGH on choice, LOW on exact version. |
| C# | 14.0 | Primary language | Included with .NET 10. Use primary constructors, collection expressions, and pattern matching for cleaner code. |
| ReactiveUI | 20.x (verify at project start) | MVVM framework | The standard MVVM library for Avalonia. Avalonia's own templates default to ReactiveUI. Provides reactive property bindings, routing, and interaction patterns that map well to both desktop and fullscreen controller modes. Alternative: CommunityToolkit.Mvvm — see notes below. Confidence: HIGH on choice, LOW on exact version. |

### UI / Presentation Layer

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Avalonia.ReactiveUI | matches ReactiveUI | Avalonia-specific ReactiveUI adapters (ReactiveWindow, ReactiveUserControl) | Use whenever a view needs ViewModel binding — essentially always |
| Semi.Avalonia or FluentAvalonia | latest stable | Control theme / design system | Semi.Avalonia provides a clean modern theme for custom UIs; FluentAvalonia brings WinUI3 Fluent controls (NavigationView, NumberBox) which suit a grid-style launcher UI. **Recommendation: FluentAvalonia** — NavigationView maps directly to the sidebar+content layout needed. Confidence: MEDIUM |
| Avalonia.Controls.ItemsRepeater | built-in (Avalonia 11+) | Virtualized grid/list of ROM tiles | Use ItemsRepeater with a WrapLayout or UniformGridLayout for the cover art grid. Critical for large collections (10K+ ROMs) — without virtualization, performance degrades badly. Confidence: HIGH |

### Database

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| SQLite (via Microsoft.Data.Sqlite) | 9.x (bundled with .NET) | Local ROM catalog database | SQLite is the correct choice for a single-user desktop app. Zero server overhead, single-file DB, ships cross-platform natively in .NET. Confidence: HIGH |
| Entity Framework Core (EF Core) | 9.x / 10.x (target-framework matched) | ORM over SQLite | Use EF Core with SQLite provider for schema migrations, LINQ queries, and change tracking. Migrations are essential for a desktop app that upgrades in-place without a server. Alternative to raw Dapper/sqlite-net: EF Core is heavier but the migration story alone justifies it. Confidence: HIGH |
| Microsoft.EntityFrameworkCore.Sqlite | matches EF Core | SQLite provider for EF Core | Pairs with EF Core; includes SQLite-specific configuration options |

### Metadata & Scraping

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Net.Http.HttpClient (built-in) | .NET 10 BCL | HTTP client for API calls | Use with typed HttpClient registered in DI. Do NOT use WebClient or HttpWebRequest — both are legacy. |
| Polly | 8.x | Retry/resilience for HTTP calls | ScreenScraper and IGDB both have rate limits and occasional failures. Polly provides exponential backoff, circuit breakers. Register via Microsoft.Extensions.Http.Polly. Confidence: HIGH |
| System.Text.Json (built-in) | .NET 10 BCL | JSON deserialization of API responses | Prefer over Newtonsoft.Json for new projects — faster, lower allocation, source-generator support. Only switch to Newtonsoft if you encounter a complex polymorphism case System.Text.Json can't handle. |
| ScreenScraper API | REST v2 | Primary ROM metadata source | Hash-based ROM lookup (MD5/SHA1/CRC32) returns exact game match even for regional variants. Covers retro platforms comprehensively. Requires free account; rate-limited for non-devs. **Use as primary source.** No official .NET SDK — implement a thin typed client. Confidence: HIGH on choice, MEDIUM on API stability |
| IGDB API (via Twitch/IGDB) | REST v4 | Secondary/supplemental metadata | Broader modern coverage, better for post-2000 games. Requires Twitch OAuth client credentials. Use as fallback when ScreenScraper lacks a game. Confidence: MEDIUM |
| TheGamesDB API | REST | Tertiary fallback | Open, free, no OAuth. Smaller coverage. Use as last resort. Confidence: MEDIUM |

### ROM Processing

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.IO (built-in) | .NET 10 BCL | File system scanning | Use FileSystemWatcher + Channel<T> for background scanning pipeline. Use Directory.EnumerateFiles (not GetFiles) — enumerates lazily, avoids large array allocation on deep trees. |
| SharpCompress | 0.36+ | Archive inspection (zip/7z/rar ROM containers) | Many ROMs are stored in .zip or .7z. Read archive entries without full extraction to compute hashes or list contents. Confidence: HIGH |
| System.Security.Cryptography (built-in) | .NET 10 BCL | MD5/SHA1/CRC32 hashing for ScreenScraper | Use for computing ROM hashes to submit to ScreenScraper. Note: CRC32 is not in BCL — use a small standalone impl or SharpCompress's built-in CRC. |
| Force.Crc32 | 0.9+ | CRC32 hardware-accelerated hashing | ScreenScraper uses CRC32 as its primary hash. Force.Crc32 uses SSE4.2 intrinsics — significantly faster than pure-managed for large ROM files. Confidence: HIGH on choice, LOW on exact version |

### Emulator Integration

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Diagnostics.Process (built-in) | .NET 10 BCL | Launch emulator processes | Use Process.Start with ProcessStartInfo. Set UseShellExecute = false, RedirectStandardError = true for error capture. Always use full executable path — don't rely on PATH on Linux Steam Deck. |
| Microsoft.Extensions.DependencyInjection | 10.x | DI container | Register emulator launcher strategy per emulator type. Pattern: IEmulatorLauncher per emulator, factory resolves by platform/emulator key. |

### Background Processing / Async

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Threading.Channels (built-in) | .NET 10 BCL | Producer/consumer pipeline for ROM scanning | Use Channel<RomScanResult> for background scanner → UI update pipeline. Bounded channels prevent memory runaway on large libraries. Confidence: HIGH |
| Microsoft.Extensions.Hosting | 10.x | BackgroundService host for scanner | Use IHostedService / BackgroundService for the ROM scanner. Allows clean start/stop, DI injection, and cancellation. Confidence: HIGH |

### Image / Cache

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| SkiaSharp (via Avalonia's rendering) | bundled with Avalonia | Image rendering | Do not add directly unless doing custom image processing. Avalonia's Bitmap and Image controls handle display. |
| ImageSharp (SixLabors.ImageSharp) | 3.x | Server-side image resize/thumbnail generation | Use only if you need to pre-process/thumbnail cover art before storing. Not required if displaying at native resolution via Avalonia controls. Confidence: MEDIUM |

### Dependency Injection / App Host

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.DependencyInjection | 10.x | DI container | Use the standard Microsoft DI container. Avalonia integrates cleanly with it via AppBuilder. No need for Autofac or other third-party containers at this scale. |
| Microsoft.Extensions.Logging | 10.x | Structured logging | Use with Serilog sink (see below) for output. |
| Serilog | 4.x | Structured file logging | Write to rolling file sink for diagnostics. Critical for diagnosing emulator launch failures on users' machines. Confidence: HIGH |
| Serilog.Sinks.File | 5.x | File sink for Serilog | Use with RollingInterval.Day, retained 7 days |

### Testing

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.x | Test framework | Standard .NET test framework. Avalonia community uses xUnit. |
| Moq or NSubstitute | 4.x / 5.x | Mocking for unit tests | Mock IEmulatorLauncher, IMetadataService, etc. NSubstitute is more ergonomic for record/class mocks. |
| Avalonia.Headless | matches Avalonia | UI testing without display | Allows running Avalonia controls in tests on CI (Linux, no X11 required). Essential for Steam Deck CI pipelines. Confidence: HIGH |

---

## Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| JetBrains Rider or VS 2022 | IDE | Rider has better Linux dev support and Avalonia XAML preview. VS 2022 has the Avalonia extension for XAML designer. Either works; Rider preferred for cross-platform dev workflow. |
| Avalonia Templates (`dotnet new avalonia.app`) | Project scaffolding | Install via: `dotnet new install Avalonia.Templates`. Creates MVVM project with ReactiveUI wired up. |
| dotnet-ef (EF Core CLI) | Database migrations | Install globally: `dotnet tool install --global dotnet-ef`. Used for `dotnet ef migrations add` / `dotnet ef database update`. |
| Velopack or dotnet publish (self-contained) | Distribution | For Steam Deck: publish as self-contained Linux x64. For Windows: self-contained or Velopack for auto-update. |

---

## Installation (Project Bootstrap)

```bash
# Install Avalonia project templates
dotnet new install Avalonia.Templates

# Create MVVM project
dotnet new avalonia.app.mvvm -n YARL -o src/YARL

# Core NuGet packages (run from project directory)
dotnet add package Avalonia
dotnet add package Avalonia.Desktop
dotnet add package Avalonia.ReactiveUI
dotnet add package ReactiveUI
dotnet add package FluentAvalonia

# Database
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# Resilience
dotnet add package Polly
dotnet add package Microsoft.Extensions.Http.Polly

# Archive/hashing
dotnet add package SharpCompress
dotnet add package Force.Crc32

# Logging
dotnet add package Serilog
dotnet add package Serilog.Extensions.Hosting
dotnet add package Serilog.Sinks.File

# App host
dotnet add package Microsoft.Extensions.Hosting

# Dev/test
dotnet add package xunit --project tests/YARL.Tests
dotnet add package NSubstitute --project tests/YARL.Tests
dotnet add package Avalonia.Headless.XUnit --project tests/YARL.Tests

# EF Core CLI tool
dotnet tool install --global dotnet-ef
```

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Avalonia UI | .NET MAUI | Never for this project — MAUI has no Linux desktop target as of 2025. MAUI targets Android/iOS/Windows/Mac but Linux is community-only and not production-ready. |
| Avalonia UI | WPF | Never — Windows-only. Steam Deck requirement alone rules this out. |
| ReactiveUI | CommunityToolkit.Mvvm | If the team has no Rx background and finds ReactiveUI's Rx patterns confusing. CommunityToolkit is simpler but lacks ReactiveUI's IScreen/routing system which is useful for full-screen gamepad navigation mode switching. |
| EF Core + SQLite | LiteDB | LiteDB is a document DB (no schema). Use if you want schema-free storage and don't care about migrations. EF Core is better here because structured ROM metadata maps cleanly to relational tables and migrations are critical for in-place upgrades. |
| EF Core + SQLite | Dapper + raw SQL | If EF Core's performance is a bottleneck in bulk scan inserts. Dapper is faster for bulk inserts. Could use Dapper for the scanner write path and EF Core for read/query. Hybrid is valid but adds complexity. |
| System.Text.Json | Newtonsoft.Json | Only if you need complex polymorphic JSON that System.Text.Json can't handle, or are consuming a badly-formed API that Newtonsoft handles more leniently. |
| Polly | Custom retry loops | Never — Polly is the standard; custom retry logic is error-prone. |
| ScreenScraper (primary) | IGDB (primary) | If your audience skews toward modern/PC games rather than retro ROMs. IGDB covers Steam/modern releases better. For a ROM-focused launcher, ScreenScraper's hash lookup is more accurate for retro platforms. |
| Microsoft.Extensions.Hosting | Plain BackgroundThread | Never — BackgroundService provides structured cancellation, DI, and lifecycle that hand-rolled threads lack. |
| Velopack | ClickOnce / MSIX | MSIX is Windows Store only. ClickOnce is legacy. Velopack supports both Windows and Linux updates with a clean API. Only needed when auto-update feature is built. |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| WPF | Windows-only, no Steam Deck | Avalonia UI |
| .NET MAUI | No production Linux desktop target | Avalonia UI |
| HttpWebRequest / WebClient | Deprecated in .NET 5+, no async, poor performance | HttpClient (typed, DI-registered) |
| Newtonsoft.Json (by default) | Slower, higher allocation than System.Text.Json; no source-generator support | System.Text.Json |
| Application.DoEvents() / Thread.Sleep in UI | Blocks Avalonia's Dispatcher; causes UI freeze | async/await + Dispatcher.UIThread.InvokeAsync |
| GetFiles() for large directory trees | Allocates full array before returning | Directory.EnumerateFiles() (lazy enumeration) |
| Direct Process.Kill() without WaitForExit | Leaves zombie processes, particularly on Linux | Process.Kill(); process.WaitForExitAsync(ct) |
| Bundling emulator binaries | Legal and licensing minefield (many emulators are GPL) | Download/install at runtime from official sources, or document manual install |
| LiteDB | No migration story; schema changes break existing databases | EF Core with SQLite |
| Full-text SQLite FTS without index | Performance collapses at 10K+ ROMs | Create FTS5 virtual table for title/description search from the start |
| Storing cover art as BLOBs in SQLite | Database becomes enormous; no streaming | Store as files on disk in app data dir, store path in DB |

---

## Stack Patterns by Variant

**For the Desktop (Grid) mode:**
- Use Avalonia ItemsRepeater with WrapLayout for the cover art grid
- Virtualization is non-negotiable at scale — do not use ItemsControl with no virtualization
- Load cover art asynchronously with AsyncImage pattern (Avalonia has no built-in async image loading; implement via binding to Task<Bitmap>)

**For the Fullscreen/Gamepad mode:**
- Use a separate top-level Window or Avalonia's FullScreen mode
- ReactiveUI's IScreen/RoutingState maps navigation stack to controller Back button
- Map gamepad axes to keyboard-equivalent navigation via Avalonia's InputManager or Steam Input API
- On Steam Deck: register as a non-Steam game and rely on Steam Input for controller mapping; do not implement raw HID input

**For ROM scanning at scale:**
- Use Channel<T> (bounded, capacity ~500) to stream scan results to UI
- Compute hashes lazily — only hash when metadata scraping is triggered, not during initial scan
- Store scan state in DB so interrupted scans resume rather than restart

**For emulator launching (cross-platform):**
- Implement IEmulatorLauncher interface per emulator family (RetroArch, Standalone, Wine-wrapped)
- RetroArch: launch with `-L /path/to/core.so rompath` — single binary, many cores
- Standalone emulators (Dolphin, PCSX2, Duckstation): launch with `--no-gui --elf rompath` or emulator-specific flags — document per emulator
- On Linux/Steam Deck: check XDG_RUNTIME_DIR and WAYLAND_DISPLAY vs DISPLAY to detect Wayland vs X11 before launching
- Store emulator executable path as user-configurable, not hardcoded

**For metadata scraping with rate limits:**
- ScreenScraper free tier: ~20,000 calls/day per developer account — use shared dev credentials sparingly; prod users need own accounts or use queued batch with delays
- Implement a metadata queue backed by SQLite (`metadata_queue` table), process with a background worker at ~1 req/sec to stay within limits
- Cache all API responses to disk (JSON files) before parsing — allows re-import without re-scraping

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| Avalonia 11.x | .NET 6, 7, 8, 9, 10 | Avalonia 11 targets net6.0 minimum; targeting net10.0 is fine |
| ReactiveUI 20.x | .NET 6+ | Must match Avalonia.ReactiveUI version |
| EF Core 10.x | .NET 10 only | EF Core major version tracks .NET major version — use EF Core 10 with .NET 10 |
| EF Core 9.x | .NET 8, 9 | If targeting .NET 8 LTS for compatibility, use EF Core 9 |
| Microsoft.Extensions.* | Match .NET major version | Extensions 10.x with .NET 10, 9.x with .NET 9 |
| Polly 8.x | .NET Standard 2.0+ | Works on all .NET 6+ targets |
| SharpCompress 0.36+ | .NET Standard 2.0+ | Cross-platform, works on Linux |
| Force.Crc32 | .NET Standard 2.0+ | Hardware-accelerated CRC32; falls back gracefully on ARM |
| Serilog 4.x | .NET Standard 2.0+ | Works cross-platform; file sink works on Linux |
| Avalonia.Headless | Must match Avalonia | Use same version as main Avalonia package |

---

## Confidence Summary

| Area | Confidence | Basis |
|------|------------|-------|
| .NET 10 as runtime choice | HIGH | Confirmed via official Microsoft docs (March 2026) — LTS, current |
| Avalonia UI as UI framework | HIGH | Project constraint; only viable cross-platform .NET UI framework for Linux |
| Avalonia 11.x version | MEDIUM | Training data (Aug 2025) — Avalonia 11.0 stable released June 2023, 11.x is active branch. Verify exact version at project start. |
| ReactiveUI as MVVM | HIGH | Avalonia's own templates default to ReactiveUI; strong ecosystem fit |
| EF Core + SQLite | HIGH | Standard choice for embedded .NET databases; well-established pattern |
| SharpCompress for archives | HIGH | Dominant .NET archive library, actively maintained |
| Force.Crc32 for hashing | MEDIUM | Known library, appropriate choice; verify version at project start |
| ScreenScraper as primary source | HIGH | Industry standard for retro ROM metadata; hash-based lookup is uniquely accurate |
| Polly for HTTP resilience | HIGH | Standard .NET resilience library; v8 is current |
| System.Text.Json over Newtonsoft | HIGH | .NET team recommendation; source-generator support in .NET 10 |
| FluentAvalonia for theme | MEDIUM | Good fit for NavigationView-style layout; alternatives exist (Material.Avalonia, Semi.Avalonia) |

---

## Sources

- `https://dotnet.microsoft.com/en-us/download/dotnet/10.0` — Confirmed .NET 10.0.5 LTS, SDK 10.0.201, March 12 2026 (HIGH confidence)
- `https://dotnet.microsoft.com/en-us/download` — Confirmed .NET 9 STS and .NET 10 LTS current versions (HIGH confidence)
- Avalonia UI documentation and GitHub — Training data August 2025 (MEDIUM confidence on version; HIGH on API patterns)
- ReactiveUI documentation — Training data August 2025 (MEDIUM confidence on version)
- ScreenScraper API documentation — Training data August 2025; hash-based lookup pattern is well-established (HIGH confidence on approach)
- EF Core documentation — Training data August 2025; version 10 aligned with .NET 10 (HIGH confidence)
- Polly documentation — Training data August 2025; v8 API is stable (HIGH confidence)

---

*Stack research for: YARL — cross-platform ROM browser/manager/launcher (.NET + Avalonia UI)*
*Researched: 2026-03-18*
