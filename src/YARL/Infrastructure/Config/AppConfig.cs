using System.Text.Json.Serialization;
using YARL.Domain.Enums;

namespace YARL.Infrastructure.Config;

public record EmulatorConfig
{
    [JsonPropertyName("exePath")]
    public string ExePath { get; init; } = "";

    [JsonPropertyName("args")]
    public string Args { get; init; } = "{rompath}";

    [JsonPropertyName("isFlatpak")]
    public bool IsFlatpak { get; init; }
}

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

    [JsonPropertyName("emulatorConfigs")]
    public Dictionary<string, EmulatorConfig> EmulatorConfigs { get; set; } = new();
}
