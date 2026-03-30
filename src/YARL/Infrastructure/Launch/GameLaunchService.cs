using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Persistence;

namespace YARL.Infrastructure.Launch;

public enum LaunchOverlayState
{
    Hidden,
    Launching,
    Running,
    Failed
}

public class GameLaunchService
{
    private readonly AppConfig _appConfig;
    private readonly IServiceScopeFactory _scopeFactory;
    private Process? _process;
    private int _currentGameId;
    private DateTime _launchTime;
    private Action<LaunchOverlayState, string?>? _onStateChanged;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public GameLaunchService(AppConfig appConfig, IServiceScopeFactory scopeFactory)
    {
        _appConfig = appConfig;
        _scopeFactory = scopeFactory;
    }

    public void SetStateCallback(Action<LaunchOverlayState, string?> callback)
    {
        _onStateChanged = callback;
    }

    public ProcessStartInfo BuildStartInfo(EmulatorConfig config, string romPath)
    {
        string substitutedArgs = config.Args.Replace("{rompath}", $"\"{romPath}\"");

        if (config.IsFlatpak)
        {
            return new ProcessStartInfo
            {
                FileName = "flatpak",
                Arguments = $"run {config.ExePath} {substitutedArgs}",
                UseShellExecute = false
            };
        }

        return new ProcessStartInfo
        {
            FileName = config.ExePath,
            Arguments = substitutedArgs,
            UseShellExecute = false
        };
    }

    public async Task LaunchGameAsync(int gameId, string platformId, string romPath, string gameTitle)
    {
        if (!_appConfig.EmulatorConfigs.TryGetValue(platformId, out var config))
        {
            _onStateChanged?.Invoke(LaunchOverlayState.Failed, $"No emulator configured for platform '{platformId}'.");
            return;
        }

        _currentGameId = gameId;
        _launchTime = DateTime.UtcNow;
        _onStateChanged?.Invoke(LaunchOverlayState.Launching, null);

        try
        {
            var psi = BuildStartInfo(config, romPath);
            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.Exited += OnProcessExited;

            if (!_process.Start())
            {
                _onStateChanged?.Invoke(LaunchOverlayState.Failed, "Failed to start emulator process.");
                return;
            }

            // Update LastPlayedAt immediately on launch
            await UpdateLastPlayedAtAsync(gameId);

            // Brief delay to detect immediate crash (grace period)
            await Task.Delay(2000);

            if (_process.HasExited && _process.ExitCode != 0)
            {
                _onStateChanged?.Invoke(LaunchOverlayState.Failed,
                    $"Emulator exited immediately with code {_process.ExitCode}. If using Flatpak, try adding --filesystem=host to your args.");
                return;
            }

            _onStateChanged?.Invoke(LaunchOverlayState.Running, null);

            Log.Information("[GameLaunchService] Launched game id={GameId} platform={Platform} pid={Pid}",
                gameId, platformId, _process.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GameLaunchService] Failed to launch game id={GameId}", gameId);
            _onStateChanged?.Invoke(LaunchOverlayState.Failed, $"Launch failed: {ex.Message}");
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var duration = DateTime.UtcNow - _launchTime;
        var gameId = _currentGameId;
        Log.Information("[GameLaunchService] Process exited for game id={GameId} duration={Duration}", gameId, duration);

        // Fire-and-forget DB update (runs on thread pool)
        _ = UpdatePlayTimeAsync(gameId, duration);

        // Update overlay on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _onStateChanged?.Invoke(LaunchOverlayState.Hidden, null);
        });
    }

    public async Task UpdatePlayTimeAsync(int gameId, TimeSpan duration)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
            var game = await db.Games.FindAsync(gameId);
            if (game is null) return;
            game.LastPlayedAt = DateTime.UtcNow;
            game.TotalPlayTime += duration;
            await db.SaveChangesAsync();
            Log.Information("[GameLaunchService] Updated play time for game id={GameId} total={Total}",
                gameId, game.TotalPlayTime);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GameLaunchService] Failed to update play time for game id={GameId}", gameId);
        }
    }

    private async Task UpdateLastPlayedAtAsync(int gameId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
            var game = await db.Games.FindAsync(gameId);
            if (game is null) return;
            game.LastPlayedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GameLaunchService] Failed to update LastPlayedAt for game id={GameId}", gameId);
        }
    }

    public void BringToFront()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (_process is { HasExited: false } && _process.MainWindowHandle != IntPtr.Zero)
            SetForegroundWindow(_process.MainWindowHandle);
    }
}
