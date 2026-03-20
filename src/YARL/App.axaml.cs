using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
            var libraryVm = mainVm.LibraryViewModel;

            // Determine startup mode
            var uiMode = DetermineStartupMode();
            Log.Information("Starting in {UIMode} mode", uiMode);

            // Show window immediately — migration and scan run after first render
            desktop.MainWindow = uiMode == UIMode.Fullscreen
                ? new FullscreenShell { DataContext = mainVm }
                : new DesktopShell { DataContext = mainVm };

            // Run migration on a thread-pool thread, then kick off scan on UI thread
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Run(libraryVm.RunMigration);
                Log.Information("[App] Database migration complete, firing startup scan");
                libraryVm.RescanCommand.Execute()
                    .Subscribe(
                        _ => Log.Information("[App] Startup scan completed"),
                        ex => Log.Error(ex, "[App] Startup scan threw"));
            }, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
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
