using ReactiveUI;

namespace YARL.UI.ViewModels;

public class MainViewModel : ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new RoutingState();
    public LibraryViewModel LibraryViewModel { get; }

    public MainViewModel(LibraryViewModel libraryViewModel)
    {
        LibraryViewModel = libraryViewModel;
    }
}
