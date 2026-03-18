# Feature Research

**Domain:** ROM browser / manager / launcher (cross-platform, desktop + Steam Deck)
**Researched:** 2026-03-18
**Confidence:** MEDIUM (training knowledge of Playnite, LaunchBox, ES-DE, Pegasus, RetroArch, RomM — external verification unavailable in this session)

---

## Competitor Reference

Tools analyzed (from training data, all actively maintained as of mid-2025):

| Tool | Type | Platform | Strength |
|------|------|----------|----------|
| **Playnite** | Full launcher/manager | Windows only | Plugin ecosystem, unified library, desktop + fullscreen |
| **LaunchBox / BigBox** | Full launcher/manager | Windows only | Best-in-class media DB, premium BigBox fullscreen mode |
| **EmulationStation-DE (ES-DE)** | Frontend/browser | Cross-platform incl. Steam Deck | Portable, theme ecosystem, no emulator management |
| **Pegasus Frontend** | Frontend/browser | Cross-platform incl. Steam Deck | QML themes, scripted, lightweight, no management layer |
| **RetroArch** | Emulator core hub | Cross-platform | XMB UI, integrated emulator, not a manager |
| **RomM** | Web-based ROM manager | Self-hosted server | Metadata, file management, streaming-oriented |
| **Batocera / RetroPie** | OS-level frontend | Dedicated/SBC | Full OS stack, not portable |

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| ROM folder scanning | Foundation of the product — without it there's nothing to show | MEDIUM | Must be non-blocking/background; handle large collections (10k+ files) |
| Browse by platform/system | Every competitor organizes by console/platform first | LOW | Platform list with icon/logo, count of games |
| Cover art display | Visual browsing is the entire UX value prop; text lists feel like a file manager | MEDIUM | Grid view primary; need fallback placeholder art |
| Game metadata (title, year, genre, description) | Users expect to see what a game is, not just a filename | MEDIUM | Sourced from scraping or bundled DB |
| Metadata scraping from online DB | Manual entry is unusable at scale; all competitors scrape automatically | HIGH | Hash-based matching preferred (ScreenScraper); fallback to name search |
| Launch game in emulator | Core function — if this fails the product fails | MEDIUM | Must resolve correct emulator per platform, pass correct CLI args |
| Emulator configuration per platform | Different emulators for different platforms; users need to map them | MEDIUM | At minimum: per-platform emulator path + launch args |
| Favorites | Every competitor has this; it's the minimum personalization | LOW | Simple flag on game record |
| Recently played | Standard across all tools; users return to recent games constantly | LOW | Track last-launched timestamp + play count |
| Search / filter by name | Collections grow large; users need to find specific games quickly | LOW | Real-time filter on current platform view |
| Filter by genre / year / players | Power users have hundreds of games per platform; filters are essential | MEDIUM | Requires metadata to be populated first |
| Fullscreen / TV / big-picture mode | Steam Deck is a first-class target; controller-only navigation required | HIGH | Separate layout — large art, minimal text, D-pad navigation |
| Controller / gamepad navigation | Steam Deck use case demands this; keyboard-only is a dealbreaker for couch use | HIGH | Full UI traversal without mouse/keyboard |
| Manual metadata override | Scrapers are wrong sometimes; users need to fix individual fields | MEDIUM | Per-game edit form: title, cover art URL/file, description, genre, etc. |
| Custom collections / playlists | Users want curated lists across platforms ("RPGs", "Co-op", "Childhood") | MEDIUM | Named list, add/remove games; cross-platform |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valued.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| One-click emulator install | No competitor does this well — Playnite does it via plugins, LaunchBox doesn't, ES-DE doesn't | HIGH | Must handle Windows installers/portables AND Linux package managers + AppImage/Flatpak; Steam Deck-specific paths |
| Recommended emulator per platform | Users don't know which emulator is "best" — opinionated defaults remove friction | MEDIUM | Curated mapping: platform → recommended emulator + config; surfaced before first launch |
| Auto-configure emulator for launch | RetroArch aside, most tools just pass a path — actually configuring (BIOS paths, settings) is rare | HIGH | Minimal viable: ensure correct core/exe is used and ROM path is passed correctly |
| Remote/network path support (OS-mounted) | NAS users represent a large chunk of the audience; direct network-share awareness is rare in frontends | MEDIUM | OS-mounted paths work like local; UX just needs to handle latency gracefully |
| ROM caching with LRU eviction | Critical for Steam Deck where storage is limited; no other frontend does cache management | HIGH | Local copy of remote ROM before launch; background pre-fetch; evict by last-used time and size cap |
| Recommendation engine (genre/dev/pub) | "What should I play next?" is a common problem; no lightweight frontend addresses it | HIGH | Based on play history + metadata tags; similarity scoring; can start simple (same genre/developer) |
| Dual UI mode (desktop + fullscreen) in one app | Playnite does this well; ES-DE/Pegasus are fullscreen-only; LaunchBox charges for BigBox | MEDIUM | Same data layer, two distinct layout trees; mode switch at runtime |
| Steam Deck as first-class citizen | ES-DE and Pegasus work on Steam Deck but weren't designed for it; YARL can be | MEDIUM | Storage budget awareness, Steam Input integration, correct default emulator paths for SteamOS |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Direct SMB/CIFS browser (in-app) | Users want to browse NAS without mounting | Adds OS-level protocol handling, auth flows, credential storage, Windows/Linux path differences — massive scope creep | Document OS-mount workflow (Samba/CIFS via fstab or Windows mapped drive); design provider interface for future addition |
| Cloud sync (Google Drive, Dropbox, OneDrive) | Users want saves and collections synced | OAuth flows per provider, quota management, conflict resolution, API rate limits — each integration is a product in itself | Out of scope v1; provider pattern allows future addition without refactoring |
| ROM downloading | "Complete my collection" is a user desire | Legal liability is existential; DMCA risk; no viable partner; Cdromance/Vimm have takedowns regularly | Never build; document clearly in UI ("bring your own ROMs") |
| BIOS bundling | Users want plug-and-play | Legal gray area at best, clear copyright violation at worst; taints the whole project | Guide users to BIOS sources; validate BIOS presence and show clear error with guidance |
| Netplay / online multiplayer | Retro gaming nostalgia + social | Emulator responsibility, not frontend; adding it means embedding or forking an emulator | Surface per-emulator netplay docs; never implement in YARL |
| Game achievement tracking (RetroAchievements) | Popular feature — LaunchBox and RetroArch support it | Requires RetroAchievements account integration, OAuth, per-ROM achievement data, live state hooks into emulator — separate product complexity | Delegate to RetroArch/emulator layer; out of scope for v1 |
| Plugin / extension system | Power users want customization | Massively increases maintenance surface; Playnite's plugin ecosystem is its biggest support burden; APIs must be versioned forever | Opinionated defaults + config files; extensible architecture internally without public plugin API |
| Mobile companion app | "Manage collection from phone" | Entirely separate UI stack, sync architecture, iOS/Android distribution overhead | Not a stated goal; defer indefinitely |
| Scraper for every metadata source | "Support ScreenScraper AND IGDB AND TheGamesDB AND GameFAQs..." | Each API is different auth, rate limits, data model; merging conflicting data is hard | Pick one primary (ScreenScraper for retro hash accuracy), one fallback (IGDB for coverage); keep the abstraction clean for future sources |

---

## Feature Dependencies

```
ROM Folder Scanning
    └──requires──> Platform Detection (file extension → platform mapping)
    └──produces──> ROM Records (title, path, platform)

ROM Records
    └──enables──> Browse by Platform
    └──enables──> Metadata Scraping
    └──enables──> Launch Game

Metadata Scraping
    └──requires──> ROM Records (need hash or filename to match)
    └──produces──> Cover Art, Description, Genre, Year, Developer, Publisher
    └──enables──> Filter by Genre/Year/Developer
    └──enables──> Recommendation Engine

Cover Art
    └──requires──> Metadata Scraping (or Manual Override)
    └──enables──> Grid View (meaningful)

Emulator Configuration (per-platform mapping)
    └──enables──> Launch Game
    └──requires nothing beyond user setup

Launch Game
    └──requires──> ROM Records
    └──requires──> Emulator Configuration
    └──updates──> Recently Played
    └──triggers──> ROM Cache pre-fetch (if remote path)

ROM Cache
    └──requires──> Remote Path Detection
    └──requires──> Local Cache Storage setting (max size, eviction policy)
    └──enables──> Launch Game (remote ROMs without copying manually)

Favorites / Recently Played
    └──requires──> ROM Records
    └──enables──> Collections (special auto-collections)

Custom Collections
    └──requires──> ROM Records
    └──enhances──> Browse by Platform (cross-platform view)

Fullscreen Mode
    └──requires──> All core features (same data)
    └──requires──> Controller Navigation

Controller Navigation
    └──enables──> Fullscreen Mode (usable)
    └──enhances──> Desktop Mode

One-Click Emulator Install
    └──enhances──> Emulator Configuration
    └──requires──> Platform-specific install logic (Windows vs Linux)

Recommendation Engine
    └──requires──> Metadata (genre, developer, publisher)
    └──requires──> Play History (recently played, favorites)
    └──enhances──> Browse by Platform (surfaced suggestions)

Manual Metadata Override
    └──requires──> ROM Records
    └──overrides──> Metadata Scraping output
```

### Dependency Notes

- **ROM Folder Scanning must precede everything:** The entire product is inert without a populated ROM library. This is Phase 1 with no exception.
- **Metadata Scraping unlocks the visual layer:** Cover art + metadata transforms a file list into a browsable collection. Must land before fullscreen mode is meaningful.
- **Emulator Configuration is a prerequisite to Launch:** Users cannot launch without at least one emulator mapped. Emulator install (differentiator) makes this easier but isn't required for launch to work.
- **Fullscreen Mode requires Controller Navigation:** A fullscreen mode without gamepad support is unusable on Steam Deck — they must ship together.
- **ROM Cache requires remote path support:** Local-only users never need it. Design as conditional feature gated on whether path is remote/mounted share.
- **Recommendation Engine is a late-stage feature:** Requires populated metadata AND accumulated play history — meaningless in a fresh install.

---

## MVP Definition

### Launch With (v1)

Minimum viable product — what's needed to validate the concept.

- [ ] ROM folder scanning (background, non-blocking) — without this nothing works
- [ ] Browse by platform with cover art grid — the core visual promise
- [ ] Metadata scraping (ScreenScraper primary + IGDB fallback) — makes browsing meaningful
- [ ] Manual metadata override — scraper is never 100% accurate
- [ ] Emulator configuration per platform (manual path + args mapping) — needed to launch anything
- [ ] Launch game in correct emulator — the single most important user action
- [ ] Favorites and recently played — minimum personalization, expected by all users
- [ ] Search by name — essential for collections over 50 games
- [ ] Filter by genre/platform — power user baseline
- [ ] Custom collections — cross-platform curation, differentiates from simple file browsers
- [ ] Desktop mode (grid + list views, sidebar navigation) — primary PC use case
- [ ] Fullscreen/gamepad mode — Steam Deck first-class requirement
- [ ] Controller/gamepad navigation — required for fullscreen mode to function
- [ ] OS-mounted path support — covers NAS/network users without SMB complexity
- [ ] Recommendation system (genre/developer/publisher similarity) — stated project requirement, differentiator

### Add After Validation (v1.x)

Features to add once core is working and validated.

- [ ] One-click emulator install — reduces setup friction significantly; validates demand first
- [ ] Recommended emulator per platform — opinionated defaults; needs emulator install to be most useful
- [ ] Auto-configure emulator settings — significant complexity; validate manual config is the actual friction point first
- [ ] ROM cache with LRU eviction — essential for Steam Deck NAS users; validate remote path usage pattern first
- [ ] Cache cleanup settings (size cap, time-based eviction) — follows cache implementation

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] Direct SMB/CIFS browsing — design provider pattern now, implement when demand is clear
- [ ] Cloud storage integration — separate OAuth complexity per provider
- [ ] SFTP/SSH remote access — niche but requested; provider pattern makes it addable
- [ ] RetroAchievements integration — popular but complex; delegate to RetroArch in v1
- [ ] Advanced statistics / playtime tracking — quality-of-life; not a launch blocker
- [ ] Theme system — Pegasus/ES-DE differentiator; high effort, moderate value for YARL's target

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| ROM folder scanning | HIGH | MEDIUM | P1 |
| Browse by platform + cover art | HIGH | MEDIUM | P1 |
| Metadata scraping | HIGH | HIGH | P1 |
| Launch game | HIGH | MEDIUM | P1 |
| Emulator config (manual) | HIGH | MEDIUM | P1 |
| Controller/gamepad navigation | HIGH | HIGH | P1 |
| Fullscreen mode | HIGH | HIGH | P1 |
| Favorites + recently played | MEDIUM | LOW | P1 |
| Search + filter | MEDIUM | LOW | P1 |
| Manual metadata override | MEDIUM | MEDIUM | P1 |
| Custom collections | MEDIUM | MEDIUM | P1 |
| OS-mounted path support | MEDIUM | LOW | P1 |
| Recommendation engine | MEDIUM | HIGH | P1 (stated requirement) |
| One-click emulator install | HIGH | HIGH | P2 |
| Recommended emulator defaults | MEDIUM | MEDIUM | P2 |
| ROM cache + eviction | HIGH (Steam Deck) | HIGH | P2 |
| Auto-configure emulator | MEDIUM | HIGH | P2 |
| SFTP/cloud/SMB direct | LOW | HIGH | P3 |
| RetroAchievements | MEDIUM | HIGH | P3 |
| Theme system | LOW | HIGH | P3 |
| Playtime statistics | LOW | MEDIUM | P3 |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

---

## Competitor Feature Analysis

| Feature | Playnite | LaunchBox/BigBox | ES-DE | Pegasus | YARL Plan |
|---------|----------|-----------------|-------|---------|-----------|
| ROM folder scanning | Yes (plugin) | Yes | Yes | Yes (gamelist.xml or script) | Yes, native |
| Cover art / media | Yes | Yes (best DB) | Yes | Yes | Yes, scraped |
| Metadata scraping | Yes (plugin) | Yes (LaunchBox DB) | Yes (ScreenScraper/IGDB) | Minimal | Yes, native |
| Launch game | Yes | Yes | Yes | Yes | Yes, native |
| Emulator management | Plugin | Manual + download guide | No (assumes installed) | No | Yes, install + config |
| Controller navigation | Yes (fullscreen) | BigBox only (paid) | Yes | Yes | Yes, both modes |
| Fullscreen mode | Yes (free) | BigBox (paid upgrade) | Yes (only) | Yes (only) | Yes, free |
| Desktop mode | Yes | Yes | No | No | Yes |
| Custom collections | Yes | Yes | Yes | Yes | Yes |
| Recommendations | No | No | No | No | Yes (differentiator) |
| ROM cache (remote) | No | No | No | No | Yes (differentiator) |
| One-click emulator install | Partial (plugin) | Partial | No | No | Yes (differentiator) |
| Cross-platform (Win+Linux) | No (Windows only) | No (Windows only) | Yes | Yes | Yes |
| Steam Deck native | No | No | Yes (adapted) | Yes (adapted) | Yes (first-class) |
| Network/NAS paths | Basic | Basic | Basic | Basic | Yes (OS-mounted + future SMB) |

---

## Sources

- Playnite feature knowledge: training data (playnite.link docs, GitHub JosefNemec/Playnite), confidence MEDIUM
- LaunchBox/BigBox features: training data (launchbox-app.com), confidence MEDIUM
- EmulationStation-DE: training data (es-de.org, GitLab), confidence MEDIUM
- Pegasus Frontend: training data (pegasus-frontend.org), confidence MEDIUM
- RetroArch: training data (retroarch.com), confidence HIGH (very stable feature set)
- RomM: training data (GitHub rommapp/romm), confidence MEDIUM
- Feature categorization: synthesized from project requirements in PROJECT.md + domain knowledge

**Note:** External web verification was unavailable during this research session. All findings are from training data (cutoff August 2025). Features marked as competitor-absent should be re-verified before using as marketing claims. Core table-stakes features are stable enough to treat as HIGH confidence; specific competitor feature states are MEDIUM confidence.

---
*Feature research for: ROM browser / manager / launcher*
*Researched: 2026-03-18*
