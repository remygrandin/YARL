namespace YARL.Domain.Models;

public class GameVersion
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string Region { get; set; } = "";        // "usa" | "europe" | "japan" | "world"
    public string? LocalizedTitle { get; set; }
    public int? RomFileId { get; set; }
}
