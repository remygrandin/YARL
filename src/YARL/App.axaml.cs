using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Splat;
using YARL.Domain.Enums;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Persistence;
using YARL.UI.Desktop;
using YARL.UI.Fullscreen;
using YARL.UI.ViewModels;

namespace YARL;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Locator.Current.GetService<MainViewModel>()!;

            // Run database migration
            RunMigrations();

            // Determine startup mode
            var uiMode = DetermineStartupMode();
            Log.Information("Starting in {UIMode} mode", uiMode);

            // Create the appropriate shell
            desktop.MainWindow = uiMode == UIMode.Fullscreen
                ? new FullscreenShell { DataContext = mainVm }
                : new DesktopShell { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RunMigrations()
    {
        try
        {
            var serviceProvider = Locator.Current.GetService<IServiceProvider>()!;
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
            db.Database.Migrate();
            Log.Information("Database migration complete");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration failed");
            // Don't crash on migration failure — log and continue
            // The app will work but DB features may be limited
        }
    }

    private static UIMode DetermineStartupMode()
    {
        try
        {
            var config = Locator.Current.GetService<AppConfig>();
            if (config?.UiModeOverride is { } forced)
                return forced;

            // Simple gamepad detection — keep it basic per CONTEXT.md
            // Check if any gamepad/joystick device exists
            if (OperatingSystem.IsLinux())
            {
                // Check /dev/input/js* devices
                var jsDevices = Directory.GetFiles("/dev/input/", "js*");
                if (jsDevices.Length > 0)
                {
                    Log.Information("Gamepad detected on Linux ({Count} js devices), starting fullscreen", jsDevices.Length);
                    return UIMode.Fullscreen;
                }
            }

            // Default to desktop mode
            return UIMode.Desktop;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Gamepad detection failed, defaulting to desktop mode");
            return UIMode.Desktop;
        }
    }
}
