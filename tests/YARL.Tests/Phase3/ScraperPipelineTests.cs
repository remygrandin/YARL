using System.Net;
using System.Net.Http;
using NSubstitute;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Images;
using YARL.Infrastructure.Scraping;

namespace YARL.Tests.Phase3;

[Trait("Category", "Phase3")]
[Trait("Class", "ScraperPipelineTests")]
public class ScraperPipelineTests
{
    private static readonly ScrapedGameData SampleData = new()
    {
        Title = "Super Mario World",
        Description = "A platform game",
        Genre = "Platform",
        ReleaseYear = 1990,
        Developer = "Nintendo",
        Publisher = "Nintendo",
        CoverArtUrl = "https://example.com/cover.jpg",
        ScraperMatchId = "1234",
        ScraperSource = "screenscraper"
    };

    private static Game MakeGame() => new()
    {
        Id = 42,
        Title = "Super Mario World",
        PlatformId = "snes",
        ScrapeStatus = ScrapeStatus.Pending
    };

    private static RomFile MakeRom(string? crc32 = "A1B2C3D4") => new()
    {
        Id = 1,
        FileName = "Super Mario World.sfc",
        CRC32Hash = crc32
    };

    /// <summary>
    /// Creates an ArtCacheService backed by a fake HTTP handler that returns image bytes.
    /// </summary>
    private static ArtCacheService MakeArtCache(string? returnPath = null)
    {
        var handler = new FakeImageHandler(returnPath != null);
        return new ArtCacheService(new HttpClient(handler));
    }

    private sealed class FakeImageHandler : HttpMessageHandler
    {
        private readonly bool _succeed;
        public FakeImageHandler(bool succeed) => _succeed = succeed;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_succeed)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF }) // fake JPEG header
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Pipeline_TriesScreenScraperFirst()
    {
        // META-01: CRC32 hash → ScreenScraper → success → IGDB never called
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns(SampleData);

        var pipeline = new ScraperPipeline(ss, igdb, MakeArtCache(null));
        var game = MakeGame();
        var rom = MakeRom();

        var matched = await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        Assert.True(matched);
        Assert.Equal(ScrapeStatus.Matched, game.ScrapeStatus);
        await igdb.DidNotReceive().SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pipeline_FallsBackToIgdb_WhenScreenScraperReturnsNull()
    {
        // META-02: ScreenScraper miss (both CRC32 and title) → IGDB title search → success
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns((ScrapedGameData?)null);
        ss.SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns((ScrapedGameData?)null);

        var igdbData = SampleData with { ScraperSource = "igdb" };
        igdb.SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(igdbData);

        var pipeline = new ScraperPipeline(ss, igdb, MakeArtCache(null));
        var game = MakeGame();
        var rom = MakeRom();

        var matched = await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        Assert.True(matched);
        Assert.Equal(ScrapeStatus.Matched, game.ScrapeStatus);
        Assert.Equal("igdb", game.ScraperSource);
        await igdb.Received(1).SearchByTitleAsync("Super Mario World", 4, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pipeline_MarksUnmatched_WhenBothSourcesFail()
    {
        // META-01/02: Both sources return null → ScrapeStatus.Unmatched
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns((ScrapedGameData?)null);
        ss.SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns((ScrapedGameData?)null);
        igdb.SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((ScrapedGameData?)null);

        var pipeline = new ScraperPipeline(ss, igdb, MakeArtCache(null));
        var game = MakeGame();
        var rom = MakeRom();

        var matched = await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        Assert.False(matched);
        Assert.Equal(ScrapeStatus.Unmatched, game.ScrapeStatus);
    }

    [Fact]
    public async Task Pipeline_PopulatesAllMetadataFields()
    {
        // META-03: After successful scrape, Game has all 6 metadata fields populated
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns(SampleData);

        var pipeline = new ScraperPipeline(ss, igdb, MakeArtCache(null));
        var game = MakeGame();
        var rom = MakeRom();

        await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        Assert.Equal("A platform game", game.Description);
        Assert.Equal("Platform", game.Genre);
        Assert.Equal(1990, game.ReleaseYear);
        Assert.Equal("Nintendo", game.Developer);
        Assert.Equal("Nintendo", game.Publisher);
        Assert.Equal("1234", game.ScraperMatchId);
        Assert.Equal("screenscraper", game.ScraperSource);
        Assert.Equal(ScrapeStatus.Matched, game.ScrapeStatus);
    }

    [Fact]
    public async Task Pipeline_DownloadsCoverArt_ToLocalCache()
    {
        // META-03: Cover art URL triggers download via ArtCacheService
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns(SampleData);  // SampleData has CoverArtUrl = "https://example.com/cover.jpg"

        // Use a real ArtCacheService backed by a fake HTTP handler that returns fake image bytes
        var artCache = MakeArtCache("some-path");  // pass non-null = fake HTTP returns 200

        var pipeline = new ScraperPipeline(ss, igdb, artCache);
        var game = MakeGame();
        var rom = MakeRom();

        await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        // CoverArtPath should be populated (a local path)
        Assert.NotNull(game.CoverArtPath);
        Assert.Contains("snes", game.CoverArtPath);
        Assert.Contains("42", game.CoverArtPath);
    }

    [Fact]
    public async Task Pipeline_FallsBackToTitleSearch_WhenCrc32Misses()
    {
        // META-01: CRC32 miss → title search on ScreenScraper → success
        var ss = Substitute.For<IMetadataScraper>();
        var igdb = Substitute.For<IMetadataScraper>();

        ss.SearchByCrc32Async(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns((ScrapedGameData?)null);
        ss.SearchByTitleAsync("Super Mario World", Arg.Any<int>(), Arg.Any<CancellationToken>())
          .Returns(SampleData);

        var pipeline = new ScraperPipeline(ss, igdb, MakeArtCache(null));
        var game = MakeGame();
        var rom = MakeRom();

        var matched = await pipeline.ScrapeGameAsync(game, rom, screenscraperSystemId: 4);

        Assert.True(matched);
        Assert.Equal(ScrapeStatus.Matched, game.ScrapeStatus);
        await ss.Received(1).SearchByTitleAsync("Super Mario World", Arg.Any<int>(), Arg.Any<CancellationToken>());
        await igdb.DidNotReceive().SearchByTitleAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
