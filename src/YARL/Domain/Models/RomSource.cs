using YARL.Domain.Enums;

namespace YARL.Domain.Models;

public class RomSource
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public SourceType SourceType { get; set; }
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = true;
}
