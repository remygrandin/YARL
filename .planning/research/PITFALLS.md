# Pitfalls Research

**Domain:** Cross-platform ROM browser / manager / launcher (.NET + Avalonia UI)
**Researched:** 2026-03-18
**Confidence:** MEDIUM — Based on training data from known projects (Playnite, EmulationStation-DE, RetroArch, LaunchBox). Could not verify via live sources due to tool restrictions. Flag for validation before implementation.

---

## Critical Pitfalls

### Pitfall 1: Controller Navigation as an Afterthought

**What goes wrong:**
The application is built keyboard/mouse-first and controller support is bolted on at the end. Focus management, scroll-into-view, modal dismissal, and virtual keyboard are never properly wired. The result is an app that "works with a controller" in the demo but breaks in real use: focus gets stuck on hidden elements, pressing B doesn't go back, scrolling lists jump to the wrong item, and text search requires a physical keyboard.

**Why it happens:**
Avalonia's focus system is designed for pointer/keyboard input. Controller input arrives as GamepadButton events (or raw joystick axes) and requires explicit mapping to UI actions — there is no automatic "controller mode." Developers defer this as a UX polish pass but it requires architectural decisions (focus scopes, navigation graphs, modal stacks) that are very hard to retrofit.

**How to avoid:**
Decide the navigation model in Phase 1. Use a dedicated `NavigationManager` / focus scope hierarchy. Every view must define: what is the default focused element, what does B/Back do, what does the menu button open. Test on a gamepad after every view is built. Use Avalonia's `KeyboardDevice.SetFocusedElement` and `FocusManager` deliberately. Consider a "controller mode" flag that simplifies layouts.

**Warning signs:**
- First PR that adds a new screen doesn't mention where focus lands on entry.
- Test matrix has no gamepad test column.
- "Controller support" is a single backlog ticket rather than per-view acceptance criteria.

**Phase to address:** Foundation / Core UI phase (first UI milestone). Must be structural from day one.

---

### Pitfall 2: Emulator CLI Arguments Are Never Stable

**What goes wrong:**
The emulator abstraction stores a static CLI template per emulator (e.g., `retroarch -L {core} "{rom}"`). Six months in, RetroArch changes a flag, PCSX2 v2.0 drops the old CLI entirely, Dolphin adds a required `--config` argument for headless launches, and the app silently fails to launch games for those emulators with no useful error message.

**Why it happens:**
Emulators are independent projects with no stable API contract. Their CLI interfaces are undocumented or change without notice. Projects treat the CLI template as static data rather than versioned, validated configuration.

**How to avoid:**
Model each emulator as a versioned profile: `{emulator_id, version_constraint, cli_template, launch_args, exit_code_map}`. Ship community-maintained profiles as a separate versioned data file (JSON/TOML), not hardcoded in the binary. Implement a launch-verification test: on first launch per emulator, run `emulator --version` and compare against profile's `version_constraint`, warning when out of range. Log the exact process invocation (command + arguments) to a debug log. Make the CLI template user-editable in settings as an escape hatch.

**Warning signs:**
- Emulator profiles are defined in C# constants or appsettings.json with no version field.
- No integration test that actually spawns an emulator process.
- "Launch failed" error message doesn't show the command that was run.

**Phase to address:** Emulator integration phase. Version constraints should be part of the initial schema design.

---

### Pitfall 3: ROM Scanner Blocks the UI Thread

**What goes wrong:**
Scanning a large ROM collection (10,000+ files across network mounts) freezes the UI for 30–120 seconds. Users think the app crashed. On Steam Deck, the OS watchdog may kill the process.

**Why it happens:**
`Directory.EnumerateFiles` on a network mount (SMB, NFS) can block for seconds per directory due to network latency. Even on local SSD, hashing 10,000 files for ScreenScraper is CPU-intensive. Developers scaffold the scan on the UI thread or use a background Task but update `ObservableCollection` synchronously, causing dispatcher congestion.

**How to avoid:**
Scan runs entirely on a background `Task` / `Channel`. Use `Directory.EnumerateFiles` (lazy) not `GetFiles` (eager). Batch UI updates: accumulate 50–100 ROM entries, then dispatch a single collection update. Expose a `ScanProgress` observable (files scanned, files remaining, current path) that the UI subscribes to. Never await file I/O on the UI thread. On network paths, implement a timeout per directory and surface mount-unavailable as a warning, not an error.

**Warning signs:**
- `ObservableCollection.Add` called directly inside the scan loop without marshaling.
- No cancellation token passed to the scan operation.
- Scan result is a `List<Rom>` returned from one big async method rather than streamed.

**Phase to address:** ROM scanning phase. The background/streaming architecture must be established before adding metadata lookups to the scan pipeline.

---

### Pitfall 4: Metadata Scraping Rate Limits Cause Bans

**What goes wrong:**
During initial collection scan, the app fires one HTTP request per ROM to ScreenScraper or IGDB, hammering the API with hundreds of concurrent requests. The account gets rate-limited or permanently banned. ScreenScraper in particular enforces per-user daily quotas (around 20,000 requests/day for free accounts) and IP bans for abusive clients.

**Why it happens:**
Developers use `Task.WhenAll` or `Parallel.ForEachAsync` with unlimited concurrency on the metadata fetch phase. No throttling, no backoff, no request queue. The "it works on my 20-game test collection" phase never reveals the problem.

**How to avoid:**
- Implement a `RateLimitedHttpClient` wrapper with a `SemaphoreSlim` limiting concurrent requests (max 1–2 for ScreenScraper, 4 for IGDB).
- Add exponential backoff on HTTP 429 / 503 responses.
- ScreenScraper specifically: use ROM file hash (CRC32/MD5/SHA1) for lookups — this is their preferred method and more accurate than filename matching.
- Cache API responses to disk (SQLite or flat files) so rescans never re-request already-fetched metadata.
- Surface remaining quota in the UI when approaching limits.
- Respect `Retry-After` headers.

**Warning signs:**
- Metadata fetcher uses `Task.WhenAll` with no concurrency limit.
- No HTTP response cache between app sessions.
- No test covering HTTP 429 response handling.

**Phase to address:** Metadata scraping phase. The rate-limited client must be the very first thing built before any scraping integration.

---

### Pitfall 5: Avalonia Rendering Differences Between Windows and Linux

**What goes wrong:**
The app looks and works correctly on Windows (Direct3D/Skia backend) but has rendering artifacts, font rendering differences, missing icons, or input handling bugs on Linux (X11/Wayland backends). Steam Deck ships with a Wayland compositor (KDE Plasma / Gamescope) that has additional quirks.

**Why it happens:**
Avalonia abstracts rendering but backend behavior differs:
- Font fallback chains differ between Windows (DirectWrite) and Linux (FreeType/fontconfig). Custom fonts that load fine on Windows may not render on Linux if the font embedding isn't correct.
- Wayland has no global keyboard grab — popup menus and tooltips behave differently.
- `WindowState.FullScreen` behavior varies significantly by compositor on Linux.
- The Gamescope compositor (Steam Deck game mode) intercepts some input events before Avalonia sees them.
- Skia rendering of some visual effects (blur, shadows) may differ at the GPU driver level.

**How to avoid:**
- CI must include a Linux test run (Docker + Xvfb or GitHub Actions Linux runner).
- Test fullscreen toggle specifically on Linux early — don't assume `WindowState` is portable.
- Use system font fallbacks via Avalonia's `FontFamily` cascading instead of custom-only fonts for body text.
- For Steam Deck fullscreen/kiosk mode, test under Gamescope specifically (not just standard KDE).
- Keep platform-specific code behind `OperatingSystem.IsLinux()` / `IsWindows()` guards from the start rather than discovering the need to branch late.

**Warning signs:**
- Only tested on Windows during development.
- No Linux CI runner.
- Fullscreen/window mode switching is "TODO — test on Steam Deck."

**Phase to address:** Foundation phase. Set up Linux CI as part of project scaffolding before writing UI code.

---

### Pitfall 6: Image Loading Freezes the UI at Scale

**What goes wrong:**
The grid view loads 500+ cover art images synchronously as the user scrolls. Each image load (decode + display) happens on the UI thread, causing visible stutter. On Steam Deck's AMD iGPU, GPU texture upload of many large images in a single frame causes frame drops. Memory usage balloons to 2–4 GB with full-res images.

**Why it happens:**
The naive approach is binding an `Image.Source` to a file path and letting Avalonia load it. Avalonia's default image loading is synchronous on first render. No virtualization, no deferred loading, no cache eviction.

**How to avoid:**
- Use lazy/async image loading: load images on a background thread, display a placeholder until ready, then swap in the image.
- Implement a bounded `ImageCache` (e.g., 200 images max, LRU eviction) to cap memory.
- Store thumbnails at display resolution (e.g., 200x280 for a grid card) alongside original resolution — never decode a 1920x1080 scan just to display it at 200x280.
- `ItemsControl` / `ListBox` with UI virtualization must be verified to actually virtualize (Avalonia's VirtualizingStackPanel must be explicitly set).
- For the fullscreen/big art mode, load high-res lazily while showing the thumbnail immediately.

**Warning signs:**
- Cover art stored at original scan resolution without a thumbnail pipeline.
- `ItemsControl` without explicit `VirtualizingStackPanel` — Avalonia does not virtualize by default.
- No profiling done at 500+ items.

**Phase to address:** Browse UI phase, when the grid view is first built. Do not defer thumbnail generation to a "polish" phase.

---

### Pitfall 7: Cache Management Logic Never Runs

**What goes wrong:**
The "delete cached ROMs older than X months" feature is implemented but never actually fires. The Steam Deck fills up completely, games stop launching, and users blame the app for a "disk full" crash. Alternatively, aggressive cleanup deletes a game the user is mid-playthrough of.

**Why it happens:**
Cache cleanup is implemented as a background job triggered on startup, but:
- It's never tested because test machines don't have limited storage.
- "Last launched time" isn't persisted reliably — a database migration clears it.
- The cleanup routine has a bug that makes it a no-op when no games have been launched recently.
- Disk usage calculation uses incorrect paths or doesn't account for filesystem overhead.

**How to avoid:**
- Persist `last_launched_at` in the database from day 1, never derive it from filesystem metadata.
- Cache cleanup must be: observable (show user what will be deleted before deleting), reversible (30-second undo window), and testable (injectable clock for "what counts as 3 months old").
- Display current cache size and "estimated time until full" prominently in settings.
- Test with an artificially low disk quota (e.g., mock free space = 500 MB) during development.
- Never delete a ROM that is currently queued to launch or was launched in the last 24 hours.

**Warning signs:**
- `last_launched_at` is derived from `File.GetLastAccessTime()` — unreliable on Linux and some NTFS configs.
- Cache cleanup test coverage is 0%.
- No UI showing current cache disk usage.

**Phase to address:** Cache management phase. The data model must include `last_launched_at` from the initial DB schema.

---

### Pitfall 8: Emulator Installation Abstraction Breaks on Steam Deck

**What goes wrong:**
The "one-click install" for emulators works on Windows (download binary, extract, register path) but fails on Steam Deck because:
- Many emulators on Steam Deck are distributed as Flatpaks, not binaries.
- Flatpak apps launch via `flatpak run com.example.Emulator` not a direct binary path.
- The Flatpak sandbox restricts file system access — the ROM path the app passes may not be visible inside the Flatpak sandbox.
- Some emulators are available only through EmuDeck's custom install scripts, not standard Flatpak.

**Why it happens:**
The emulator install abstraction is designed around "download binary to `~/.config/YARL/emulators/`" which works fine on Windows and for AppImage/native Linux binaries but doesn't model the Flatpak execution model at all.

**How to avoid:**
- Model the launch method as a discriminated union from day 1: `NativeBinary(path)`, `Flatpak(appId, extraArgs)`, `AppImage(path)`, `SystemCommand(command)`.
- For Flatpak launches: use `flatpak run --filesystem=host <appId> -- <rom>` to bypass sandbox path restrictions, or pass `--file-forwarding` depending on the app.
- Detection: `flatpak list --app --columns=application` to discover installed Flatpak emulators before prompting install.
- Flatpak installs: `flatpak install flathub <appId>` — needs network, shows user prompt — use `--noninteractive` but surface progress.
- Document which emulators are Flatpak-only on Linux (RPCS3, PPSSPP, many others have Flathub versions as the primary distribution).

**Warning signs:**
- Emulator profile only has a `BinaryPath` field, no `LaunchType` discriminator.
- No test for Flatpak launch path.
- "Emulator install" tested on Windows only.

**Phase to address:** Emulator integration phase, specifically the launch abstraction design. Must be modeled before any installer UI is built.

---

### Pitfall 9: SQLite Database Schema Designed Without Future Migrations in Mind

**What goes wrong:**
v1 ships with a flat schema. When platforms, multi-disc ROMs, or ROM variants (e.g., different region dumps of the same game) are added, the schema requires breaking changes. SQLite has very limited `ALTER TABLE` support (no column rename, no column drop in older SQLite versions). Existing users lose their metadata and favorites on update.

**Why it happens:**
The schema is created with `CREATE TABLE IF NOT EXISTS` statements scattered through the codebase. No migration framework. Version number stored in a `PRAGMA user_version` is never checked. When the schema changes, the app either crashes on startup or silently runs against the old schema.

**How to avoid:**
- Use a migration framework from day 1: EF Core migrations or a lightweight library like `FluentMigrator` or `Evolve`.
- Every schema change is a numbered migration file. Database version is checked on every app start. Startup fails with a clear error (not a crash) if migration fails.
- Design the initial schema with extensibility: use a separate `rom_metadata` key-value table for scraper data rather than columns on the `rom` table. This way adding a new metadata field is an insert, not an `ALTER TABLE`.
- Test migration path: ship a test that starts from schema v1, inserts data, runs all migrations, and verifies data integrity.

**Warning signs:**
- Schema creation in a single `ExecuteNonQuery` string with no version tracking.
- Metadata fields as columns on the main `roms` table.
- No migration test.

**Phase to address:** Foundation / data model phase. Migration framework must be set up before the first feature that uses the database.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Hardcode emulator CLI templates in C# | Fast to implement first emulator | Every emulator update requires a binary release; community can't contribute profiles | Never — use external data files from day 1 |
| Load all ROMs into memory on startup | Simple data access pattern | 10k+ ROM collection uses 500MB+ RAM; startup takes 10+ seconds | Never — use paginated/lazy DB queries |
| Store cover art as full-resolution originals only | Simpler pipeline | Grid view stutters; memory exhaustion on large collections | MVP only if grid shows <50 items |
| No cancellation tokens on background tasks | Simpler async code | App hangs on shutdown; scan cannot be interrupted mid-way | Never — add CancellationToken from the start |
| Synchronous HTTP for metadata (blocking, one at a time) | Easy to reason about | Full scan of 1000 ROMs takes 30+ minutes | Prototype only, never ship |
| Filename-only ROM matching (no hash) | No hashing overhead on scan | Wrong metadata for regional variants, hacks, overdumps | Acceptable for IGDB fallback only; ScreenScraper requires hash |
| `Process.Start` without process tracking | Simple launch | Cannot detect emulator crash; no "game is running" state; re-launch while running causes two instances | Never — always track launched process PID |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| ScreenScraper API | Use filename for lookup | Use CRC32/MD5/SHA1 file hash — ScreenScraper's hash database is far more accurate than name matching; name matching is a fallback only |
| ScreenScraper API | Fire all requests immediately on scan | Enforce max 1–2 concurrent requests; implement exponential backoff on 429; cache responses persistently |
| ScreenScraper API | Ignore `ssusersinfos` response fields | The API response includes `requeststoday`, `maxrequestsperday`, `maxthreads` — read these and throttle accordingly |
| IGDB API | Forget token expiry | IGDB uses Twitch OAuth — tokens expire; implement refresh logic and handle 401 gracefully |
| IGDB API | Search by exact name | Use fuzzy search + `search` endpoint, not `where name = "..."` — ROM filenames rarely match IGDB titles exactly |
| Emulator process launch | `Process.Start` and forget | Store `Process` reference; watch for exit; map exit code to success/error using emulator's documented exit codes |
| Flatpak emulators (Linux) | Launch via binary path inside Flatpak bundle | Use `flatpak run --filesystem=host <appId>` to ensure ROM files are accessible |
| RetroArch cores | Assume core files are stable paths | Core paths differ between OS, between RetroArch versions, and between standalone vs. Flatpak installs; always resolve core paths at launch time |
| OS-mounted network paths (SMB) | Treat mount failure as scan error | Mount drops are common; scan should detect "path not reachable" and surface as a warning with retry, not fail the entire scan |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| `Directory.GetFiles` (eager) on network mount | UI freezes 10–120 seconds on scan start | Use `Directory.EnumerateFiles` (lazy streaming) | Immediately on any network path |
| Loading all ROM records from SQLite without pagination | Startup slow; 500MB+ RAM for 10k collection | Paginate with `LIMIT`/`OFFSET` or keyset pagination; virtualize the list | ~5,000 ROMs |
| Loading full-res cover images in grid view | Grid scroll stutters; 2–4 GB RAM | Generate thumbnails at ingest; display thumbnails in grid, full-res on detail view | ~200 items in grid without thumbnails |
| Synchronous image decode on UI thread | Visible frame drops when scrolling | Async image loading with placeholder; bounded LRU image cache | ~50 items visible simultaneously |
| No index on `roms.platform_id` and `roms.title` | Filtering/search slow as collection grows | Add DB indexes on all filterable/searchable columns during schema creation | ~10,000 ROMs |
| Re-scanning unchanged directories on every startup | Startup takes minutes for large collections | Track directory `LastWriteTime` and file count; skip scan if unchanged | Any collection >500 ROMs on slow storage |
| Hash all ROMs synchronously during scan | Scan takes hours for large collections | Hash files lazily (only when needed for scraping); skip already-hashed files; run hashing concurrently with I/O limit |  ~500 ROMs on HDD |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Pass ROM path directly to emulator without sanitization | Path traversal or argument injection if ROM filename contains shell metacharacters | Use `ProcessStartInfo.ArgumentList` (array-based args) instead of string concatenation; never build CLI strings with user data via `string.Format` |
| Store API credentials (ScreenScraper password, IGDB client secret) in plaintext config file | Credential exposure if config is accidentally shared or backed up to cloud | Use OS credential store (Windows DPAPI / Linux Secret Service via `libsecret`); never store secrets in `appsettings.json` |
| Download emulator binaries without checksum verification | Supply chain attack: malicious binary installed silently | Verify SHA256 of downloaded binary against a pinned, community-maintained checksum list before installing |
| Execute emulator process with app's full privilege level | Malicious ROM could exploit emulator with elevated privileges | Launch emulator process at same or lower privilege; consider sandboxing on Linux (Flatpak provides this automatically) |
| Trust `rom_path` from database without existence check | Misleading error on stale paths; potential for path confusion bugs | Always verify path existence before launching; surface "file not found" clearly |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Progress shown as spinner only during long scan | User has no idea if it's working or hung; may force-quit | Show files-scanned counter, current directory, and estimated time remaining |
| "Scraping metadata" blocks browsing | User cannot browse their already-known games while scraping continues | Scraping runs fully in background; show "metadata pending" badge on unscraped games; browsing is always available |
| No "game is already running" state | User re-launches same game, opens duplicate emulator instance | Track launched process; disable launch button while game is running; show "Now Playing" indicator |
| Fullscreen mode has no way to get back to library without controller | User launches a game, emulator exits, they're stuck in app fullscreen with no visible exit | Always provide a controller-navigable "back to library" action; never require mouse/keyboard to exit fullscreen mode |
| Metadata mismatch shown as nothing (empty fields) | User sees games with no cover art, no description, no genre — looks broken | Show "no metadata found" placeholder with a manual search/override button; distinguish "not scraped yet" from "scraped but nothing found" |
| Cache deletion without confirmation or undo | User accidentally triggers cleanup, loses cached games they were about to play | Always show what will be deleted, require confirmation, provide 30-second undo |
| Settings are desktop-window-optimized | On Steam Deck, tiny text, no controller navigation in settings | Design settings screen with controller navigation from the start; test on 7-inch 1280x800 screen |

---

## "Looks Done But Isn't" Checklist

- [ ] **Controller navigation:** Every screen tested with only a gamepad — no keyboard/mouse needed. Back/B button works on every modal and popover. Focus never gets stuck on an invisible element.
- [ ] **Emulator launch:** Tested with at least one Windows binary, one Linux Flatpak, and RetroArch with a core. Process exit is detected. Error from emulator surfaces to user.
- [ ] **Metadata scraping:** HTTP 429 and HTTP 503 responses handled with backoff. Daily quota checked. Results cached. Scraping can be cancelled mid-scan.
- [ ] **ROM scanner:** Can be cancelled and restarted. Handles permission-denied directories gracefully. Network path unavailability is a warning, not a crash.
- [ ] **Cache management:** `last_launched_at` persists across app restarts. Cleanup preview shows exact files to be deleted. Cleanup cannot be triggered while a game is running.
- [ ] **Image loading:** Grid with 500+ items scrolls at 60fps on Steam Deck hardware. Memory usage stays below 500MB in grid view.
- [ ] **Database migrations:** Migration runs on first launch after upgrade. Pre-existing data (favorites, last played) survives migration. Migration failure shows a clear error, not a crash.
- [ ] **Cross-platform fonts:** Body text renders correctly on Linux with no custom font fallback required. Icons render on both platforms.
- [ ] **Fullscreen mode on Linux:** `WindowState.FullScreen` works under KDE Plasma, Gamescope (Steam Deck), and standard X11.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Controller navigation is retrofitted | HIGH | Requires redesigning focus scope hierarchy across all views; likely 1–2 sprint regressions |
| Emulator CLI templates hardcoded in binary | MEDIUM | Extract to JSON data file, add version field, ship data file as embedded resource with upgrade path |
| Rate-limited/banned from ScreenScraper | LOW (technical) / MEDIUM (time) | Implement throttling; wait out ban (24–48h); consider fallback to IGDB while banned |
| Schema migration failure in production | HIGH | Requires shipping a migration repair tool or manual SQL instructions; users may lose metadata if not handled carefully |
| Image loading performance collapse | MEDIUM | Add thumbnail generation pipeline as a background job; requires schema addition (thumbnail_path column) and re-processing of existing cover art |
| Cache never cleaned (disk full on Steam Deck) | MEDIUM | Ship emergency cleanup tool as separate command-line utility; add disk-full detection at launch time |
| Flatpak launch model not abstracted | MEDIUM | Requires adding `LaunchType` discriminator to emulator profile model and updating all profiles; launch code changes but data model change may need migration |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Controller navigation as afterthought | Phase 1: Foundation / Core UI | Every view spec includes "default focus element" and "Back action" in acceptance criteria |
| Emulator CLI instability | Phase 3: Emulator Integration | Emulator profile schema includes `version_constraint`; launch logs the exact command; user-editable template escape hatch exists |
| ROM scanner blocks UI | Phase 2: ROM Scanning | Scanner benchmark test: 10,000 dummy files scanned without UI freeze; cancellation token verified |
| Metadata scraping rate limits | Phase 4: Metadata Scraping | Rate-limited client unit tested with mock HTTP 429; response cache verified to persist across sessions |
| Avalonia Windows vs. Linux differences | Phase 1: Foundation | Linux CI runner configured before first UI component is merged |
| Image loading performance | Phase 5: Browse UI | Grid view benchmark: 500+ items, 60fps scroll on Steam Deck-class hardware (AMD iGPU) |
| Cache management silent failures | Phase 6: Cache Management | Integration test with mocked disk quota; `last_launched_at` persistence verified across restart |
| Flatpak launch model | Phase 3: Emulator Integration | Flatpak detection and launch tested on Linux runner; `LaunchType` enum present in schema |
| SQLite schema migration gaps | Phase 1: Foundation | Migration framework configured; migration-from-v0 test present in test suite before any feature uses the DB |

---

## Sources

- Training data from known ROM manager projects: Playnite (C#/WPF), EmulationStation-DE (C++/SDL), LaunchBox (C#/WPF), RetroArch (C/SDL) — MEDIUM confidence
- Avalonia UI known Linux/Wayland behavior: documented in Avalonia GitHub issues and community discussions as of training cutoff (August 2025) — MEDIUM confidence
- ScreenScraper API behavior (rate limits, hash-based lookup preference): documented in ScreenScraper's own API docs and widely reported in scraper tool communities — MEDIUM confidence
- Flatpak filesystem sandbox behavior: documented in Flatpak official docs — HIGH confidence on the model; specific emulator Flatpak behavior is MEDIUM confidence
- SQLite `ALTER TABLE` limitations: documented in official SQLite docs — HIGH confidence
- Steam Deck / Gamescope compositor quirks: documented in Valve's Steam Deck developer resources and community reports — MEDIUM confidence

**Note:** All findings are based on training data (cutoff August 2025). No live web verification was possible due to tool restrictions in this session. Recommend validating the following before acting:
1. Current ScreenScraper API rate limit values (may have changed)
2. Current Avalonia version's Wayland/Gamescope support status
3. IGDB token expiry mechanics (Twitch API specifics)

---
*Pitfalls research for: Cross-platform ROM browser / manager / launcher (YARL)*
*Researched: 2026-03-18*
