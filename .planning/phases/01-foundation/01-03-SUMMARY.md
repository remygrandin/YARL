---
phase: 01-foundation
plan: 03
subsystem: testing
tags: [xunit, avalonia-headless, nsubstitute, ef-core, sqlite, github-actions, ci]

# Dependency graph
requires:
  - phase: 01-02
    provides: YarlDbContext, RomSource domain model, MainViewModel/LibraryViewModel, App class, EF Core migrations

provides:
  - xUnit test project with Avalonia.Headless.XUnit 11.3.12 and NSubstitute
  - 11 foundation tests: LIB-08 OsMounted persistence (4), EF Core migration smoke (2), ViewModel sharing (5)
  - GitHub Actions CI pipeline running on ubuntu-latest + windows-latest
  - .gitignore for .NET, IDE, OS, and YARL-specific files

affects: [all-future-phases, phase-02-library, phase-03-metadata]

# Tech tracking
tech-stack:
  added:
    - Avalonia.Headless.XUnit 11.3.12
    - NSubstitute 5.3.0
    - Microsoft.EntityFrameworkCore.Sqlite 10.0.5 (in test project)
    - xUnit 2.9.3
    - coverlet.collector 6.0.4
  patterns:
    - In-memory SQLite with OpenConnection() + EnsureCreated() for persistence tests
    - In-memory SQLite with OpenConnection() + MigrateAsync() for migration tests
    - AvaloniaTestApplication assembly attribute on TestApp.cs for headless test runner
    - No Avalonia-specific test infrastructure needed for pure ViewModel tests

key-files:
  created:
    - tests/YARL.Tests/YARL.Tests.csproj
    - tests/YARL.Tests/GlobalUsings.cs
    - tests/YARL.Tests/TestApp.cs
    - tests/YARL.Tests/Foundation/RomSourceProviderTests.cs
    - tests/YARL.Tests/Foundation/MigrationTests.cs
    - tests/YARL.Tests/Foundation/ViewModelTests.cs
    - .github/workflows/ci.yml
    - .gitignore
  modified:
    - YARL.sln (added test project)

key-decisions:
  - "In-memory SQLite requires explicit OpenConnection() before EnsureCreated()/MigrateAsync() — connection is per-process lifetime for :memory: databases"
  - "xUnit project generated with net10.0 by default (dotnet new xunit) matching main project — no target framework edit needed"
  - "dotnet new xunit generates Using Include=Xunit implicit global using — GlobalUsings.cs is redundant but kept for explicit documentation"

patterns-established:
  - "Pattern: In-memory SQLite test context — OpenConnection() then EnsureCreated() for unit tests that bypass migrations"
  - "Pattern: Migration smoke tests use MigrateAsync() on :memory: SQLite to verify migrations run without errors"
  - "Pattern: ViewModel tests need no Avalonia test infrastructure — pure C# with ReactiveObject works headlessly"

requirements-completed: [LIB-08]

# Metrics
duration: 8min
completed: 2026-03-18
---

# Phase 01 Plan 03: Foundation Tests and CI Summary

**xUnit + Avalonia.Headless test project with 11 passing tests covering LIB-08 OsMounted persistence, EF Core migration integrity, and ViewModel sharing, plus cross-platform GitHub Actions CI on ubuntu-latest + windows-latest**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-18T23:11:14Z
- **Completed:** 2026-03-18T23:19:00Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Test project with Avalonia.Headless.XUnit 11.3.12, NSubstitute 5.3.0, and in-memory SQLite EF Core — all 11 tests pass
- LIB-08 verified: RomSource with SourceType.OsMounted persists, retrieves, and stores as string "OsMounted" in SQLite
- EF Core migration smoke test confirms MigrateAsync() creates RomSources and __EFMigrationsHistory tables on fresh DB
- ViewModel sharing tests confirm LibraryViewModel is the same instance across both shells via MainViewModel
- GitHub Actions CI matrix (ubuntu-latest + windows-latest) targeting .NET 10 — runs restore, build, test on every push/PR to main

## Task Commits

Each task was committed atomically:

1. **Task 1: Create test project with foundation tests** - `bf56b1e` (feat)
2. **Task 2: Create GitHub Actions CI workflow and .gitignore** - `b7496b4` (feat)

**Plan metadata:** _(pending final docs commit)_

## Files Created/Modified
- `tests/YARL.Tests/YARL.Tests.csproj` - xUnit test project with Avalonia.Headless.XUnit, NSubstitute, EF Core SQLite
- `tests/YARL.Tests/GlobalUsings.cs` - Global using for Xunit
- `tests/YARL.Tests/TestApp.cs` - AvaloniaTestApplication assembly attribute for headless runner
- `tests/YARL.Tests/Foundation/RomSourceProviderTests.cs` - 4 tests for LIB-08 OsMounted and Local persistence
- `tests/YARL.Tests/Foundation/MigrationTests.cs` - 2 tests verifying EF Core migration integrity
- `tests/YARL.Tests/Foundation/ViewModelTests.cs` - 5 tests verifying ViewModel sharing and PropertyChanged
- `.github/workflows/ci.yml` - Cross-platform CI on ubuntu-latest + windows-latest with .NET 10
- `.gitignore` - Standard .NET + IDE + OS + YARL app data entries
- `YARL.sln` - Modified to include test project

## Decisions Made
- In-memory SQLite requires explicit `OpenConnection()` before `EnsureCreated()` or `MigrateAsync()` for `:memory:` databases — connection lifetime is tied to the DbContext object.
- ViewModel tests (LibraryViewModel, MainViewModel) require no Avalonia headless infrastructure — ReactiveObject works in plain .NET context, making these tests fast and simple.
- dotnet new xunit generates `net10.0` by default, matching the main YARL project — no csproj edit required.

## Deviations from Plan

None - plan executed exactly as written. The xUnit project generated by `dotnet new xunit` already targeted `net10.0` so the explicit edit step in the plan was unnecessary (already correct).

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Foundation test suite proves domain model, persistence, migrations, and ViewModel layer are correct
- CI pipeline will catch Linux-specific Avalonia or platform issues on every push
- Phase 02 (ROM library scanning) can be built with confidence the foundation is solid
- No blockers

## Self-Check: PASSED

- FOUND: tests/YARL.Tests/YARL.Tests.csproj
- FOUND: tests/YARL.Tests/Foundation/RomSourceProviderTests.cs
- FOUND: .github/workflows/ci.yml
- FOUND: .gitignore
- FOUND: .planning/phases/01-foundation/01-03-SUMMARY.md
- FOUND: commit bf56b1e (feat: test project with foundation tests)
- FOUND: commit b7496b4 (feat: CI workflow and .gitignore)

---
*Phase: 01-foundation*
*Completed: 2026-03-18*
