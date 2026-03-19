---
phase: 03-metadata-and-cover-art
plan: 02
subsystem: infrastructure
tags: [screenscraper, igdb, http-client, nuget, art-cache, scraper-pipeline, tdd, nsubstitute]

# Dependency graph
requires:
  - phase: 03-metadata-and-cover-art/03-01
    provides: Game entity with metadata fields, ScrapeStatus enum, platforms.json with screenscraperSystemId, Wave 0 test stubs

provides:
  - IMetadataScraper interface (SearchByCrc32Async, SearchByTitleAsync)
  - ScrapedGameData record (metadata DTO with RegionalName)
  - ScreenScraperClient: hand-rolled HTTP client for ScreenScraper API v2 (jeuInfos.php, CRC32 + title search, box-2D cover art selection)
  - IgdbClient: IGDB APIv4 wrapper via NuGet SDK (title search, cover URL normalization)
  - ScraperPipeline: CRC32 → ScreenScraper → title fallback → IGDB → unmatched orchestration with IsMetadataOverridden guard
  - ArtCacheService: downloads and caches cover art to {ArtCacheDir}/{platformId}/{gameId}.ext
  - 10 passing tests (4 ScreenScraperClientTests + 6 ScraperPipelineTests)
  - 4 NuGet packages: Avalonia.Controls.ItemsRepeater 11.1.5, AsyncImageLoader.Avalonia 3.7.0, IGDB 6.1.0, Microsoft.Extensions.Http.Resilience 10.4.0

affects:
  - 03-03 (ScraperHostedService uses ScraperPipeline as its core engine)
  - 03-04 (metadata override service interacts with the same pipeline IsMetadataOverridden guard)
  - 03-05 (LibraryViewModel search/filter references Genre, Developer, ReleaseYear populated by pipeline)
  - 03-06 (cover art display uses CoverArtPath set by ArtCacheService)

# Tech tracking
tech-stack:
  added:
    - Avalonia.Controls.ItemsRepeater 11.1.5 (virtualized grid layout for 500+ tile scrolling)
    - AsyncImageLoader.Avalonia 3.7.0 (off-thread image loading with LRU memory cache)
    - IGDB 6.1.0 (kamranayub/igdb-dotnet SDK — handles OAuth2 token management automatically)
    - Microsoft.Extensions.Http.Resilience 10.4.0 (Polly-based retry + exponential backoff for HttpClient)
  patterns:
    - ScreenScraper API v2 parsed with System.Text.Json.JsonDocument (hand-rolled, no library exists)
    - Box-2D cover art selection: region priority us > eu > wor > jp, fallback to screenshot (ss type)
    - IGDB cover URL normalization: prepend "https:" when URL starts with "//" (protocol-relative)
    - ScraperPipeline accepts IMetadataScraper interface for both SS and IGDB params — enables NSubstitute mocking in tests
    - ArtCacheService uses real HTTP client with fake handler in tests (FakeImageHandler sealed inner class)
    - FakeMessageHandler pattern: sealed inner class extending HttpMessageHandler — avoids NSubstitute protected method limitation

key-files:
  created:
    - src/YARL/Infrastructure/Scraping/IMetadataScraper.cs
    - src/YARL/Infrastructure/Scraping/ScrapedGameData.cs
    - src/YARL/Infrastructure/Scraping/ScreenScraperClient.cs
    - src/YARL/Infrastructure/Scraping/IgdbClient.cs
    - src/YARL/Infrastructure/Scraping/ScraperPipeline.cs
    - src/YARL/Infrastructure/Images/ArtCacheService.cs
    - tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs (implemented, 4 passing)
    - tests/YARL.Tests/Phase3/ScraperPipelineTests.cs (implemented, 6 passing)
  modified:
    - src/YARL/YARL.csproj (4 new NuGet packages)
    - tests/YARL.Tests/Phase3/MetadataOverrideTests.cs (reverted 3 broken stubs back to skip)

key-decisions:
  - "ScraperPipeline accepts two IMetadataScraper params (not ScreenScraperClient/IgdbClient concrete types) to enable clean NSubstitute mocking in tests without needing concrete class instantiation"
  - "FakeMessageHandler sealed inner class pattern used for HttpClient mocking — NSubstitute cannot mock HttpMessageHandler.SendAsync (protected method) directly"
  - "IGDB FirstReleaseDate is already DateTimeOffset? (not Unix timestamp) — SDK deserializes it correctly, no conversion needed"

patterns-established:
  - "FakeMessageHandler: sealed class extending HttpMessageHandler with overridden SendAsync — use this pattern for all HTTP client tests in this codebase"
  - "IMetadataScraper: both SourceName string property + CRC32 + title search methods — all scrapers must implement this interface"
  - "ScreenScraper localized text extraction: prefer us/eu/wor/ss for region, en/fr/de for langue — consistent order across all fields"

requirements-completed: [META-01, META-02, META-03]

# Metrics
duration: 9min
completed: 2026-03-19
---

# Phase 3 Plan 02: Scraping Infrastructure Summary

**ScreenScraperClient (hand-rolled HTTP + box-2D art selection), IgdbClient (NuGet SDK wrapper), ScraperPipeline (CRC32 → SS → IGDB → unmatched), and ArtCacheService with 10 passing TDD tests**

## Performance

- **Duration:** ~9 min
- **Started:** 2026-03-19T20:19:39Z
- **Completed:** 2026-03-19T20:28:46Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Built ScreenScraperClient: hand-rolled `HttpClient` wrapper for ScreenScraper API v2, with CRC32 hash and title lookup, box-2D cover art selection with region priority (us > eu > wor > jp), and full JSON parsing of jeu element
- Built IgdbClient: thin wrapper around IGDB NuGet SDK v6.1.0, title search returning first result, cover URL https: normalization for protocol-relative URLs, developer/publisher extraction from InvolvedCompany
- Built ScraperPipeline: CRC32 → ScreenScraper → title fallback on SS → IGDB title → unmatched, with `IsMetadataOverridden` guard protecting user data from re-scrape
- Built ArtCacheService: downloads cover art to `{ArtCacheDir}/{platformId}/{gameId}.ext`, handles URI extension extraction and graceful error logging
- Installed 4 NuGet packages needed for Phase 3: ItemsRepeater (virtualized grid), AsyncImageLoader (off-thread image decode), IGDB SDK, Http.Resilience (Polly retry)
- 10 TDD tests all passing: 4 ScreenScraperClientTests (CRC32 hit, no match, title search, box-2D preference) + 6 ScraperPipelineTests (all pipeline paths)

## Task Commits

Each task was committed atomically:

1. **Task 1: NuGet packages, IMetadataScraper, ScreenScraperClient, IgdbClient** - `3570ac5` (feat)
2. **Task 2: ScraperPipeline orchestration and ArtCacheService** - `3f34f20` (feat)

**Plan metadata:** _(docs commit hash added after state update)_

## Files Created/Modified

- `src/YARL/YARL.csproj` - Added 4 NuGet packages
- `src/YARL/Infrastructure/Scraping/IMetadataScraper.cs` - Shared interface for all scraper clients
- `src/YARL/Infrastructure/Scraping/ScrapedGameData.cs` - Metadata DTO record with RegionalName
- `src/YARL/Infrastructure/Scraping/ScreenScraperClient.cs` - Hand-rolled ScreenScraper API v2 HTTP client
- `src/YARL/Infrastructure/Scraping/IgdbClient.cs` - IGDB NuGet SDK wrapper with cover URL normalization
- `src/YARL/Infrastructure/Scraping/ScraperPipeline.cs` - Orchestrates the full matching pipeline
- `src/YARL/Infrastructure/Images/ArtCacheService.cs` - Downloads and caches cover art to local filesystem
- `tests/YARL.Tests/Phase3/ScreenScraperClientTests.cs` - 4 passing tests (Wave 0 stubs implemented)
- `tests/YARL.Tests/Phase3/ScraperPipelineTests.cs` - 6 passing tests (Wave 0 stubs implemented)
- `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs` - Reverted 3 broken stubs referencing unimplemented GameDetailViewModel

## Decisions Made

- **ScraperPipeline uses IMetadataScraper for both constructor params** (not concrete ScreenScraperClient/IgdbClient): plan recommended this explicitly; enables clean NSubstitute mocking without needing to construct concrete classes with HTTP clients
- **FakeMessageHandler sealed inner class** for test HTTP mocking: NSubstitute cannot mock `HttpMessageHandler.SendAsync` (protected method); a simple sealed subclass is the correct pattern
- **IGDB FirstReleaseDate is already DateTimeOffset?**: The SDK deserializes it from Unix timestamp internally — no manual `DateTimeOffset.FromUnixTimeSeconds()` conversion needed (verified via runtime reflection)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed MetadataOverrideTests 3 broken stubs referencing GameDetailViewModel**
- **Found during:** Task 2 (build verification before running tests)
- **Issue:** MetadataOverrideTests.cs was filled in from a previous session with references to `GameDetailViewModel` (not yet implemented) and accidentally included a `using YARL.UI.ViewModels` — caused 3 compile errors blocking the test build
- **Fix:** Reverted the 3 broken tests back to `[Fact(Skip = "Wave 0 stub — GameDetailViewModel not yet implemented (Plan 05)")]` stubs, keeping the 1 functional test that doesn't use the missing type
- **Files modified:** `tests/YARL.Tests/Phase3/MetadataOverrideTests.cs`
- **Verification:** Test build succeeds (0 errors), full suite 55 passing + 3 skipped
- **Committed in:** `3f34f20` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug in pre-existing test file)
**Impact on plan:** Fix was required to unblock the build. No scope creep. Stub tests will be filled in when GameDetailViewModel is implemented in Plan 05.

## Issues Encountered

- NSubstitute cannot mock `HttpMessageHandler.SendAsync` because it is a `protected` method. Used a sealed `FakeMessageHandler : HttpMessageHandler` inner class as the correct substitute pattern. This is now the established pattern for HTTP tests in this codebase.
- IGDB SDK `QueryAsync<T>` is generic but was listed in reflection output without `<T>` suffix — confirmed via runtime reflection that `QueryAsync<Game>(endpoint, query)` is the correct call signature.

## User Setup Required

**External services require manual configuration before scraping will produce results.**

ScreenScraper and IGDB credentials must be provided via environment variables or configuration:

| Variable | Source |
|---|---|
| `YARL_SS_DEVID` | Register developer application at https://screenscraper.fr |
| `YARL_SS_DEVPASSWORD` | Register developer application at https://screenscraper.fr |
| `YARL_SS_USERID` | Optional — personal ScreenScraper user account |
| `YARL_SS_USERPASSWORD` | Optional — personal ScreenScraper user account |
| `YARL_IGDB_CLIENT_ID` | Twitch Developer Console → Applications → Client ID |
| `YARL_IGDB_CLIENT_SECRET` | Twitch Developer Console → Applications → Client Secret |

The scraping infrastructure will compile and tests will pass without these credentials (mocked HTTP in tests). Credentials are only needed at runtime when making real API calls.

## Next Phase Readiness

- Scraping infrastructure is complete: IMetadataScraper interface + ScreenScraperClient + IgdbClient + ScraperPipeline + ArtCacheService
- ScraperHostedService (Plan 04) can consume ScraperPipeline directly via DI injection
- DI registration of ScreenScraperClient, IgdbClient, ScraperPipeline, ArtCacheService needed in Program.cs (Plan 04)
- AsyncImageLoader.Avalonia and Avalonia.Controls.ItemsRepeater packages ready for Plan 05/06 UI work
- No blockers for Plan 03-03 (metadata override service)

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-19*
