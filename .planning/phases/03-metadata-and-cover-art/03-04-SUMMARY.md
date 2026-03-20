---
phase: 03-metadata-and-cover-art
plan: "04"
subsystem: infra
tags: [background-service, di, channel, resilience, polly, igdb, screenscraper, reactiveui]

# Dependency graph
requires:
  - phase: 03-02
    provides: ScraperPipeline, ScreenScraperClient, IgdbClient, ArtCacheService
  - phase: 03-03
    provides: GameDetailViewModel
provides:
  - ScraperHostedService (BackgroundService, Channel<int> queue, auto-scrape after scan)
  - ScrapingProgress record (observable progress model)
  - ScrapingStatusViewModel (Settings panel progress binding + RescrapeLibraryCommand)
  - Phase 3 full DI registration in Program.cs
  - RomScanHostedService triggers auto-scrape via QueuePendingGamesAsync
affects: [04-emulator-launch, 05-fullscreen-ui]

# Tech tracking
tech-stack:
  added: [System.Threading.Channels, Microsoft.Extensions.Http.Resilience, Polly, Microsoft.Extensions.Configuration]
  patterns: [BackgroundService with Channel<int> queue, singleton hosted service pattern, DI lambda factory for action callback]

key-files:
  created:
    - src/YARL/Infrastructure/Scraping/ScrapingProgress.cs
    - src/YARL/Infrastructure/Scraping/ScraperHostedService.cs
    - src/YARL/UI/ViewModels/ScrapingStatusViewModel.cs
  modified:
    - src/YARL/Program.cs
    - src/YARL/Infrastructure/Scanning/RomScanHostedService.cs
    - src/YARL/Infrastructure/Scanning/PlatformDefinition.cs

key-decisions:
  - "ScraperHostedService registered as AddSingleton + AddHostedService(sp => sp.GetRequired<ScraperHostedService>()) pattern — allows the same singleton instance to be resolved both as IHostedService and directly for QueuePendingGamesAsync injection into RomScanHostedService"
  - "ScrapingStatusViewModel RescrapeLibraryCommand uses fire-and-forget lambda (() => _ = hostedService.QueueAllForRescrapeAsync()) — avoids async void while keeping the ReactiveCommand<Unit,Unit> signature"
  - "IConfiguration registered via ConfigurationBuilder.AddEnvironmentVariables() so ScreenScraperClient can read YARL_SS_* credentials from env vars without requiring appsettings.json"
  - "Polly using directive required for DelayBackoffType.Exponential and HttpRetryStrategyOptions — not included by Microsoft.Extensions.Http.Resilience implicit usings"

patterns-established:
  - "Hosted service singleton pattern: AddSingleton<T>() + AddHostedService(sp => sp.GetRequiredService<T>()) — register once, resolve by concrete type and as IHostedService"
  - "Channel<int>-based work queue in BackgroundService: ReadAllAsync consumes items as they arrive, Writer.WriteAsync enqueues from external callers"
  - "RescrapeLibraryCommand fire-and-forget: () => _ = asyncMethod() — correct pattern for ReactiveCommand Create taking Action"

requirements-completed: [META-01, META-02, META-03]

# Metrics
duration: 15min
completed: 2026-03-20
---

# Phase 03 Plan 04: Scraping Pipeline Lifecycle Wiring Summary

**ScraperHostedService wired into app lifecycle via Channel<int> queue with auto-scrape trigger after ROM scan and full Phase 3 DI registration in Program.cs**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-19T23:50:00Z
- **Completed:** 2026-03-20T00:05:32Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- ScraperHostedService processes queued game IDs through ScraperPipeline sequentially via Channel<int>, reporting progress to ScrapingStatusViewModel after each game
- Auto-scrape triggers automatically after ROM scan: RomScanHostedService calls QueuePendingGamesAsync after LoadGamesFromDbAsync
- All Phase 3 services registered in Program.cs DI: ScreenScraperClient with Polly resilience handler (4 retries, exponential backoff, 429/5xx handling), IgdbClient via IGDB SDK, ScraperPipeline (scoped), ArtCacheService, ScrapingStatusViewModel, GameDetailViewModel

## Task Commits

Each task was committed atomically:

1. **Task 1: ScrapingProgress, ScrapingStatusViewModel, ScraperHostedService** - `ecf36d0` (feat)
2. **Task 2: DI wiring for all Phase 3 services in Program.cs** - `fbe2ac1` (feat)

## Files Created/Modified

- `src/YARL/Infrastructure/Scraping/ScrapingProgress.cs` - Record with TotalGames, ScrapedCount, MatchedCount, UnmatchedCount, IsRateLimited, RateLimitResumeSeconds, CurrentGameTitle
- `src/YARL/Infrastructure/Scraping/ScraperHostedService.cs` - BackgroundService with Channel<int> queue, QueuePendingGamesAsync, QueueAllForRescrapeAsync, sequential processing loop
- `src/YARL/UI/ViewModels/ScrapingStatusViewModel.cs` - ReactiveObject with UpdateProgress, RescrapeLibraryCommand, ProgressPercent, StatusText
- `src/YARL/Program.cs` - Added Phase 3 DI: IConfiguration, ScreenScraperClient+resilience, ArtCacheService, IGDBClient, IgdbClient, ScraperPipeline, ScraperHostedService, ScrapingStatusViewModel, GameDetailViewModel
- `src/YARL/Infrastructure/Scanning/RomScanHostedService.cs` - Inject ScraperHostedService, call QueuePendingGamesAsync after scan
- `src/YARL/Infrastructure/Scanning/PlatformDefinition.cs` - Added ScreenscraperSystemId (int) and ThemeColor (string, default "#0f3460")

## Decisions Made

- ScraperHostedService registered as AddSingleton + AddHostedService delegate to allow both direct resolution and IHostedService lifecycle
- RescrapeLibraryCommand uses fire-and-forget lambda `() => _ = hostedService.QueueAllForRescrapeAsync()` to avoid async void
- IConfiguration registered via ConfigurationBuilder.AddEnvironmentVariables() for credential resolution without appsettings.json
- Polly using directive required explicitly — not in Microsoft.Extensions.Http.Resilience implicit usings

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] PlatformDefinition missing ScreenscraperSystemId property**
- **Found during:** Task 1 (build verification)
- **Issue:** ScraperHostedService.cs referenced `platform?.ScreenscraperSystemId` but PlatformDefinition had no such property, causing CS1061 compile error
- **Fix:** Added `ScreenscraperSystemId` (int) and `ThemeColor` (string) to PlatformDefinition per plan Task 2 requirements; included in Task 1 commit since it unblocked the build
- **Files modified:** src/YARL/Infrastructure/Scanning/PlatformDefinition.cs
- **Verification:** Build passed after fix
- **Committed in:** ecf36d0 (Task 1 commit)

**2. [Rule 3 - Blocking] Missing Polly using directive in Program.cs**
- **Found during:** Task 2 (build verification after Program.cs changes)
- **Issue:** DelayBackoffType and HttpRetryStrategyOptions from Microsoft.Extensions.Http.Resilience require `using Polly;` which was not in the plan's using list
- **Fix:** Added `using Polly;` to Program.cs
- **Files modified:** src/YARL/Program.cs
- **Verification:** Build passed, all 58 tests pass
- **Committed in:** fbe2ac1 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 3 - blocking)
**Impact on plan:** Both fixes required for compilation. No scope creep.

## Issues Encountered

None beyond the two auto-fixed blocking issues above.

## User Setup Required

External scraping services require credentials:
- `YARL_SS_DEVID`, `YARL_SS_DEVPASSWORD` — ScreenScraper developer credentials
- `YARL_SS_USERID`, `YARL_SS_USERPASSWORD` — ScreenScraper user credentials (optional, increases rate limits)
- `YARL_IGDB_CLIENT_ID`, `YARL_IGDB_CLIENT_SECRET` — IGDB/Twitch OAuth credentials

Without these env vars, scraping will return no matches but the app runs normally.

## Next Phase Readiness

- All Phase 3 services are DI-registered and wired — scraping runs automatically after ROM scan
- ScrapingStatusViewModel ready to bind to Settings panel UI (Phase 5)
- GameDetailViewModel ready for detail drawer UI binding
- Phase 4 (emulator launch) can proceed independently

---
*Phase: 03-metadata-and-cover-art*
*Completed: 2026-03-20*

## Self-Check: PASSED

- ScrapingProgress.cs: FOUND
- ScraperHostedService.cs: FOUND
- ScrapingStatusViewModel.cs: FOUND
- 03-04-SUMMARY.md: FOUND
- Commit ecf36d0: FOUND
- Commit fbe2ac1: FOUND
