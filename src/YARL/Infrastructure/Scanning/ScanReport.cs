namespace YARL.Infrastructure.Scanning;

public record ScanReport
{
    public int PlatformsFound { get; init; }
    public int GamesAdded { get; init; }
    public int GamesRemoved { get; init; }
    public List<string> UnmatchedFolders { get; init; } = [];
    public DateTime CompletedAt { get; init; }
}
