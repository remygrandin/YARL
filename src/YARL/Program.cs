using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Avalonia.Splat;
using Serilog;
using YARL.Domain.Interfaces;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Persistence;
using YARL.Infrastructure.Providers;
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
                services.AddSingleton<LibraryViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<IScreen>(sp => sp.GetRequiredService<MainViewModel>());

                // ROM Source Providers (LIB-08)
                services.AddSingleton<IRomSourceProvider, LocalRomSourceProvider>();
                services.AddSingleton<IRomSourceProvider, OsMountedRomSourceProvider>();
            });
}
