---
phase: 03
slug: metadata-and-cover-art
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 03 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit 2.9.3 + Avalonia.Headless.XUnit 11.3.12 |
| **Config file** | none — Wave 0 installs test stubs in existing YARL.Tests project |
| **Quick run command** | `dotnet test tests/YARL.Tests/ --filter "Category=Phase3" -x` |
| **Full suite command** | `dotnet test tests/YARL.Tests/ --logger "console;verbosity=normal"` |
| **Estimated runtime** | ~15 seconds (unit tests only, no network calls) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/YARL.Tests/ --filter "Category=Phase3" -x`
- **After every plan wave:** Run `dotnet test tests/YARL.Tests/ --logger "console;verbosity=normal"`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** ~15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | W0 | META-01 | unit | `dotnet test --filter "Category=Phase3&Class=ScreenScraperClientTests"` | ❌ W0 | ⬜ pending |
| 03-01-02 | 01 | W0 | META-01,02,03 | unit | `dotnet test --filter "Category=Phase3&Class=ScraperPipelineTests"` | ❌ W0 | ⬜ pending |
| 03-01-03 | 01 | W0 | META-04 | unit | `dotnet test --filter "Category=Phase3&Class=MetadataOverrideTests"` | ❌ W0 | ⬜ pending |
| 03-01-04 | 01 | W0 | LIB-04,05 | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | ❌ W0 | ⬜ pending |
| 03-02-01 | 02 | 1 | META-01 | unit | `dotnet test --filter "Category=Phase3&Class=ScreenScraperClientTests"` | ✅ W0 | ⬜ pending |
| 03-02-02 | 02 | 1 | META-02 | unit | `dotnet test --filter "Category=Phase3&Class=ScraperPipelineTests"` | ✅ W0 | ⬜ pending |
| 03-02-03 | 02 | 1 | META-03,04 | unit | `dotnet test --filter "Category=Phase3&Class=MetadataOverrideTests"` | ✅ W0 | ⬜ pending |
| 03-03-01 | 03 | 2 | LIB-04 | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | ✅ W0 | ⬜ pending |
| 03-03-02 | 03 | 2 | LIB-05 | unit | `dotnet test --filter "Category=Phase3&Class=LibraryViewModelPhase3Tests"` | ✅ W0 | ⬜ pending |
| 03-04-01 | 04 | 3 | META-01,02,03 | manual | See manual verification table | N/A | ⬜ pending |
| 03-04-02 | 04 | 3 | LIB-04,05 | manual | See manual verification table | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs` — stubs for META-01 (mocked HttpMessageHandler, CRC32 hash lookup response)
- [ ] `tests/YARL.Tests/Phase3/ScraperPipelineTests.cs` — stubs for META-01 (primary route), META-02 (IGDB fallback), META-03 (field population)
- [ ] `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs` — stubs for META-04 (override flag survives re-scrape)
- [ ] `tests/YARL.Tests/Phase3/LibraryViewModelPhase3Tests.cs` — stubs for LIB-04 (search debounce 250ms via TestScheduler), LIB-05 (genre/year/developer filter composition)
- [ ] NSubstitute already installed (YARL.Tests.csproj) — mock `IMetadataScraper`, `HttpMessageHandler`
- [ ] Avalonia.Headless.XUnit 11.3.12 already installed — available for UI tests if needed (not required for Phase 3 unit tests)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cover art grid renders 500+ tiles at 60fps with images visible | META-01/03 | Performance metric + visual | Load library with 500+ scraped games, scroll grid, confirm no stutter |
| Detail drawer slides in with animation from right | UI-SPEC | Visual animation timing | Click a game tile, confirm drawer slides in from right within ~200ms |
| Auto-scrape triggers after scan and fills cover art in grid tiles | META-01 | End-to-end integration | Add ROM source, scan, wait for scraper progress in Settings, confirm cover art appears in grid |
| Search filters results in real time while typing | LIB-04 | Visual/interactive | Type in search box, confirm list updates within 250ms debounce |
| Manual metadata edit survives a re-scrape | META-04 | End-to-end | Edit a field in detail drawer, mark as override, trigger re-scrape, confirm field unchanged |
| Interactive match dialog appears on ambiguous match | META-01 | UI flow | Trigger scrape on a ROM with no CRC32 match, confirm dialog with result list |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
