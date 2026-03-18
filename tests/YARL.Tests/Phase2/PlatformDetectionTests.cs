using YARL.Infrastructure.Scanning;

namespace YARL.Tests.Phase2;

[Trait("Category", "Phase2")]
public class PlatformDetectionTests
{
    private static PlatformRegistry BuildTestRegistry()
    {
        var platforms = new List<PlatformDefinition>
        {
            new()
            {
                Id = "snes",
                Name = "Super Nintendo",
                Extensions = [".smc", ".sfc", ".fig"],
                Aliases = ["SNES", "Super Famicom", "SFC", "Super Nintendo Entertainment System"]
            },
            new()
            {
                Id = "nes",
                Name = "Nintendo Entertainment System",
                Extensions = [".nes", ".unf"],
                Aliases = ["NES", "Famicom"]
            }
        };
        return new PlatformRegistry(platforms);
    }

    [Fact]
    public void Resolves_platform_by_exact_name()
    {
        var registry = BuildTestRegistry();
        var result = registry.Resolve("Super Nintendo");
        Assert.NotNull(result);
        Assert.Equal("snes", result.Id);
    }

    [Fact]
    public void Resolves_platform_by_alias_case_insensitive()
    {
        var registry = BuildTestRegistry();

        var lower = registry.Resolve("snes");
        Assert.NotNull(lower);
        Assert.Equal("snes", lower.Id);

        var upper = registry.Resolve("SNES");
        Assert.NotNull(upper);
        Assert.Equal("snes", upper.Id);
    }

    [Fact]
    public void Returns_null_for_unknown_folder()
    {
        var registry = BuildTestRegistry();
        var result = registry.Resolve("Unknown System");
        Assert.Null(result);
    }

    [Fact]
    public void Filters_files_by_platform_extensions()
    {
        var registry = BuildTestRegistry();
        var snes = registry.Resolve("SNES")!;

        Assert.True(PlatformRegistry.IsAllowedExtension(snes, "game.smc"));
        Assert.True(PlatformRegistry.IsAllowedExtension(snes, "game.sfc"));
        Assert.False(PlatformRegistry.IsAllowedExtension(snes, "game.txt"));
        Assert.False(PlatformRegistry.IsAllowedExtension(snes, "game.iso"));
    }

    [Fact]
    public void CleanTitle_strips_region_and_version_tags()
    {
        var result = FilenameParser.CleanTitle("Super Mario World (USA) (Rev 1)");
        Assert.Equal("Super Mario World", result);
    }

    [Fact]
    public void ExtractRegion_parses_USA_Europe_Japan()
    {
        Assert.Equal("USA", FilenameParser.ExtractRegion("Game (USA)"));
        Assert.Equal("Europe", FilenameParser.ExtractRegion("Game (Europe)"));
        Assert.Equal("Japan", FilenameParser.ExtractRegion("Game (Japan)"));
        Assert.Null(FilenameParser.ExtractRegion("Game Without Region"));
    }
}
