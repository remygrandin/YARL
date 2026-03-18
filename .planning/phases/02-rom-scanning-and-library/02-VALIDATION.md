---
phase: 2
slug: rom-scanning-and-library
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Avalonia.Headless.XUnit 11.3.12 (from Phase 1) |
| **Config file** | `tests/YARL.Tests/YARL.Tests.csproj` |
| **Quick run command** | `dotnet test tests/YARL.Tests --filter "Category=Phase2" -q` |
| **Full suite command** | `dotnet test tests/YARL.Tests -q` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/YARL.Tests --filter "Category=Phase2" -q`
- **After every plan wave:** Run `dotnet test tests/YARL.Tests -q`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 20 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 2-01-01 | 01 | 1 | LIB-01 | unit | `dotnet test --filter "Category=Phase2&FullyQualifiedName~RomSource"` | ❌ W0 | ⬜ pending |
| 2-01-02 | 01 | 1 | — | unit | `dotnet build src/YARL -c Release` | ✅ existing | ⬜ pending |
| 2-02-01 | 02 | 2 | LIB-02 | unit | `dotnet test --filter "Category=Phase2&FullyQualifiedName~Scanner"` | ❌ W0 | ⬜ pending |
| 2-02-02 | 02 | 2 | LIB-02 | unit | `dotnet test --filter "Category=Phase2&FullyQualifiedName~Hash"` | ❌ W0 | ⬜ pending |
| 2-03-01 | 03 | 3 | LIB-03 | unit | `dotnet test --filter "Category=Phase2&FullyQualifiedName~Platform"` | ❌ W0 | ⬜ pending |
| 2-03-02 | 03 | 3 | LIB-06, LIB-07 | unit | `dotnet test --filter "Category=Phase2&FullyQualifiedName~Library"` | ❌ W0 | ⬜ pending |
| 2-04-01 | 04 | 4 | LIB-03 | build | `dotnet build src/YARL -c Release` | ✅ existing | ⬜ pending |
| 2-04-02 | 04 | 4 | LIB-03, LIB-06, LIB-07 | manual | Visual inspection | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/YARL.Tests/Phase2/ScannerTests.cs` — stub tests for LIB-01, LIB-02 (scanner registration, config, background non-blocking)
- [ ] `tests/YARL.Tests/Phase2/HashingTests.cs` — stub tests for CRC32/MD5/SHA1 hashing correctness
- [ ] `tests/YARL.Tests/Phase2/PlatformDetectionTests.cs` — stub tests for folder-name→platform matching, alias resolution, extension filtering
- [ ] `tests/YARL.Tests/Phase2/LibraryViewModelTests.cs` — stub tests for LIB-06 (favorites toggle), LIB-07 (recently played ordering)
- [ ] `tests/YARL.Tests/Phase2/IncrementalScanTests.cs` — stub tests for EF Core upsert: add new, mark missing, preserve existing metadata
- [ ] Add `System.IO.Hashing 10.0.4` to `tests/YARL.Tests/YARL.Tests.csproj` (if hash tests need it directly)

*Wave 0 test stubs should compile with `[Fact(Skip = "Wave 0 stub")]` — they fail until implementation lands.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Library browsable during active scan (live updates) | LIB-02 | Requires real filesystem + UI rendering | Add a folder with 500+ ROMs, start scan, scroll through library while scan runs — verify new platform tiles appear without UI freeze |
| Platform grid → game list navigation | LIB-03 | Visual/interactive | Click a platform tile, verify game list shows, navigate back |
| Recently Played carousel populates after launch | LIB-07 | Requires game launch (Phase 4) | Manually set LastPlayedAt in DB, restart app, verify carousel shows correct order |
| Favorites carousel and platform filter | LIB-06 | Visual/interactive | Mark 3 games as favorite, verify carousel + platform filter both show them |
| Scan progress status bar updates | LIB-02 | UI visual | Run scan, verify status bar text changes platform-by-platform |
| Scan can be cancelled mid-run | success criteria | Interactive | Start large scan, click cancel, verify scan stops and DB is in consistent state |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 20s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
