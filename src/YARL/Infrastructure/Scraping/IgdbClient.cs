using IGDB;
using IGDB.Models;
using Serilog;
using YARL.Infrastructure.Config;

namespace YARL.Infrastructure.Scraping;

/// <summary>
/// IGDB APIv4 client using the kamranayub/igdb-dotnet NuGet SDK.
/// Used as fallback scraper when ScreenScraper returns no match.
/// Note: IGDB does not support hash-based lookup — only title search.
/// </summary>
public class IgdbClient : IMetadataScraper
{
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public string SourceName => "igdb";

    public IgdbClient(AppConfig config)
    {
        _config = config;
        _logger = Log.ForContext<IgdbClient>();
    }

    private IGDBClient CreateClient() =>
        IGDBClient.CreateWithDefaults(
            _config.IgdbClientId ?? "",
            _config.IgdbClientSecret ?? "");

    /// <summary>
    /// IGDB does not support CRC32/hash-based lookup — always returns null.
    /// The pipeline will fall back to SearchByTitleAsync.
    /// </summary>
    public Task<ScrapedGameData?> SearchByCrc32Async(string crc32, int screenscraperSystemId, CancellationToken ct = default)
    {
        return Task.FromResult<ScrapedGameData?>(null);
    }

    /// <summary>
    /// Search IGDB by game title. Takes the first result.
    /// </summary>
    public async Task<ScrapedGameData?> SearchByTitleAsync(string title, int screenscraperSystemId, CancellationToken ct = default)
    {
        try
        {
            var query = $"fields id,name,cover.image_id,first_release_date,genres.name," +
                        $"involved_companies.company.name,involved_companies.developer,involved_companies.publisher," +
                        $"summary; search \"{EscapeIgdbString(title)}\"; limit 5;";

            var games = await CreateClient().QueryAsync<Game>(IGDBClient.Endpoints.Games, query);

            var game = games?.FirstOrDefault();
            if (game == null)
                return null;

            return MapToScrapedData(game);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning(ex, "[IgdbClient] Failed to search IGDB for title '{Title}'", title);
            return null;
        }
    }

    private static ScrapedGameData MapToScrapedData(Game game)
    {
        var coverUrl = ExtractCoverUrl(game);
        var developer = ExtractCompany(game, isDeveloper: true);
        var publisher = ExtractCompany(game, isDeveloper: false);
        var releaseYear = ExtractReleaseYear(game);
        var genre = game.Genres?.Values?.FirstOrDefault()?.Name;

        return new ScrapedGameData
        {
            Title = game.Name ?? "",
            Description = game.Summary,
            Genre = genre,
            ReleaseYear = releaseYear,
            Developer = developer,
            Publisher = publisher,
            CoverArtUrl = coverUrl,
            ScraperMatchId = game.Id?.ToString(),
            ScraperSource = "igdb"
        };
    }

    private static string? ExtractCoverUrl(Game game)
    {
        var imageId = game.Cover?.Value?.ImageId;
        if (string.IsNullOrEmpty(imageId)) return null;

        var rawUrl = ImageHelper.GetImageUrl(imageId, ImageSize.CoverBig, false);
        if (string.IsNullOrEmpty(rawUrl)) return null;

        // IGDB returns protocol-relative URLs ("//images.igdb.com/...")
        // HttpClient requires absolute URLs with scheme
        return rawUrl.StartsWith("//") ? "https:" + rawUrl : rawUrl;
    }

    private static string? ExtractCompany(Game game, bool isDeveloper)
    {
        var companies = game.InvolvedCompanies?.Values;
        if (companies == null) return null;

        var match = isDeveloper
            ? companies.FirstOrDefault(c => c.Developer == true)
            : companies.FirstOrDefault(c => c.Publisher == true);

        return match?.Company?.Value?.Name;
    }

    private static int? ExtractReleaseYear(Game game)
    {
        if (!game.FirstReleaseDate.HasValue) return null;
        return game.FirstReleaseDate.Value.Year;
    }

    private static string EscapeIgdbString(string s)
    {
        // Escape double quotes in IGDB APIcalypse query string
        return s.Replace("\"", "\\\"");
    }
}
