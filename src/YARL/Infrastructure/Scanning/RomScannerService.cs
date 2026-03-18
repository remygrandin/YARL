using Microsoft.EntityFrameworkCore;
using YARL.Domain.Enums;
using YARL.Domain.Interfaces;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;

namespace YARL.Infrastructure.Scanning;

public class RomScannerService
{
    private readonly YarlDbContext _db;
    private readonly PlatformRegistry _registry;
    private readonly IEnumerable<IRomSourceProvider> _providers;

    public RomScannerService(
        YarlDbContext db,
        PlatformRegistry registry,
        IEnumerable<IRomSourceProvider> providers)
    {
        _db = db;
        _registry = registry;
        _providers = providers;
    }

    public async Task<ScanReport> ScanAllAsync(
        IProgress<ScanUpdate>? progress = null,
        CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return new ScanReport { CompletedAt = DateTime.UtcNow };

        var sources = await _db.RomSources
            .Where(s => s.IsEnabled)
            .ToListAsync(ct);

        int totalGamesAdded = 0;
        int totalGamesRemoved = 0;
        int platformsFound = 0;
        var unmatchedFolders = new List<string>();

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested)
                break;

            var provider = _providers.FirstOrDefault(p => p.CanHandle(source));
            if (provider is null)
                continue;

            // Collect all files from the source
            var allFiles = new List<string>();
            await foreach (var file in provider.EnumerateRomsAsync(source, ct))
                allFiles.Add(file);

            if (ct.IsCancellationRequested)
                break;

            // Group files by immediate parent folder name
            var byFolder = allFiles
                .GroupBy(f => Path.GetFileName(Path.GetDirectoryName(f) ?? "") ?? "")
                .ToDictionary(g => g.Key, g => g.ToList());

            // Load existing RomFile paths for this source into a HashSet for O(1) lookup
            var existingPaths = await _db.RomFiles
                .Where(r => r.SourceId == source.Id)
                .Select(r => r.FilePath)
                .ToHashSetAsync(ct);

            int sourceGamesAdded = 0;
            int sourceGamesRemoved = 0;
            var discoveredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (folderName, files) in byFolder)
            {
                if (ct.IsCancellationRequested)
                    break;

                var platform = _registry.Resolve(folderName);
                if (platform is null)
                {
                    unmatchedFolders.Add(folderName);
                    continue;
                }

                platformsFound++;
                var allowedFiles = files
                    .Where(f => PlatformRegistry.IsAllowedExtension(platform, f)
                                || string.Equals(Path.GetExtension(f), ".m3u", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var filePath in allowedFiles)
                    discoveredPaths.Add(filePath);

                // Pre-collect all disc paths referenced by .m3u files so we can skip
                // those files when processing them as standalone ROMs.
                var m3uDiscPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m3uFile in allowedFiles.Where(f =>
                    string.Equals(Path.GetExtension(f), ".m3u", StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (var dp in M3uParser.ParseDiscPaths(m3uFile))
                        m3uDiscPaths.Add(dp);
                }

                // Add disc paths to discoveredPaths so they don't get marked Missing
                foreach (var dp in m3uDiscPaths)
                    discoveredPaths.Add(dp);

                // Process new files
                foreach (var filePath in allowedFiles)
                {
                    if (existingPaths.Contains(filePath))
                        continue;

                    // Skip disc files that are referenced by an .m3u — they'll be
                    // imported as child RomFiles under the m3u game instead.
                    if (m3uDiscPaths.Contains(filePath))
                        continue;

                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    var isM3u = string.Equals(Path.GetExtension(filePath), ".m3u", StringComparison.OrdinalIgnoreCase);

                    var game = new Game
                    {
                        Title = FilenameParser.CleanTitle(fileNameWithoutExt),
                        RawTitle = fileNameWithoutExt,
                        PlatformId = platform.Id,
                        SourceId = source.Id,
                        Region = FilenameParser.ExtractRegion(fileNameWithoutExt),
                        CreatedAt = DateTime.UtcNow,
                        Status = GameStatus.Active
                    };

                    var romFile = new RomFile
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        FileSize = new FileInfo(filePath).Length,
                        IsM3uPlaylist = isM3u,
                        SourceId = source.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    game.RomFiles.Add(romFile);

                    // For .m3u files, parse disc references and create additional RomFiles
                    if (isM3u)
                    {
                        var discPaths = M3uParser.ParseDiscPaths(filePath);
                        for (int i = 0; i < discPaths.Count; i++)
                        {
                            var discPath = discPaths[i];
                            var discRomFile = new RomFile
                            {
                                FilePath = discPath,
                                FileName = Path.GetFileName(discPath),
                                FileSize = File.Exists(discPath) ? new FileInfo(discPath).Length : 0,
                                DiscNumber = i + 1,
                                SourceId = source.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            game.RomFiles.Add(discRomFile);
                        }
                    }

                    _db.Games.Add(game);
                    sourceGamesAdded++;
                }

                progress?.Report(new ScanUpdate(platform.Name, sourceGamesAdded, allowedFiles.Count));
            }

            // Mark removed files (in DB but not on disk) as Missing
            var removedPaths = existingPaths
                .Where(p => !discoveredPaths.Contains(p))
                .ToList();

            if (removedPaths.Count > 0)
            {
                // Find game IDs with removed RomFiles
                var gameIdsToMiss = await _db.RomFiles
                    .Where(r => removedPaths.Contains(r.FilePath))
                    .Select(r => r.GameId)
                    .Distinct()
                    .ToListAsync(ct);

                if (gameIdsToMiss.Count > 0)
                {
                    var removedCount = await _db.Games
                        .Where(g => gameIdsToMiss.Contains(g.Id) && g.Status == GameStatus.Active)
                        .ExecuteUpdateAsync(s => s.SetProperty(g => g.Status, GameStatus.Missing), ct);
                    sourceGamesRemoved = removedCount;
                }
            }

            // Restore reappeared files (in DB as Missing AND on disk)
            var reappearedPaths = existingPaths
                .Where(p => discoveredPaths.Contains(p))
                .ToList();

            if (reappearedPaths.Count > 0)
            {
                var gameIdsToRestore = await _db.RomFiles
                    .Where(r => reappearedPaths.Contains(r.FilePath))
                    .Select(r => r.GameId)
                    .Distinct()
                    .ToListAsync(ct);

                if (gameIdsToRestore.Count > 0)
                {
                    await _db.Games
                        .Where(g => gameIdsToRestore.Contains(g.Id) && g.Status == GameStatus.Missing)
                        .ExecuteUpdateAsync(s => s.SetProperty(g => g.Status, GameStatus.Active), ct);
                }
            }

            await _db.SaveChangesAsync(ct);

            // Clear change tracker so subsequent reads get fresh DB state
            _db.ChangeTracker.Clear();

            // Background hashing pass for newly added RomFiles without hashes
            await HashNewRomFilesAsync(source.Id, ct);

            totalGamesAdded += sourceGamesAdded;
            totalGamesRemoved += sourceGamesRemoved;
        }

        progress?.Report(new ScanUpdate("", totalGamesAdded, totalGamesAdded, IsComplete: true));

        return new ScanReport
        {
            PlatformsFound = platformsFound,
            GamesAdded = totalGamesAdded,
            GamesRemoved = totalGamesRemoved,
            UnmatchedFolders = unmatchedFolders,
            CompletedAt = DateTime.UtcNow
        };
    }

    private async Task HashNewRomFilesAsync(int sourceId, CancellationToken ct)
    {
        var unhashed = await _db.RomFiles
            .Where(r => r.SourceId == sourceId && r.CRC32Hash == null && !r.IsM3uPlaylist)
            .ToListAsync(ct);

        if (unhashed.Count == 0)
            return;

        // Collect results to batch-update after parallel completion
        var results = new System.Collections.Concurrent.ConcurrentBag<(int Id, string Crc32, string Md5, string Sha1)>();

        await Parallel.ForEachAsync(unhashed, new ParallelOptions
        {
            MaxDegreeOfParallelism = 2,
            CancellationToken = ct
        }, async (romFile, innerCt) =>
        {
            try
            {
                if (!File.Exists(romFile.FilePath))
                    return;

                var (crc32, md5, sha1) = await FileHasher.ComputeHashesAsync(romFile.FilePath, innerCt);
                results.Add((romFile.Id, crc32, md5, sha1));
            }
            catch (Exception)
            {
                // Skip files that are locked or inaccessible
            }
        });

        // Batch-update hashes
        foreach (var (id, crc32, md5, sha1) in results)
        {
            var romFile = unhashed.FirstOrDefault(r => r.Id == id);
            if (romFile is not null)
            {
                romFile.CRC32Hash = crc32;
                romFile.MD5Hash = md5;
                romFile.SHA1Hash = sha1;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
