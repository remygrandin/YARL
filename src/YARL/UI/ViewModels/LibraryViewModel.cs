using ReactiveUI;

namespace YARL.UI.ViewModels;

/// <summary>
/// Shared ViewModel consumed by both DesktopShell and FullscreenShell.
/// </summary>
public class LibraryViewModel : ReactiveObject
{
    private string _statusMessage = "YARL is ready. Add ROM folders to get started.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    private string _scanProgressText = "";
    public string ScanProgressText
    {
        get => _scanProgressText;
        set => this.RaiseAndSetIfChanged(ref _scanProgressText, value);
    }
}
