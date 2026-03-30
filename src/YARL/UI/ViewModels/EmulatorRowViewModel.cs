using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using YARL.Infrastructure.Config;

namespace YARL.UI.ViewModels;

public partial class EmulatorRowViewModel : ReactiveObject
{
    private readonly AppConfig? _appConfig;
    private readonly AppConfigService? _appConfigService;

    public string PlatformId { get; }
    public string PlatformDisplayName { get; }

    [Reactive] private string _exePath = "";
    [Reactive] private string _args = "{rompath}";
    [Reactive] private bool _isFlatpak;
    [Reactive] private bool _isPathValid;

    public bool IsFlatpakAvailable { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public EmulatorRowViewModel(
        string platformId,
        string platformDisplayName,
        AppConfig? appConfig = null,
        AppConfigService? appConfigService = null,
        EmulatorConfig? existingConfig = null)
    {
        PlatformId = platformId;
        PlatformDisplayName = platformDisplayName;
        _appConfig = appConfig;
        _appConfigService = appConfigService;

        if (existingConfig != null)
        {
            _exePath = existingConfig.ExePath;
            _args = existingConfig.Args;
            _isFlatpak = existingConfig.IsFlatpak;
        }

        // Compute path validity reactively
        this.WhenAnyValue(x => x.ExePath, x => x.IsFlatpak)
            .Select(tuple =>
            {
                var (path, isFlatpak) = tuple;
                if (isFlatpak) return true; // Flatpak app IDs are not file paths
                if (string.IsNullOrWhiteSpace(path)) return false;
                return File.Exists(path);
            })
            .Subscribe(valid => IsPathValid = valid);

        SaveCommand = ReactiveCommand.Create(Save);
    }

    private void Save()
    {
        if (_appConfig is null) return;
        _appConfig.EmulatorConfigs[PlatformId] = new EmulatorConfig
        {
            ExePath = ExePath,
            Args = string.IsNullOrWhiteSpace(Args) ? "{rompath}" : Args,
            IsFlatpak = IsFlatpak
        };
        _appConfigService?.Save(_appConfig);
        Log.Information("[EmulatorRowViewModel] Saved emulator config for platform={PlatformId}", PlatformId);
    }
}
