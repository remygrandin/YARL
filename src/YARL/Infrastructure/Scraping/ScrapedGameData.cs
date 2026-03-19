namespace YARL.Infrastructure.Scraping;

public record ScrapedGameData
{
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? Genre { get; init; }
    public int? ReleaseYear { get; init; }
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string? CoverArtUrl { get; init; }       // remote URL to download
    public string? ScraperMatchId { get; init; }    // source-specific game ID
    public string ScraperSource { get; init; } = "";  // "screenscraper" or "igdb"
    public List<RegionalName> RegionalNames { get; init; } = [];
}

public record RegionalName(string Region, string Title);
