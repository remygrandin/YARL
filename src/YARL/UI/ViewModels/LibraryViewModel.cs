using ReactiveUI;

namespace YARL.UI.ViewModels;

/// <summary>
/// Shared ViewModel consumed by both DesktopShell and FullscreenShell.
/// Phase 2 will add ROM collection, scanning state, and filtering.
/// </summary>
public class LibraryViewModel : ReactiveObject
{
    private string _statusMessage = "YARL is ready. Add ROM folders to get started.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
}
