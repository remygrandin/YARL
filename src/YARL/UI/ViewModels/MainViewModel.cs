using ReactiveUI;

namespace YARL.UI.ViewModels;

public class MainViewModel : ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new RoutingState();
    public LibraryViewModel LibraryViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public LaunchOverlayViewModel LaunchOverlay { get; }

    public MainViewModel(LibraryViewModel libraryViewModel, SettingsViewModel settingsViewModel, LaunchOverlayViewModel launchOverlayViewModel)
    {
        LibraryViewModel = libraryViewModel;
        SettingsViewModel = settingsViewModel;
        LaunchOverlay = launchOverlayViewModel;
    }
}
