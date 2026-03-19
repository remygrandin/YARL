using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using Splat;
using YARL.Domain.Enums;
using YARL.Infrastructure.Config;
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
            // IServiceScopeFactory is a framework-internal MS DI service — Splat's adapter
            // does not expose it. We route through LibraryViewModel which receives it via
            // proper DI constructor injection in Program.cs.
            var libraryVm = Locator.Current.GetService<LibraryViewModel>()
                ?? throw new InvalidOperationException("LibraryViewModel not registered in Splat.");
            libraryVm.RunMigration();
            Log.Information("Database migration complete");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration failed");
            // Don't crash on migration failure — log and continue
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
