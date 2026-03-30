using YARL.Domain.Enums;

namespace YARL.Domain.Models;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; } = "";           // cleaned filename
    public string RawTitle { get; set; } = "";         // original filename without extension
    public string PlatformId { get; set; } = "";       // FK to platforms.json Id
    public int SourceId { get; set; }                  // FK to RomSources.Id
    public bool IsFavorite { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Active;
    public string? Region { get; set; }                // parsed from filename tags

    // Metadata (nullable — populated by scraper)
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public string? CoverArtPath { get; set; }
    public string? ScraperMatchId { get; set; }
    public string? ScraperSource { get; set; }         // "screenscraper" | "manual"
    public bool IsMetadataOverridden { get; set; }
    public ScrapeStatus ScrapeStatus { get; set; } = ScrapeStatus.Pending;

    // Regional variant grouping
    public List<GameVersion> Versions { get; set; } = [];

    public List<RomFile> RomFiles { get; set; } = [];
}
