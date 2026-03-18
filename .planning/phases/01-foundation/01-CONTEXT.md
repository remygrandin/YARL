# Phase 1: Foundation - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Project scaffolding for YARL — a runnable skeleton app on both Windows and Linux with critical architectural decisions locked in. Delivers: .NET solution structure, Avalonia app shell with dual view modes, EF Core + SQLite database with migration framework, CI pipeline, and the IRomSourceProvider abstraction for remote path support. No features beyond the skeleton.

</domain>

<decisions>
## Implementation Decisions

### MVVM Framework
- ReactiveUI for MVVM — chosen for IScreen/RoutingState routing that maps to the dual-shell navigation model
- Hot-swap mode switching: DesktopShell and FullscreenShell swap in the same window at runtime, sharing the same ViewModel layer
- Auto-detect startup mode: if a controller/gamepad is detected at launch, start in fullscreen mode; otherwise start in desktop mode
- Mode preference is overridable in settings — user can force "always desktop" or "always fullscreen"
- Settings override persists and takes priority over auto-detection

### Solution Structure
- Single .NET project with folder-based organization (not multi-project)
- Namespace: YARL (all caps — matches branding)
- Layout: src/YARL/ for the app, tests/ for test projects
- Hosted on GitHub

### CI/CD
- GitHub Actions for CI
- Must run on both Windows and Linux runners (cross-platform verification)

### Database and Config
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

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project context
- `.planning/PROJECT.md` — Vision, core value, constraints, key decisions
- `.planning/REQUIREMENTS.md` — Full v1 requirements with phase mapping (LIB-08 is this phase)
- `.planning/ROADMAP.md` — Phase 1 goal and success criteria

### Research
- `.planning/research/STACK.md` — .NET 10 + Avalonia 11.x + ReactiveUI + EF Core stack recommendation with versions
- `.planning/research/ARCHITECTURE.md` — Component architecture, dual shell pattern, provider abstractions
- `.planning/research/PITFALLS.md` — Controller navigation must be architectural from Phase 1, Flatpak launch model, Linux CI before first UI

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — greenfield project

### Established Patterns
- None — patterns will be established in this phase

### Integration Points
- Phase 2 (ROM Scanning) will be the first consumer of the database schema and IRomSourceProvider
- Phase 5 (Desktop UI) and Phase 6 (Fullscreen) will consume the DesktopShell and FullscreenShell stubs
- All subsequent phases depend on the DI container, database context, and navigation infrastructure set up here

</code_context>

<specifics>
## Specific Ideas

- Platform definitions should work like a data pack: YARL ships with a comprehensive default platforms.json, but users can add custom platforms for niche systems or homebrew
- The auto-detect mode logic should be simple and not over-engineered — check for a connected gamepad at startup, that's it
- XDG compliance matters for Linux/Steam Deck — don't invent custom paths

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-03-18*
