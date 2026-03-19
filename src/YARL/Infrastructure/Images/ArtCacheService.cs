using Serilog;
using YARL.Infrastructure.Config;

namespace YARL.Infrastructure.Images;

/// <summary>
/// Downloads cover art from a remote URL and saves it to the local art cache directory.
/// Cache path: {AppPaths.ArtCacheDir}/{platformId}/{gameId}.{ext}
/// </summary>
public class ArtCacheService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public ArtCacheService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _logger = Log.ForContext<ArtCacheService>();
    }

    /// <summary>
    /// Downloads cover art from URL and saves to local cache.
    /// Returns local file path on success, null on failure.
    /// </summary>
    public async Task<string?> DownloadAndCacheAsync(
        string imageUrl, string platformId, int gameId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        try
        {
            var dir = Path.Combine(AppPaths.ArtCacheDir, platformId);
            Directory.CreateDirectory(dir);

            string extension;
            try
            {
                extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            }
            catch
            {
                extension = ".jpg";
            }
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";

            var localPath = Path.Combine(dir, $"{gameId}{extension}");

            var response = await _httpClient.GetAsync(imageUrl, ct);
            response.EnsureSuccessStatusCode();

            await using var fs = File.Create(localPath);
            await response.Content.CopyToAsync(fs, ct);

            return localPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning(ex, "[ArtCacheService] Failed to download cover art from {Url}", imageUrl);
            return null;
        }
    }

    /// <summary>
    /// Returns the expected local path for a game's cover art (may not exist yet).
    /// </summary>
    public static string GetExpectedPath(string platformId, int gameId, string extension = ".jpg")
    {
        return Path.Combine(AppPaths.ArtCacheDir, platformId, $"{gameId}{extension}");
    }
}
