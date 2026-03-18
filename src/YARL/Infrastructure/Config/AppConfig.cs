using System.Text.Json.Serialization;
using YARL.Domain.Enums;

namespace YARL.Infrastructure.Config;

public class AppConfig
{
    [JsonPropertyName("uiModeOverride")]
    public UIMode? UiModeOverride { get; set; }

    [JsonPropertyName("romCacheDir")]
    public string? RomCacheDir { get; set; }
}
