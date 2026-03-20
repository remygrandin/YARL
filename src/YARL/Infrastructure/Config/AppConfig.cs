using System.Text.Json.Serialization;
using YARL.Domain.Enums;

namespace YARL.Infrastructure.Config;

public class AppConfig
{
    [JsonPropertyName("uiModeOverride")]
    public UIMode? UiModeOverride { get; set; }

    [JsonPropertyName("romCacheDir")]
    public string? RomCacheDir { get; set; }

    // ScreenScraper credentials
    [JsonPropertyName("screenScraperUser")]
    public string? ScreenScraperUser { get; set; }

    [JsonPropertyName("screenScraperPass")]
    public string? ScreenScraperPass { get; set; }

    // IGDB credentials (optional fallback scraper)
    [JsonPropertyName("igdbClientId")]
    public string? IgdbClientId { get; set; }

    [JsonPropertyName("igdbClientSecret")]
    public string? IgdbClientSecret { get; set; }
}
