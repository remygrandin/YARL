# Phase 4: Emulator Integration and Launch — Context

**Gathered:** 2026-03-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Make the Play button functional: users configure one emulator (exe + args) per platform in Settings, click Play on any game, and it launches in the correct emulator. The app records launch time, accumulates total play time, and shows a full-page launch overlay while the game runs. Covers both Windows native binaries and Linux Flatpak emulators.

Multi-emulator selection, recommended defaults, and one-click emulator install are Phase 7.

</domain>

<decisions>
## Implementation Decisions

### Emulator Config Model

- **One emulator per platform** — each platform has a single configured emulator entry (exe path + args template). Phase 7 adds multi-emulator support and recommended defaults.
- **Stored in AppConfig JSON** — same pattern as ScreenScraper credentials. A `Dictionary<string, EmulatorConfig>` property keyed by platform ID (e.g. `"snes"`, `"n64"`). No DB migration needed.
- **EmulatorConfig shape**: `{ ExePath: string, Args: string, IsFlatpak: bool }` — exe and args are separate properties so updating the exe path does not erase the args.
- **Default args**: new rows pre-filled with `{rompath}` as the args value. User overwrites if needed.

### Launch Args Template

- **`{rompath}` placeholder** — the args field is a user-authored string with `{rompath}` where the ROM file path should be injected. YARL substitutes the full absolute path at launch time.
  - Example: `-L ~/.config/retroarch/cores/snes9x_libretro.so {rompath}` for RetroArch
  - Example: `{rompath}` for Dolphin, DuckStation, PCSX2
- **No built-in presets in Phase 4** — user fills exe and args manually. Phase 7 adds recommended emulator defaults with auto-fill.

### Process Lifecycle and Launch Overlay

- **Track process**: YARL holds the `Process` handle after launching. The process is monitored for exit.
- **`LastPlayedAt`**: updated twice — once immediately on launch, once on process exit (to have the final timestamp).
- **`TotalPlayTime`** (new per-game field): accumulated duration. Incremented by (exit time − launch time) each session.
- **Launch overlay** — full-page overlay that covers YARL's main content when a game is launched. Three states:
  1. **Launching** (first few seconds): blurred cover art background, spinner, "Launching [Game Title]…"
  2. **Running**: cover art background, game title, live play-time counter (⏱ 0:04:32), `[Switch to Game]` + `[Dismiss]` buttons
  3. **Failed** (process never started or exited immediately): error message + `[Close]` button
- **Auto-dismiss**: overlay auto-dismisses when the process exits from Running state.
- **Switch to Game**: brings the emulator window to focus; the overlay stays open behind it (acts as a background dashboard). User can alt-tab back.
- **Dismiss**: manually closes the overlay regardless of game state. Does not kill the process.
- **Multi-window emulators** (Switch, NDS, RPCS3 launcher): these spawn multiple windows or a launcher before the game. Tracking by the initial process handle is still correct; note as a known edge case to test.
- **Emulator takes focus** by default when launched. YARL stays open in the background, not minimized.

### Settings UX — EMULATORS Section

- **Show only platforms the user has games for** — rows are dynamically generated from the library's known platforms. Plus a `[ + Add platform ]` button that opens a dropdown of all platforms from `platforms.json` (allows pre-configuring before adding ROMs).
- **Config rows persist** — when a platform's games are removed from the library, its emulator config row stays visible (config is preserved).
- **Row layout per platform**:
  - Platform name + platform theme color accent
  - Exe path: text field (editable) + 📂 browse button (file picker). Browse button hidden when Flatpak checkbox is ticked.
  - Args: text field, pre-filled with `{rompath}`
  - Flatpak checkbox: "Flatpak app" — when ticked, hides the browse button; disabled/greyed on Windows (no Flatpak available). Shows a yellow warning: "Flatpak emulators may not be able to read ROM files. Add `--filesystem=host` to your args if needed."
  - Path validity indicator: ✅ / ⚠️ shown inline after the exe field — checks file existence only (no process spawn). A path that fails the check can still be saved.
  - `[ Save ]` button per row — saves only that platform's config to AppConfig. Exe and args are saved as separate fields.
- **Unconfigured platform** — exe field empty, shown with a subtle "not configured" hint text.
- **On-demand prompt** — if user clicks Play on a game whose platform has no emulator configured, the detail drawer shows an inline message: "No emulator configured for [Platform]. Go to Settings → Emulators." with a direct navigation button.

### Flatpak Handling

- **Explicit Flatpak checkbox** per emulator row — user explicitly marks an emulator as Flatpak.
  - Checkbox is disabled (greyed out) on Windows — Flatpak is Linux-only.
  - When checked: browse button is hidden (Flatpak uses app ID, not a file path); exe field becomes a plain text field for the app ID (e.g. `org.DolphinEmu.dolphin-emu`).
- **YARL builds the launch command** for Flatpak: `flatpak run [AppID] [args with {rompath} substituted]`
- **Filesystem warning in Settings**: when Flatpak is ticked, display a yellow info box: "Flatpak sandboxes may block access to ROM files. If launch fails, add `--filesystem=host` to your args."
- **No auto-injection of `--filesystem=host`** — the user adds it to their args if needed.

### Claude's Discretion

- Live play-time counter tick interval (e.g. every second or every 10 seconds)
- Exact overlay animation (fade-in, slide-up, etc.)
- Error message copy for failed launches
- How to handle the process monitor when YARL itself is closed while a game is running (likely: just let it go, update LastPlayedAt on next launch if needed)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — EMU-01, EMU-02 are the phase requirements. All others pending.

### Existing infrastructure to extend
- `src/YARL/Infrastructure/Config/AppConfig.cs` — Add `EmulatorConfigs: Dictionary<string, EmulatorConfig>` property here
- `src/YARL/Infrastructure/Config/AppPaths.cs` — AppConfig JSON location
- `src/YARL/Data/platforms.json` — Platform IDs and names for the Settings emulator section; `retroarchCoreId` already present per platform
- `src/YARL/UI/Views/SettingsView.axaml` — Existing Settings layout; add new EMULATORS section here
- `src/YARL/UI/ViewModels/SettingsViewModel.cs` — Existing SettingsViewModel; add emulator config properties and commands here
- `src/YARL/UI/Views/GameDetailDrawer.axaml` — Play button is currently a no-op; wire LaunchCommand here
- `src/YARL/UI/ViewModels/GameDetailViewModel.cs` — Add LaunchCommand here
- `src/YARL/Domain/Models/Game.cs` — Add `TotalPlayTime` (TimeSpan) field here

No external specs — requirements are fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AppConfig` + `AppConfigService.Save()` — proven pattern for JSON-persisted user settings; add `EmulatorConfigs` dict to AppConfig, same save flow as ScreenScraper credentials
- `SettingsView.axaml` — ScrollViewer + StackPanel sections already established; add a new EMULATORS section with the same `Border + StackPanel` card pattern
- `SettingsView.axaml.cs` — code-behind has `OnAddSourceClicked` pattern using Avalonia file/folder pickers; reuse `StorageProvider.OpenFilePickerAsync` for the exe browse button
- `GameDetailDrawer.axaml` — Play button already exists (no-op tooltip), bind `LaunchCommand` from `GameDetailViewModel`
- `LibraryViewModel.SelectedGame` / `GameDetailViewModel.Game` — already carries the full `GameViewModel` including `PlatformId`; use to look up emulator config at launch time

### Established Patterns
- Per-section Save button: established in ScreenScraper credentials section — keep the same `[ Save ]` per row pattern
- `[Reactive]` properties + `ReactiveCommand` in ViewModels — all existing VMs follow this pattern
- `IServiceScopeFactory` for DB access — pass scope factory to new `GameLaunchService` (or add to existing `GameDetailViewModel`)
- `Dispatcher.UIThread.Post(...)` for background→UI updates — use for live play-time counter increments

### Integration Points
- `GameDetailViewModel` — LaunchCommand goes here; needs access to `AppConfig` (emulator lookup) and `IServiceScopeFactory` (to update DB play time)
- `SettingsViewModel` — EmulatorConfig list, per-row Save commands, path-validity checker
- `AppConfig` — new `EmulatorConfigs` dictionary property; `AppConfigService.Save()` persists it
- `Game` entity + EF Core migration — `TotalPlayTime` (TimeSpan stored as ticks or seconds in SQLite)
- `PlatformRegistry` — already resolves platform name from ID; use in Settings to display platform names in the emulator section

</code_context>

<specifics>
## Specific Ideas

- The launch overlay is a **full-page overlay** on top of YARL's main content — not a dialog, not a drawer. Cover art as blurred background image.
- The play-time counter in the Running state ticks live (like a stopwatch): `⏱ Playing — 0:04:32`
- Flatpak checkbox is a first-class UI element per row, not a hidden option — makes it discoverable for Steam Deck users.
- "Switch to Game" button: brings the emulator window to front without closing the overlay (overlay stays as a background dashboard the user can alt-tab back to).
- The inline "not configured" prompt in the detail drawer navigates directly to Settings → Emulators (not just to Settings root).
- Path validity check: only checks `File.Exists(exePath)` (or `Directory.Exists` for Flatpak app IDs, which don't map to files) — no test launch.

</specifics>

<deferred>
## Deferred Ideas

- Multi-emulator selection per platform (dropdown to pick between configured emulators for a game) — Phase 7
- Recommended emulator presets / auto-fill for known emulators — Phase 7
- One-click emulator install — Phase 7
- Core auto-detection for RetroArch (scan installed cores and suggest) — Phase 7
- Per-game emulator override (override which emulator is used for a specific game) — Phase 7 or later
- Session history log (list of all play sessions with duration) — future phase

</deferred>

---

*Phase: 04-emulator-integration-and-launch*
*Context gathered: 2026-03-30*
