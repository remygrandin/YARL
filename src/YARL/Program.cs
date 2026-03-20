using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ReactiveUI;
using ReactiveUI.Avalonia.Splat;
using Serilog;
using YARL.Domain.Interfaces;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Images;
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
                    sp.GetRequiredService<ScrapingStatusViewModel>()
                ));
                services.AddSingleton<MainViewModel>();
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

                // IConfiguration (for ScreenScraperClient credential lookup)
                services.AddSingleton<IConfiguration>(_ =>
                    new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .Build());

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

                // IGDB client (uses kamranayub/igdb-dotnet SDK)
                services.AddSingleton(_ =>
                {
                    var clientId = Environment.GetEnvironmentVariable("YARL_IGDB_CLIENT_ID") ?? "";
                    var clientSecret = Environment.GetEnvironmentVariable("YARL_IGDB_CLIENT_SECRET") ?? "";
                    return IGDB.IGDBClient.CreateWithDefaults(clientId, clientSecret);
                });
                services.AddScoped<IgdbClient>();

                // Scraper pipeline (scoped — uses scoped DbContext)
                services.AddScoped<ScraperPipeline>(sp =>
                    new ScraperPipeline(
                        sp.GetRequiredService<ScreenScraperClient>(),
                        sp.GetRequiredService<IgdbClient>(),
                        sp.GetRequiredService<ArtCacheService>()
                    ));

                // ScraperHostedService (singleton — registered before ScrapingStatusViewModel)
                services.AddSingleton<ScraperHostedService>();
                services.AddHostedService(sp => sp.GetRequiredService<ScraperHostedService>());

                // Scraping ViewModels (singleton — depend on ScraperHostedService)
                services.AddSingleton<ScrapingStatusViewModel>(sp =>
                {
                    var hostedService = sp.GetRequiredService<ScraperHostedService>();
                    return new ScrapingStatusViewModel(() => _ = hostedService.QueueAllForRescrapeAsync());
                });
                services.AddSingleton<GameDetailViewModel>(sp =>
                    new GameDetailViewModel(sp.GetRequiredService<IServiceScopeFactory>()));
            });
}
