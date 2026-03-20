using System.Net;
using System.Net.Http;
using NSubstitute;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Scraping;

namespace YARL.Tests.Phase3;

[Trait("Category", "Phase3")]
[Trait("Class", "ScreenScraperClientTests")]
public class ScreenScraperClientTests
{
    private const string SuccessJson = """
        {
          "response": {
            "jeu": {
              "id": "1234",
              "noms": [
                { "region": "us", "text": "Super Mario World" },
                { "region": "eu", "text": "Super Mario World EU" }
              ],
              "synopsis": [
                { "langue": "en", "text": "A platform game" },
                { "langue": "fr", "text": "Un jeu de plateformes" }
              ],
              "genres": [
                { "noms": [{ "langue": "en", "text": "Platform" }, { "langue": "fr", "text": "Plateforme" }] }
              ],
              "dates": [
                { "region": "us", "text": "1990-11-21" }
              ],
              "developpeur": { "text": "Nintendo" },
              "editeur": { "text": "Nintendo" },
              "medias": [
                {
                  "type": "box-2D",
                  "region": "us",
                  "url": "https://screenscraper.fr/image.php?gameid=1234&media=box-2D&region=us",
                  "format": "png"
                }
              ]
            }
          }
        }
        """;

    private const string NoMatchJson = """
        {
          "response": {
            "ssuser": { "id": "0" }
          }
        }
        """;

    private const string BothMediaTypesJson = """
        {
          "response": {
            "jeu": {
              "id": "5678",
              "noms": [{ "region": "us", "text": "Test Game" }],
              "synopsis": [],
              "genres": [],
              "dates": [],
              "developpeur": {},
              "editeur": {},
              "medias": [
                {
                  "type": "ss",
                  "region": "us",
                  "url": "https://screenscraper.fr/image.php?gameid=5678&media=ss&region=us",
                  "format": "jpg"
                },
                {
                  "type": "box-2D",
                  "region": "eu",
                  "url": "https://screenscraper.fr/image.php?gameid=5678&media=box-2D&region=eu",
                  "format": "png"
                }
              ]
            }
          }
        }
        """;

    private sealed class FakeMessageHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FakeMessageHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private static ScreenScraperClient CreateClient(string responseJson)
    {
        var httpClient = new HttpClient(new FakeMessageHandler(responseJson));
        var config = new AppConfig();
        return new ScreenScraperClient(httpClient, config);
    }

    [Fact]
    public async Task SearchByCrc32_ReturnsGameData_WhenHashMatches()
    {
        // Arrange
        var client = CreateClient(SuccessJson);

        // Act
        var result = await client.SearchByCrc32Async("A1B2C3D4", screenscraperSystemId: 4);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Super Mario World", result.Title);
        Assert.Equal("A platform game", result.Description);
        Assert.Equal("Platform", result.Genre);
        Assert.Equal(1990, result.ReleaseYear);
        Assert.Equal("Nintendo", result.Developer);
        Assert.Equal("Nintendo", result.Publisher);
        Assert.Contains("box-2D", result.CoverArtUrl);
        Assert.Equal("screenscraper", result.ScraperSource);
        Assert.Equal("1234", result.ScraperMatchId);
    }

    [Fact]
    public async Task SearchByCrc32_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var client = CreateClient(NoMatchJson);

        // Act
        var result = await client.SearchByCrc32Async("DEADBEEF", screenscraperSystemId: 4);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchByTitle_ReturnsGameData_WhenTitleMatches()
    {
        // Arrange
        var client = CreateClient(SuccessJson);

        // Act
        var result = await client.SearchByTitleAsync("Super Mario World", screenscraperSystemId: 4);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Super Mario World", result.Title);
        Assert.Equal("A platform game", result.Description);
        Assert.Equal("Nintendo", result.Developer);
        Assert.Equal("screenscraper", result.ScraperSource);
    }

    [Fact]
    public async Task CoverArtUrl_SelectsBox2D_OverScreenshot()
    {
        // Arrange: response contains both ss (screenshot) and box-2D — box-2D should be preferred
        var client = CreateClient(BothMediaTypesJson);

        // Act
        var result = await client.SearchByCrc32Async("ABCD1234", screenscraperSystemId: 4);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CoverArtUrl);
        Assert.Contains("box-2D", result.CoverArtUrl);
        Assert.DoesNotContain("media=ss", result.CoverArtUrl);
    }
}
