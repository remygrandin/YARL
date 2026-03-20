using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using YARL.Domain.Enums;
using YARL.Infrastructure.Images;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Scanning;
using YARL.UI.ViewModels;

namespace YARL.Infrastructure.Scraping;

/// <summary>
/// Background service that scrapes metadata for games after ROM scan completes.
/// Queues all games with ScrapeStatus.Pending and processes sequentially
/// (single thread for ScreenScraper free account compliance).
/// </summary>
public class ScraperHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LibraryViewModel _libraryVm;
    private readonly Action<ScrapingProgress>? _reportProgress;
    private readonly PlatformRegistry _platformRegistry;
    private readonly Channel<int> _scrapeQueue = Channel.CreateUnbounded<int>();
    private readonly ILogger _logger = Log.ForContext<ScraperHostedService>();

    public ScraperHostedService(
        IServiceScopeFactory scopeFactory,
        LibraryViewModel libraryVm,
        PlatformRegistry platformRegistry,
        Action<ScrapingProgress>? reportProgress = null)
    {
        _scopeFactory = scopeFactory;
        _libraryVm = libraryVm;
        _platformRegistry = platformRegistry;
        _reportProgress = reportProgress;
    }

    /// <summary>
    /// Called externally (e.g., by RomScanHostedService after scan) to queue games for scraping.
    /// </summary>
    public async Task QueuePendingGamesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

        var pendingIds = await db.Games
            .Where(g => g.ScrapeStatus == ScrapeStatus.Pending)
            .Select(g => g.Id)
            .ToListAsync();

        _logger.Information("[ScraperHostedService] Queuing {Count} pending games for scraping", pendingIds.Count);

        foreach (var id in pendingIds)
            await _scrapeQueue.Writer.WriteAsync(id);
    }

    /// <summary>
    /// Queue all games for re-scrape (ignoring override flag — overridden games are skipped in pipeline).
    /// </summary>
    public async Task QueueAllForRescrapeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

        // Reset non-overridden games to Pending
        await db.Games
            .Where(g => !g.IsMetadataOverridden)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.ScrapeStatus, ScrapeStatus.Pending));

        await QueuePendingGamesAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Return control to startup pipeline

        _logger.Information("[ScraperHostedService] Started — waiting for scrape queue");

        int totalGames = 0, scrapedCount = 0, matchedCount = 0, unmatchedCount = 0;

        await foreach (var gameId in _scrapeQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
                var pipeline = scope.ServiceProvider.GetRequiredService<ScraperPipeline>();

                var game = await db.Games
                    .Include(g => g.RomFiles)
                    .FirstOrDefaultAsync(g => g.Id == gameId, stoppingToken);

                if (game == null) continue;

                var primaryRom = game.RomFiles.FirstOrDefault(r => !r.IsM3uPlaylist);
                if (primaryRom == null) continue;

                var platform = _platformRegistry.Resolve(game.PlatformId);
                var systemId = platform?.ScreenscraperSystemId ?? 0;

                if (totalGames == 0)
                {
                    // Count total on first item
                    totalGames = await db.Games.CountAsync(g => g.ScrapeStatus == ScrapeStatus.Pending, stoppingToken);
                    totalGames = Math.Max(totalGames, 1); // avoid division by zero
                }

                var matched = await pipeline.ScrapeGameAsync(game, primaryRom, systemId, stoppingToken);
                await db.SaveChangesAsync(stoppingToken);

                scrapedCount++;
                if (matched) matchedCount++; else unmatchedCount++;

                // Update SourceCache so UI reflects new cover art
                var gvm = new GameViewModel(game);
                _libraryVm.AddOrUpdateGame(gvm);

                // Report progress
                _reportProgress?.Invoke(new ScrapingProgress
                {
                    TotalGames = totalGames,
                    ScrapedCount = scrapedCount,
                    MatchedCount = matchedCount,
                    UnmatchedCount = unmatchedCount,
                    CurrentGameTitle = game.Title,
                    IsComplete = false
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.Error(ex, "[ScraperHostedService] Error scraping gameId={GameId}", gameId);
                scrapedCount++;
                unmatchedCount++;
            }
        }

        _reportProgress?.Invoke(new ScrapingProgress
        {
            TotalGames = totalGames,
            ScrapedCount = scrapedCount,
            MatchedCount = matchedCount,
            UnmatchedCount = unmatchedCount,
            IsComplete = true
        });

        _logger.Information("[ScraperHostedService] Finished — {Matched} matched, {Unmatched} unmatched",
            matchedCount, unmatchedCount);
    }
}
