---
phase: 4
slug: emulator-integration-and-launch
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-30
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit 2.9.3 + NSubstitute 5.3.0 |
| **Config file** | none (discovered by xunit runner) |
| **Quick run command** | `dotnet test tests/YARL.Tests/ --filter "Category=Phase4" --no-build` |
| **Full suite command** | `dotnet test tests/YARL.Tests/ --no-build` |
| **Estimated runtime** | ~15 seconds (quick), ~30 seconds (full) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/YARL.Tests/ --filter "Category=Phase4" --no-build`
- **After every plan wave:** Run `dotnet test tests/YARL.Tests/ --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 4-01-01 | 01 | 1 | EMU-01 | unit | `dotnet test --filter "Category=Phase4&Class=EmulatorConfigTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-01-02 | 01 | 1 | EMU-01 | unit | `dotnet test --filter "Category=Phase4&Class=EmulatorRowViewModelTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-01-03 | 01 | 1 | EMU-01 | unit | `dotnet test --filter "Category=Phase4&Class=EmulatorRowViewModelTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-02-01 | 02 | 2 | EMU-02 | unit | `dotnet test --filter "Category=Phase4&Class=GameLaunchServiceTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-02-02 | 02 | 2 | EMU-02 | unit | `dotnet test --filter "Category=Phase4&Class=GameLaunchServiceTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-02-03 | 02 | 2 | EMU-02 | unit | `dotnet test --filter "Category=Phase4&Class=LaunchOverlayViewModelTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-02-04 | 02 | 2 | EMU-02 | unit | `dotnet test --filter "Category=Phase4&Class=GameLaunchServiceTests" --no-build` | ❌ W0 | ⬜ pending |
| 4-02-05 | 02 | 2 | EMU-02 | unit | `dotnet test --filter "Category=Phase4&Class=GameLaunchServiceTests" --no-build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/YARL.Tests/Phase4/EmulatorConfigTests.cs` — AppConfigService JSON round-trip for EMU-01 config persistence
- [ ] `tests/YARL.Tests/Phase4/EmulatorRowViewModelTests.cs` — path validity indicator (File.Exists mock), IsFlatpakAvailable=false on Windows
- [ ] `tests/YARL.Tests/Phase4/GameLaunchServiceTests.cs` — ProcessStartInfo arg building (native + Flatpak), rompath quoting, DB update after process exit, Failed state on non-zero exit
- [ ] `tests/YARL.Tests/Phase4/LaunchOverlayViewModelTests.cs` — state machine transitions: Hidden → Launching → Running → Hidden on exit, Failed on error

*All four files must be created as Wave 0 before any Phase 4 implementation tasks begin.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Flatpak launches emulator with ROM visible inside sandbox | EMU-02 | Requires actual Flatpak runtime + emulator installed on Linux | On Linux: tick Flatpak, enter valid AppID, add `--filesystem=host`, click Play — emulator opens and loads ROM |
| SetForegroundWindow brings emulator to front on Windows | EMU-02 | Requires real process with a window | On Windows: launch game, switch focus away, click "Switch to Game" — emulator window comes to foreground |
| Live play-time counter increments each second in overlay | EMU-02 | Requires real process running | Launch any game, verify ⏱ counter ticks each second in Running overlay state |
| Overlay auto-dismisses when emulator closes normally | EMU-02 | Requires real process lifecycle | Launch game, close emulator normally — YARL overlay disappears automatically |
| Play button inline prompt when no emulator configured | EMU-01 | Requires full UI integration | Open game detail for a platform with no emulator set — drawer shows "Configure emulator" message with navigation button |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
