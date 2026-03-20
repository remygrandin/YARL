namespace YARL.Infrastructure.Scanning;

public class PlatformDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Extensions { get; set; } = [];
    public List<string> Aliases { get; set; } = [];
    public string? RetroarchCoreId { get; set; }
    public int? ScreenscraperSystemId { get; set; }
    public string ThemeColor { get; set; } = "#0f3460";
}

public class PlatformsRoot
{
    public List<PlatformDefinition> Platforms { get; set; } = [];
}
