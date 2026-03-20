using System.Text;
using System.Text.Json;
using Serilog;
using YARL.Infrastructure.Config;

namespace YARL.Infrastructure.Scraping;

/// <summary>
/// Hand-rolled HTTP client for ScreenScraper API v2 (no .NET library exists).
/// Uses the jeuInfos.php endpoint for CRC32 hash and title lookups.
/// </summary>
public class ScreenScraperClient : IMetadataScraper
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public string SourceName => "screenscraper";

    public ScreenScraperClient(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = Log.ForContext<ScreenScraperClient>();
    }

    /// <summary>
    /// Look up a game by CRC32 hash.
    /// CRC32 must be 8-char uppercase hex (e.g. "A1B2C3D4").
    /// Note: use GetCurrentHashAsUInt32().ToString("X8") — not Convert.ToHexString (wrong endianness).
    /// </summary>
    public async Task<ScrapedGameData?> SearchByCrc32Async(string crc32, int screenscraperSystemId, CancellationToken ct = default)
    {
        var url = BuildUrl(screenscraperSystemId, $"crc={crc32.ToUpperInvariant()}");
        return await FetchGameDataAsync(url, ct);
    }

    /// <summary>
    /// Look up a game by title (fallback when CRC32 hash misses).
    /// </summary>
    public async Task<ScrapedGameData?> SearchByTitleAsync(string title, int screenscraperSystemId, CancellationToken ct = default)
    {
        var url = BuildUrl(screenscraperSystemId, $"romnom={Uri.EscapeDataString(title)}");
        return await FetchGameDataAsync(url, ct);
    }

    private string BuildUrl(int systemId, string searchParam)
    {
        var sb = new StringBuilder("https://www.screenscraper.fr/api2/jeuInfos.php?");
        sb.Append($"devid=&devpassword=&");
        sb.Append($"softname=yarl&output=json&");
        sb.Append($"systemeid={systemId}&");
        sb.Append(searchParam);
        if (!string.IsNullOrEmpty(_config.ScreenScraperUser))
            sb.Append($"&ssid={Uri.EscapeDataString(_config.ScreenScraperUser)}&sspassword={Uri.EscapeDataString(_config.ScreenScraperPass ?? "")}");
        return sb.ToString();
    }

    private async Task<ScrapedGameData?> FetchGameDataAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var responseEl) ||
                !responseEl.TryGetProperty("jeu", out var jeu))
            {
                return null;
            }

            return ParseJeuElement(jeu);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning(ex, "[ScreenScraperClient] Failed to fetch game data from {Url}", url);
            return null;
        }
    }

    private static ScrapedGameData ParseJeuElement(JsonElement jeu)
    {
        var title = ExtractLocalizedText(jeu, "noms", "region", "us", "eu", "wor", "ss") ?? "";
        var description = ExtractLocalizedText(jeu, "synopsis", "langue", "en", "fr", "de");
        var genre = ExtractGenreText(jeu);
        var releaseYear = ExtractReleaseYear(jeu);
        var developer = ExtractTextProperty(jeu, "developpeur");
        var publisher = ExtractTextProperty(jeu, "editeur");
        var coverArtUrl = ExtractCoverArtUrl(jeu);
        var scraperMatchId = jeu.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var regionalNames = ExtractRegionalNames(jeu);

        return new ScrapedGameData
        {
            Title = title,
            Description = description,
            Genre = genre,
            ReleaseYear = releaseYear,
            Developer = developer,
            Publisher = publisher,
            CoverArtUrl = coverArtUrl,
            ScraperMatchId = scraperMatchId,
            ScraperSource = "screenscraper",
            RegionalNames = regionalNames
        };
    }

    /// <summary>
    /// Extracts text from an array of localized entries, preferring regions/languages in order.
    /// Looks for: { "region"/"langue": "us", "text": "..." }
    /// </summary>
    private static string? ExtractLocalizedText(JsonElement parent, string arrayName, string keyName, params string[] preferenceOrder)
    {
        if (!parent.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        var entries = arr.EnumerateArray().ToList();
        foreach (var key in preferenceOrder)
        {
            var match = entries.FirstOrDefault(e =>
                e.TryGetProperty(keyName, out var k) && k.GetString() == key);
            if (match.ValueKind != JsonValueKind.Undefined)
            {
                if (match.TryGetProperty("text", out var textEl))
                    return textEl.GetString();
            }
        }

        // Fallback: first entry with text
        return entries.Select(e => e.TryGetProperty("text", out var t) ? t.GetString() : null)
                      .FirstOrDefault(t => t != null);
    }

    private static string? ExtractGenreText(JsonElement jeu)
    {
        if (!jeu.TryGetProperty("genres", out var genres) || genres.ValueKind != JsonValueKind.Array)
            return null;

        var firstGenre = genres.EnumerateArray().FirstOrDefault();
        if (firstGenre.ValueKind == JsonValueKind.Undefined) return null;

        return ExtractLocalizedText(firstGenre, "noms", "langue", "en", "fr");
    }

    private static int? ExtractReleaseYear(JsonElement jeu)
    {
        if (!jeu.TryGetProperty("dates", out var dates) || dates.ValueKind != JsonValueKind.Array)
            return null;

        var entries = dates.EnumerateArray().ToList();
        string? dateStr = null;

        foreach (var region in new[] { "us", "eu", "wor" })
        {
            var match = entries.FirstOrDefault(e =>
                e.TryGetProperty("region", out var r) && r.GetString() == region);
            if (match.ValueKind != JsonValueKind.Undefined &&
                match.TryGetProperty("text", out var t))
            {
                dateStr = t.GetString();
                break;
            }
        }

        dateStr ??= entries.Select(e => e.TryGetProperty("text", out var t) ? t.GetString() : null)
                           .FirstOrDefault(t => t != null);

        if (dateStr != null && dateStr.Length >= 4 && int.TryParse(dateStr[..4], out var year))
            return year;
        return null;
    }

    private static string? ExtractTextProperty(JsonElement jeu, string propertyName)
    {
        if (!jeu.TryGetProperty(propertyName, out var prop))
            return null;
        if (prop.TryGetProperty("text", out var textEl))
            return textEl.GetString();
        return null;
    }

    private static string? ExtractCoverArtUrl(JsonElement jeu)
    {
        if (!jeu.TryGetProperty("medias", out var medias) || medias.ValueKind != JsonValueKind.Array)
            return null;

        var mediaList = medias.EnumerateArray().ToList();
        var regionPriority = new[] { "us", "eu", "wor", "jp" };

        // Prefer box-2D
        foreach (var region in regionPriority)
        {
            var match = mediaList.FirstOrDefault(m =>
                m.TryGetProperty("type", out var t) && t.GetString() == "box-2D" &&
                m.TryGetProperty("region", out var r) && r.GetString() == region);
            if (match.ValueKind != JsonValueKind.Undefined)
            {
                if (match.TryGetProperty("url", out var urlEl))
                    return urlEl.GetString();
            }
        }

        // Fallback: any box-2D
        var anyBox2D = mediaList.FirstOrDefault(m =>
            m.TryGetProperty("type", out var t) && t.GetString() == "box-2D");
        if (anyBox2D.ValueKind != JsonValueKind.Undefined && anyBox2D.TryGetProperty("url", out var boxUrl))
            return boxUrl.GetString();

        // Fallback: screenshot (ss type)
        var screenshot = mediaList.FirstOrDefault(m =>
            m.TryGetProperty("type", out var t) && t.GetString() == "ss");
        if (screenshot.ValueKind != JsonValueKind.Undefined && screenshot.TryGetProperty("url", out var ssUrl))
            return ssUrl.GetString();

        return null;
    }

    private static List<RegionalName> ExtractRegionalNames(JsonElement jeu)
    {
        var result = new List<RegionalName>();
        if (!jeu.TryGetProperty("noms", out var noms) || noms.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var nom in noms.EnumerateArray())
        {
            if (nom.TryGetProperty("region", out var region) && nom.TryGetProperty("text", out var text))
            {
                var regionStr = region.GetString() ?? "";
                var titleStr = text.GetString() ?? "";
                if (!string.IsNullOrEmpty(titleStr))
                    result.Add(new RegionalName(regionStr, titleStr));
            }
        }
        return result;
    }
}
