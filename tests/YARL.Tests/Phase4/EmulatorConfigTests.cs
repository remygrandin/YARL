using System.Text.Json;
using YARL.Infrastructure.Config;

namespace YARL.Tests.Phase4;

[Trait("Category", "Phase4")]
[Trait("Class", "EmulatorConfigTests")]
public class EmulatorConfigTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void EmulatorConfig_RoundTrip_JsonSerializeDeserialize()
    {
        // Arrange
        var config = new AppConfig();
        config.EmulatorConfigs["snes"] = new EmulatorConfig
        {
            ExePath = "/usr/bin/retroarch",
            Args = "-L core.so {rompath}",
            IsFlatpak = false
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)!;

        // Assert
        Assert.True(deserialized.EmulatorConfigs.ContainsKey("snes"));
        var snes = deserialized.EmulatorConfigs["snes"];
        Assert.Equal("/usr/bin/retroarch", snes.ExePath);
        Assert.Equal("-L core.so {rompath}", snes.Args);
        Assert.False(snes.IsFlatpak);
    }

    [Fact]
    public void EmulatorConfig_DefaultArgs_IsRompath()
    {
        Assert.Equal("{rompath}", new EmulatorConfig().Args);
    }

    [Fact]
    public void EmulatorConfig_EmptyDictionary_DeserializesToEmpty()
    {
        // JSON without emulatorConfigs key
        var json = """{"uiModeOverride":null,"romCacheDir":null}""";

        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)!;

        Assert.NotNull(config.EmulatorConfigs);
        Assert.Empty(config.EmulatorConfigs);
    }
}
