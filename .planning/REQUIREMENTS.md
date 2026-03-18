# Requirements: YARL

**Defined:** 2026-03-18
**Core Value:** Users can browse their ROM collection visually and launch any game on any platform with one click, using the right emulator, on both desktop and Steam Deck.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Library

- [x] **LIB-01**: User can configure ROM folder paths for scanning
- [ ] **LIB-02**: App scans folders in background without blocking browsing
- [ ] **LIB-03**: User can browse ROMs organized by platform with cover art grid
- [ ] **LIB-04**: User can search games by name with real-time filtering
- [ ] **LIB-05**: User can filter games by genre, year, and developer
- [ ] **LIB-06**: User can mark games as favorites
- [ ] **LIB-07**: App tracks recently played games with last-launched time
- [x] **LIB-08**: User can tag a path as "remote" (OS-mounted network share)
- [ ] **LIB-09**: User can create custom cross-platform collections

### Metadata

- [ ] **META-01**: App scrapes metadata from ScreenScraper using ROM hash matching
- [ ] **META-02**: App falls back to IGDB for games not found on ScreenScraper
- [ ] **META-03**: Scraping fetches cover art, description, genre, year, developer, publisher
- [ ] **META-04**: User can manually override any metadata field per game

### Emulator

- [ ] **EMU-01**: User can configure emulator path and launch args per platform
- [ ] **EMU-02**: App launches ROM in the correct emulator for its platform
- [ ] **EMU-03**: App provides one-click emulator install (Windows + Linux/Flatpak)
- [ ] **EMU-04**: App suggests a recommended emulator per platform
- [ ] **EMU-05**: App auto-configures emulator settings for ROM launch

### UI

- [ ] **UI-01**: Desktop mode with grid/list views, sidebar navigation, and filters
- [ ] **UI-02**: Fullscreen/gamepad mode with large cover art and minimal text
- [ ] **UI-03**: Full controller/gamepad navigation across all UI
- [ ] **UI-04**: User can switch between desktop and fullscreen modes at runtime

### Cache

- [ ] **CACHE-01**: App caches remote ROMs locally before launch
- [ ] **CACHE-02**: User can set max disk usage for ROM cache
- [ ] **CACHE-03**: App evicts cached ROMs by LRU (least recently launched)
- [ ] **CACHE-04**: User can clean up cache by last-launched time (1mo/3mo/6mo/1yr)
- [ ] **CACHE-05**: Cache management UI shows space usage and per-game cache status

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Metadata

- **META-05**: Recommendation engine suggests similar games based on genre/developer/publisher
- **META-06**: Play history influences recommendations

### Social / Integration

- **INTG-01**: RetroAchievements integration
- **INTG-02**: Playtime tracking and statistics

### Remote Access

- **RMTE-01**: Direct SMB/CIFS browsing without OS mount
- **RMTE-02**: Cloud storage integration (Google Drive, OneDrive, Dropbox)
- **RMTE-03**: SFTP/SSH remote access

### Customization

- **CUST-01**: Theme system for UI customization

## Out of Scope

| Feature | Reason |
|---------|--------|
| ROM downloading/distribution | Legal liability — users supply their own ROMs |
| BIOS bundling | Copyright violation — guide users to source their own |
| Netplay/online multiplayer | Emulator responsibility, not frontend |
| Plugin/extension system | Massive maintenance burden — opinionated defaults instead |
| Mobile companion app | Entirely separate UI stack — not a v1 goal |
| Game streaming | Different product category entirely |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| LIB-01 | Phase 2 | Complete |
| LIB-02 | Phase 2 | Pending |
| LIB-03 | Phase 2 | Pending |
| LIB-04 | Phase 3 | Pending |
| LIB-05 | Phase 3 | Pending |
| LIB-06 | Phase 2 | Pending |
| LIB-07 | Phase 2 | Pending |
| LIB-08 | Phase 1 | Complete |
| LIB-09 | Phase 5 | Pending |
| META-01 | Phase 3 | Pending |
| META-02 | Phase 3 | Pending |
| META-03 | Phase 3 | Pending |
| META-04 | Phase 3 | Pending |
| EMU-01 | Phase 4 | Pending |
| EMU-02 | Phase 4 | Pending |
| EMU-03 | Phase 7 | Pending |
| EMU-04 | Phase 7 | Pending |
| EMU-05 | Phase 7 | Pending |
| UI-01 | Phase 5 | Pending |
| UI-02 | Phase 6 | Pending |
| UI-03 | Phase 5 | Pending |
| UI-04 | Phase 5 | Pending |
| CACHE-01 | Phase 7 | Pending |
| CACHE-02 | Phase 7 | Pending |
| CACHE-03 | Phase 7 | Pending |
| CACHE-04 | Phase 7 | Pending |
| CACHE-05 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 27 total
- Mapped to phases: 27
- Unmapped: 0

---
*Requirements defined: 2026-03-18*
*Last updated: 2026-03-18 after roadmap creation — all 27 requirements mapped*
