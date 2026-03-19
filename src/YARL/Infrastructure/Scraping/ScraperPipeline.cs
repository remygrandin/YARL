using Serilog;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Images;

namespace YARL.Infrastructure.Scraping;

/// <summary>
/// Orchestrates the metadata scraping pipeline:
/// CRC32 hash → ScreenScraper → title fallback on SS → IGDB title → unmatched
///
/// Updates the Game entity in-place with scraped metadata and downloaded cover art.
/// </summary>
public class ScraperPipeline
{
    private readonly IMetadataScraper _screenScraper;
    private readonly IMetadataScraper _igdbFallback;
    private readonly ArtCacheService _artCache;
    private readonly ILogger _logger;

    public ScraperPipeline(
        IMetadataScraper screenScraper,
        IMetadataScraper igdbFallback,
        ArtCacheService artCache)
    {
        _screenScraper = screenScraper;
        _igdbFallback = igdbFallback;
        _artCache = artCache;
        _logger = Log.ForContext<ScraperPipeline>();
    }

    /// <summary>
    /// Scrapes metadata for a game using the full pipeline.
    /// Updates the Game entity in-place. Returns true if a match was found.
    /// Skips games with IsMetadataOverridden = true.
    /// </summary>
    public async Task<bool> ScrapeGameAsync(
        Game game, RomFile primaryRom, int screenscraperSystemId, CancellationToken ct = default)
    {
        if (game.IsMetadataOverridden)
        {
            _logger.Information("[ScraperPipeline] Skipping {Title} — metadata overridden", game.Title);
            return true;
        }

        ScrapedGameData? result = null;

        // Step 1: CRC32 hash lookup on ScreenScraper
        if (!string.IsNullOrEmpty(primaryRom.CRC32Hash))
        {
            result = await _screenScraper.SearchByCrc32Async(
                primaryRom.CRC32Hash, screenscraperSystemId, ct);
        }

        // Step 2: Title fallback on ScreenScraper
        if (result == null)
        {
            result = await _screenScraper.SearchByTitleAsync(
                game.Title, screenscraperSystemId, ct);
        }

        // Step 3: IGDB fallback
        if (result == null)
        {
            result = await _igdbFallback.SearchByTitleAsync(
                game.Title, screenscraperSystemId, ct);
        }

        // Step 4: Apply result or mark unmatched
        if (result == null)
        {
            game.ScrapeStatus = ScrapeStatus.Unmatched;
            _logger.Information("[ScraperPipeline] {Title} — unmatched", game.Title);
            return false;
        }

        // Apply metadata to Game entity
        game.Description = result.Description;
        game.Genre = result.Genre;
        game.ReleaseYear = result.ReleaseYear;
        game.Developer = result.Developer;
        game.Publisher = result.Publisher;
        game.ScraperMatchId = result.ScraperMatchId;
        game.ScraperSource = result.ScraperSource;
        game.ScrapeStatus = ScrapeStatus.Matched;

        // Download cover art
        if (!string.IsNullOrEmpty(result.CoverArtUrl))
        {
            var localPath = await _artCache.DownloadAndCacheAsync(
                result.CoverArtUrl, game.PlatformId, game.Id, ct);
            game.CoverArtPath = localPath;
        }

        _logger.Information("[ScraperPipeline] {Title} — matched via {Source}",
            game.Title, result.ScraperSource);
        return true;
    }
}
