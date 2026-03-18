namespace YARL.Domain.Models;

public class RomFile
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string? CRC32Hash { get; set; }
    public string? MD5Hash { get; set; }
    public string? SHA1Hash { get; set; }
    public int? DiscNumber { get; set; }               // 1-based; null for single-disc
    public bool IsM3uPlaylist { get; set; }
    public int SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
}
