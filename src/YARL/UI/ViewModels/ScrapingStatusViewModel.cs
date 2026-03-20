using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Infrastructure.Scraping;

namespace YARL.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings scraping progress panel.
/// Receives progress updates from ScraperHostedService.
/// </summary>
public partial class ScrapingStatusViewModel : ReactiveObject
{
    [Reactive] private bool _isScraping;
    [Reactive] private int _totalGames;
    [Reactive] private int _scrapedCount;
    [Reactive] private int _matchedCount;
    [Reactive] private int _unmatchedCount;
    [Reactive] private bool _isRateLimited;
    [Reactive] private int _rateLimitResumeSeconds;
    [Reactive] private string _statusText = "";
    [Reactive] private double _progressPercent;

    // Re-scrape command (triggers global re-scrape)
    public ReactiveCommand<Unit, Unit> RescrapeLibraryCommand { get; }

    private readonly Action? _triggerRescrape;

    public ScrapingStatusViewModel(Action? triggerRescrape = null)
    {
        _triggerRescrape = triggerRescrape;
        RescrapeLibraryCommand = ReactiveCommand.Create(() => _triggerRescrape?.Invoke());
    }

    public void UpdateProgress(ScrapingProgress progress)
    {
        IsScraping = !progress.IsComplete;
        TotalGames = progress.TotalGames;
        ScrapedCount = progress.ScrapedCount;
        MatchedCount = progress.MatchedCount;
        UnmatchedCount = progress.UnmatchedCount;
        IsRateLimited = progress.IsRateLimited;
        RateLimitResumeSeconds = progress.RateLimitResumeSeconds;
        ProgressPercent = progress.TotalGames > 0
            ? (double)progress.ScrapedCount / progress.TotalGames * 100
            : 0;

        if (progress.IsComplete)
            StatusText = $"Scraping complete — {MatchedCount} matched, {UnmatchedCount} unmatched";
        else if (progress.IsRateLimited)
            StatusText = $"Rate limit reached — resuming in {RateLimitResumeSeconds}s";
        else
            StatusText = $"Scraping {ScrapedCount} / {TotalGames} games";
    }
}
