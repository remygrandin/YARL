# YARL — Yet Another Retro Launcher

## What This Is

A cross-platform game ROM browser, manager, and launcher built in .NET with Avalonia UI. YARL lets users scan their ROM collections, browse them with rich metadata and cover art, and launch games in the right emulator — all with first-class controller support. Runs equally well on Windows desktop and Linux (Steam Deck).

## Core Value

Users can browse their ROM collection visually and launch any game on any platform with one click, using the right emulator, on both desktop and Steam Deck.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Scan ROM folders (background, non-blocking)
- [ ] Local and remote (OS-mounted) path support
- [ ] Local ROM cache for remote paths with disk usage management
- [ ] Cache cleanup by last-launched time (1mo, 3mo, 6mo, 1yr)
- [ ] Max disk usage setting for cache
- [ ] Browse ROMs by platform with cover art
- [ ] Full controller/gamepad navigation (first-class)
- [ ] Desktop mode (grid/list, filters, search, sidebar)
- [ ] Fullscreen/gamepad mode (console-like, big art)
- [ ] Metadata scraping from online databases (cover art, descriptions, genre, developer, publisher, release date)
- [ ] Manual metadata override
- [ ] Emulator library with one-click install
- [ ] Recommended emulator per platform
- [ ] Auto-configure emulators for ROM launch
- [ ] Launch ROMs in the correct emulator
- [ ] Platform-agnostic (any ROM, any emulator)
- [ ] Cross-platform custom collections
- [ ] Recommendation system based on genre/category/developer/publisher
- [ ] Favorites and recently played

### Out of Scope

- Direct SMB/CIFS browsing — v1 uses OS-mounted paths; extensible later
- Cloud storage integration (Google Drive, OneDrive, Dropbox) — future addition
- SFTP/SSH remote access — future addition
- Mobile app — desktop and Steam Deck only
- ROM downloading or distribution — legal boundary
- Real-time multiplayer/netplay — emulator responsibility

## Context

- **Target platforms:** Windows (desktop) and Linux (Steam Deck primarily, but any Linux)
- **UI framework:** Avalonia UI — chosen for cross-platform native support and controller input handling
- **Runtime:** .NET (latest stable)
- **Steam Deck considerations:** Limited storage makes cache management critical; gamepad-first UI is essential; runs in desktop mode or as a non-Steam game
- **Emulator ecosystem:** Wide range of emulators across platforms (RetroArch, PCSX2, Dolphin, Duckstation, RPCS3, Yuzu/Ryujinx successors, etc.) — each with different CLI launch patterns
- **Metadata sources:** ScreenScraper (ROM hash-based, strong retro coverage), IGDB (broad coverage, good API), TheGamesDB — research will determine best primary source
- **Architecture note:** Remote path handling should be designed as a provider pattern to allow adding direct SMB, cloud, and SFTP sources later without refactoring

## Constraints

- **Tech stack**: .NET + Avalonia UI — cross-platform requirement rules out WPF/WinForms
- **Controller support**: Must be first-class, not an afterthought — Avalonia input handling + Steam Input
- **Storage**: Steam Deck has limited SSD — cache management is critical, not optional
- **Legal**: No ROM distribution, no BIOS bundling — users supply their own files
- **Emulator install**: Must handle both Windows and Linux package managers/binary sources

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Avalonia UI over MAUI | Better Linux/Steam Deck support, better controller input handling | — Pending |
| OS-mounted paths for remote in v1 | Simpler to implement, covers most use cases (SMB mounts), extensible later | — Pending |
| Platform-agnostic from day 1 | User wants all platforms, not a curated subset — design emulator config to be generic | — Pending |
| Background scanning | ROM scanning must not block browsing — essential for large collections | — Pending |

---
*Last updated: 2026-03-18 after initialization*
