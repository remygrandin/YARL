---
phase: 1
slug: foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + Avalonia.Headless.XUnit |
| **Config file** | tests/YARL.Tests/YARL.Tests.csproj (Wave 0 installs) |
| **Quick run command** | `dotnet test tests/YARL.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test tests/YARL.Tests` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/YARL.Tests --filter "Category!=Integration"`
- **After every plan wave:** Run `dotnet test tests/YARL.Tests`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 1 | — | build | `dotnet build src/YARL` | ❌ W0 | ⬜ pending |
| 01-01-02 | 01 | 1 | — | build | `dotnet build src/YARL` | ❌ W0 | ⬜ pending |
| 01-02-01 | 02 | 1 | — | unit | `dotnet test tests/YARL.Tests --filter "EfCoreMigration"` | ❌ W0 | ⬜ pending |
| 01-03-01 | 03 | 1 | — | unit | `dotnet test tests/YARL.Tests --filter "DualShell"` | ❌ W0 | ⬜ pending |
| 01-04-01 | 04 | 1 | LIB-08 | unit | `dotnet test tests/YARL.Tests --filter "RomSourceProvider"` | ❌ W0 | ⬜ pending |
| 01-05-01 | 05 | 1 | — | ci | `gh workflow run ci.yml` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/YARL.Tests/YARL.Tests.csproj` — xUnit test project with Avalonia.Headless.XUnit
- [ ] `tests/YARL.Tests/GlobalUsings.cs` — shared test imports
- [ ] xUnit + FluentAssertions packages installed

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| App launches on Linux | — | Requires Linux environment | Run `dotnet run --project src/YARL` on a Linux machine or Steam Deck |
| Gamepad auto-detection | — | Requires physical controller | Connect controller, verify app starts in fullscreen mode |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
