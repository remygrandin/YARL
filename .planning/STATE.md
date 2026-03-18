# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-18)

**Core value:** Users can browse their ROM collection visually and launch any game on any platform with one click, using the right emulator, on both desktop and Steam Deck.
**Current focus:** Phase 1 — Foundation

## Current Position

Phase: 1 of 7 (Foundation)
Plan: 0 of TBD in current phase
Status: Ready to plan
Last activity: 2026-03-18 — Roadmap created, requirements mapped to 7 phases

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: —
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: —
- Trend: —

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Roadmap]: Two independent view shells (DesktopShell, FullscreenShell) sharing the same ViewModel layer — controller focus management requires this; single adaptive view is not viable
- [Roadmap]: Phase 7 (emulator management + ROM cache) depends on Phase 4, not Phase 6 — can parallelize if needed
- [Roadmap]: Research flags Phase 3 (ScreenScraper API), Phase 4 (Flatpak launch), Phase 6 (Gamescope/Wayland), Phase 7 (cache eviction) for deeper research before implementation

### Pending Todos

None yet.

### Blockers/Concerns

- [Pre-Phase 1]: Avalonia exact version must be verified at project start (dotnet new install Avalonia.Templates)
- [Pre-Phase 1]: ReactiveUI vs CommunityToolkit.Mvvm decision must be made before Phase 1 scaffolding — ReactiveUI IScreen/routing is needed for fullscreen navigation model
- [Pre-Phase 3]: ScreenScraper rate limits and IGDB OAuth token mechanics must be verified before Phase 3 begins
- [Pre-Phase 4]: RetroArch core paths on SteamOS and Flatpak --filesystem flags per emulator must be verified before Phase 4 begins
- [Pre-Phase 6]: Avalonia Wayland/Gamescope support status must be checked before Phase 6 begins

## Session Continuity

Last session: 2026-03-18
Stopped at: Roadmap created, STATE.md initialized — ready to begin Phase 1 planning
Resume file: None
