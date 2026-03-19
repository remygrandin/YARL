using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Domain.Enums;
using YARL.Domain.Models;

namespace YARL.UI.ViewModels;

public partial class GameViewModel : ReactiveObject
{
    public int Id { get; }
    public string Title { get; }
    public string RawTitle { get; }
    public string PlatformId { get; }
    public string? Region { get; }
    public GameStatus Status { get; }
    public DateTime CreatedAt { get; }

    [Reactive] private bool _isFavorite;
    [Reactive] private DateTime? _lastPlayedAt;

    // Metadata properties (populated by scraper, Plan 03-03)
    public string? Description { get; }
    public string? Genre { get; }
    public int? ReleaseYear { get; }
    public string? Developer { get; }
    public string? Publisher { get; }
    public string? ScraperMatchId { get; }
    public string? ScraperSource { get; }
    public bool IsMetadataOverridden { get; set; }

    [Reactive] private string? _coverArtPath;
    [Reactive] private ScrapeStatus _scrapeStatus;

    // Aliases for search matching (from GameVersion regional names)
    public List<string> Aliases { get; }

    public GameViewModel(Game game)
    {
        Id = game.Id;
        Title = game.Title;
        RawTitle = game.RawTitle;
        PlatformId = game.PlatformId;
        Region = game.Region;
        Status = game.Status;
        CreatedAt = game.CreatedAt;
        _isFavorite = game.IsFavorite;
        _lastPlayedAt = game.LastPlayedAt;

        // Metadata
        Description = game.Description;
        Genre = game.Genre;
        ReleaseYear = game.ReleaseYear;
        Developer = game.Developer;
        Publisher = game.Publisher;
        _coverArtPath = game.CoverArtPath;
        _scrapeStatus = game.ScrapeStatus;
        ScraperMatchId = game.ScraperMatchId;
        ScraperSource = game.ScraperSource;
        IsMetadataOverridden = game.IsMetadataOverridden;
        Aliases = game.Versions?
            .Select(v => v.LocalizedTitle)
            .Where(t => t != null)
            .Cast<string>()
            .ToList() ?? [];
    }
}
