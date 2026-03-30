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

    // Saved state for dirty tracking
    private string _savedExePath = "";
    private string _savedArgs = "{rompath}";
    private bool _savedIsFlatpak;

    public string PlatformId { get; }
    public string PlatformDisplayName { get; }

    [Reactive] private string _exePath = "";
    [Reactive] private string _args = "{rompath}";
    [Reactive] private bool _isFlatpak;
    [Reactive] private bool _isPathValid;
    [Reactive] private bool _hasUnsavedChanges;
    [Reactive] private string _exeLabel = "Path:";
    [Reactive] private string _exeWatermark = "Path to emulator executable";
    [Reactive] private string _pathStatusTooltip = "Path not set";

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

        // Store saved baseline for dirty detection
        _savedExePath = _exePath;
        _savedArgs = _args;
        _savedIsFlatpak = _isFlatpak;

        // Path validity + tooltip — computed together from ExePath and IsFlatpak
        this.WhenAnyValue(x => x.ExePath, x => x.IsFlatpak)
            .Subscribe(tuple =>
            {
                var (path, isFlatpak) = tuple;
                if (isFlatpak)
                {
                    var hasId = !string.IsNullOrWhiteSpace(path);
                    IsPathValid = hasId;
                    PathStatusTooltip = hasId ? "App ID configured" : "App ID not set";
                }
                else if (string.IsNullOrWhiteSpace(path))
                {
                    IsPathValid = false;
                    PathStatusTooltip = "Path not set";
                }
                else if (File.Exists(path))
                {
                    IsPathValid = true;
                    PathStatusTooltip = "Path OK";
                }
                else
                {
                    IsPathValid = false;
                    PathStatusTooltip = "File not found";
                }
            });

        // Label and watermark switch on Flatpak toggle
        this.WhenAnyValue(x => x.IsFlatpak)
            .Subscribe(isFlatpak =>
            {
                ExeLabel = isFlatpak ? "App ID:" : "Path:";
                ExeWatermark = isFlatpak ? "Flatpak app ID (e.g. org.DolphinEmu.dolphin-emu)" : "Path to emulator executable";
            });

        // Dirty tracking — has anything changed vs. last saved state?
        this.WhenAnyValue(x => x.ExePath, x => x.Args, x => x.IsFlatpak)
            .Select(_ => ExePath != _savedExePath || Args != _savedArgs || IsFlatpak != _savedIsFlatpak)
            .Subscribe(dirty => HasUnsavedChanges = dirty);

        // Save is only enabled when there are unsaved changes
        var canSave = this.WhenAnyValue(x => x.HasUnsavedChanges);
        SaveCommand = ReactiveCommand.Create(Save, canSave);
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

        // Reset dirty state
        _savedExePath = ExePath;
        _savedArgs = Args;
        _savedIsFlatpak = IsFlatpak;
        HasUnsavedChanges = false;
    }
}
