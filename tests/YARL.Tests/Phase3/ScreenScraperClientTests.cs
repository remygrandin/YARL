using Xunit;

namespace YARL.Tests.Phase3;

public class ScreenScraperClientTests
{
    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task SearchByCrc32_ReturnsGameData_WhenHashMatches()
    {
        // Arrange: mock HttpMessageHandler returning JSON with response.jeu
        // Act: call ScreenScraperClient.SearchByCrc32Async("A1B2C3D4", systemId: 4)
        // Assert: returned metadata contains title, genre, year, developer, publisher, cover URL
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task SearchByCrc32_ReturnsNull_WhenNoMatch()
    {
        // Arrange: mock HttpMessageHandler returning JSON with no jeu key
        // Act: call SearchByCrc32Async with unknown hash
        // Assert: returns null
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task SearchByTitle_ReturnsGameData_WhenTitleMatches()
    {
        // Arrange: mock response with title search result
        // Act: call SearchByTitleAsync("Super Mario World", systemId: 4)
        // Assert: returned metadata contains expected fields
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task CoverArtUrl_SelectsBox2D_OverScreenshot()
    {
        // Arrange: mock response with both box-2D and ss media types
        // Act: extract cover art URL
        // Assert: box-2D URL is selected
        await Task.CompletedTask;
    }
}
