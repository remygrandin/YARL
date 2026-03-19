using Xunit;

namespace YARL.Tests.Phase3;

public class ScraperPipelineTests
{
    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_TriesScreenScraperFirst()
    {
        // META-01: CRC32 hash → ScreenScraper → success → done
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_FallsBackToIgdb_WhenScreenScraperReturnsNull()
    {
        // META-02: ScreenScraper miss → IGDB title search → success
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_MarksUnmatched_WhenBothSourcesFail()
    {
        // META-01/02: Both sources return null → ScrapeStatus.Unmatched
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_PopulatesAllMetadataFields()
    {
        // META-03: After successful scrape, Game has Description, Genre, ReleaseYear, Developer, Publisher, CoverArtPath
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_DownloadsCoverArt_ToLocalCache()
    {
        // META-03: Cover art downloaded to {AppData}/yarl/art/{platformId}/{gameId}.jpg
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Pipeline_FallsBackToTitleSearch_WhenCrc32Misses()
    {
        // META-01: CRC32 miss → title search on ScreenScraper → success
        await Task.CompletedTask;
    }
}
