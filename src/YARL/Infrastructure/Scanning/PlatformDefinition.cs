namespace YARL.Infrastructure.Scanning;

public class PlatformDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Extensions { get; set; } = [];
    public List<string> Aliases { get; set; } = [];
    public string? RetroarchCoreId { get; set; }
}

public class PlatformsRoot
{
    public List<PlatformDefinition> Platforms { get; set; } = [];
}
