namespace YARL.Infrastructure.Scraping;

public record ScrapingProgress
{
    public int TotalGames { get; init; }
    public int ScrapedCount { get; init; }
    public int MatchedCount { get; init; }
    public int UnmatchedCount { get; init; }
    public bool IsComplete { get; init; }
    public bool IsRateLimited { get; init; }
    public int RateLimitResumeSeconds { get; init; }
    public string? CurrentGameTitle { get; init; }
}
