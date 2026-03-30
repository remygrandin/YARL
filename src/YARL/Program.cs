using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ReactiveUI;
using ReactiveUI.Avalonia.Splat;
using Serilog;
using YARL.Domain.Interfaces;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Images;
using YARL.Infrastructure.Launch;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Providers;
using YARL.Infrastructure.Scanning;
using YARL.Infrastructure.Scraping;
using YARL.UI.ViewModels;

namespace YARL;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(AppPaths.LogDir, "yarl-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        try
        {
            Log.Information("YARL starting up");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "YARL terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUIWithMicrosoftDependencyResolver(services =>
            {
                // Config
                services.AddSingleton<AppConfigService>();
                services.AddSingleton(sp => sp.GetRequiredService<AppConfigService>().Load());

                // Database
                services.AddDbContext<YarlDbContext>(opts =>
                    opts.UseSqlite($"Data Source={AppPaths.DatabasePath}"));

                // ViewModels
                services.AddSingleton<LibraryViewModel>(sp => new LibraryViewModel(
                    sp.GetRequiredService<PlatformRegistry>(),
                    sp.GetRequiredService<IServiceScopeFactory>()
                ));
                services.AddSingleton<SettingsViewModel>(sp => new SettingsViewModel(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<LibraryViewModel>(),
                    sp.GetRequiredService<ScrapingStatusViewModel>(),
                    sp.GetRequiredService<AppConfig>(),
                    sp.GetRequiredService<AppConfigService>(),
                    sp.GetRequiredService<PlatformRegistry>()
                ));
                services.AddSingleton<MainViewModel>(sp => new MainViewModel(
                    sp.GetRequiredService<LibraryViewModel>(),
                    sp.GetRequiredService<SettingsViewModel>(),
                    sp.GetRequiredService<LaunchOverlayViewModel>()));
                services.AddSingleton<IScreen>(sp => sp.GetRequiredService<MainViewModel>());

                // ROM Source Providers (LIB-08)
                services.AddSingleton<IRomSourceProvider, LocalRomSourceProvider>();
                services.AddSingleton<IRomSourceProvider, OsMountedRomSourceProvider>();

                // Scanning
                services.AddSingleton<PlatformRegistry>(sp =>
                {
                    var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "platforms.json");
                    return PlatformRegistry.LoadFromJson(jsonPath);
                });
                services.AddScoped<RomScannerService>();
                services.AddHostedService<RomScanHostedService>();

                // Phase 3: Scraping — HTTP clients with resilience
                services.AddHttpClient<ScreenScraperClient>()
                    .AddResilienceHandler("screenscraper", builder =>
                    {
                        builder.AddRetry(new HttpRetryStrategyOptions
                        {
                            BackoffType = DelayBackoffType.Exponential,
                            MaxRetryAttempts = 4,
                            UseJitter = true,
                            ShouldHandle = args => ValueTask.FromResult(
                                args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                                args.Outcome.Result?.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
                        });
                    });
                services.AddHttpClient<ArtCacheService>();

                // Scraper pipeline (scoped — uses scoped DbContext)
                services.AddScoped<ScraperPipeline>(sp =>
                    new ScraperPipeline(
                        sp.GetRequiredService<ScreenScraperClient>(),
                        sp.GetRequiredService<ArtCacheService>()
                    ));

                // ScrapingStatusViewModel first — deferred lambda avoids circular dep with ScraperHostedService
                services.AddSingleton<ScrapingStatusViewModel>(sp =>
                    new ScrapingStatusViewModel(
                        () => _ = sp.GetRequiredService<ScraperHostedService>().QueueAllForRescrapeAsync()));

                // ScraperHostedService — deferred lambda for progress reporting avoids circular dep
                services.AddSingleton<ScraperHostedService>(sp =>
                    new ScraperHostedService(
                        sp.GetRequiredService<IServiceScopeFactory>(),
                        sp.GetRequiredService<LibraryViewModel>(),
                        sp.GetRequiredService<PlatformRegistry>(),
                        progress => sp.GetRequiredService<ScrapingStatusViewModel>().UpdateProgress(progress)));
                services.AddHostedService(sp => sp.GetRequiredService<ScraperHostedService>());
                services.AddSingleton<GameLaunchService>(sp => new GameLaunchService(
                    sp.GetRequiredService<AppConfig>(),
                    sp.GetRequiredService<IServiceScopeFactory>()));
                services.AddSingleton<LaunchOverlayViewModel>(sp => new LaunchOverlayViewModel(
                    sp.GetRequiredService<GameLaunchService>()));

                services.AddSingleton<GameDetailViewModel>(sp =>
                    new GameDetailViewModel(
                        sp.GetRequiredService<IServiceScopeFactory>(),
                        gameId => sp.GetRequiredService<ScraperHostedService>().QueueGameAsync(gameId),
                        () => sp.GetRequiredService<LibraryViewModel>().SelectedGame = null,
                        sp.GetRequiredService<GameLaunchService>(),
                        sp.GetRequiredService<LaunchOverlayViewModel>(),
                        sp.GetRequiredService<AppConfig>()));
            });
}
