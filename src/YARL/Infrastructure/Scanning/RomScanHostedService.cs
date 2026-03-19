using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using YARL.Infrastructure.Persistence;
using YARL.UI.ViewModels;

namespace YARL.Infrastructure.Scanning;

public class RomScanHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LibraryViewModel _libraryVm;

    public RomScanHostedService(IServiceScopeFactory scopeFactory, LibraryViewModel libraryVm)
    {
        _scopeFactory = scopeFactory;
        _libraryVm = libraryVm;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately to not block app startup
        await Task.Yield();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<RomScannerService>();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

            _libraryVm.IsScanning = true;
            _libraryVm.ScanProgressText = "Scanning ROM library...";

            var progress = new Progress<ScanUpdate>(update =>
            {
                if (update.IsComplete)
                    _libraryVm.ScanProgressText = "Finishing scan...";
                else
                    _libraryVm.ScanProgressText = $"Scanning {update.PlatformName}: {update.TotalProcessed} files processed...";
            });

            var report = await scanner.ScanAllAsync(progress, stoppingToken);

            // Populate SourceCache from DB after scan completes
            await _libraryVm.LoadGamesFromDbAsync(db);

            _libraryVm.IsScanning = false;
            var total = _libraryVm.AllGames.Count;
            _libraryVm.ScanProgressText = $"{report.GamesAdded} new games found  |  {report.GamesRemoved} games disabled  |  {report.GamesScanned} total scanned";
            _libraryVm.StatusMessage = $"Library ready — {total} games across {report.PlatformsFound} platforms.";
        }
        catch (OperationCanceledException)
        {
            _libraryVm.IsScanning = false;
            _libraryVm.ScanProgressText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ROM scanner encountered an unhandled error");
            _libraryVm.IsScanning = false;
            _libraryVm.ScanProgressText = "Scan failed. Check logs for details.";
        }
    }
}
