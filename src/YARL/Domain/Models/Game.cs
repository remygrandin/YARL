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
    public DateTime CreatedAt { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Active;
    public string? Region { get; set; }                // parsed from filename tags
    public List<RomFile> RomFiles { get; set; } = [];
}
