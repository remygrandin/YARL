namespace YARL.Infrastructure.Scraping;

public interface IMetadataScraper
{
    string SourceName { get; }
    Task<ScrapedGameData?> SearchByCrc32Async(string crc32, int screenscraperSystemId, CancellationToken ct = default);
    Task<ScrapedGameData?> SearchByTitleAsync(string title, int screenscraperSystemId, CancellationToken ct = default);
}
