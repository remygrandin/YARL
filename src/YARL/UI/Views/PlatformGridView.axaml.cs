using Avalonia.Controls;
using Avalonia.Interactivity;
using YARL.UI.ViewModels;

namespace YARL.UI.Views;

public partial class PlatformGridView : UserControl
{
    public PlatformGridView()
    {
        InitializeComponent();
    }

    private void OnPlatformTileClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PlatformViewModel platform)
        {
            // Find the parent LibraryViewModel and set SelectedPlatform
            if (DataContext is LibraryViewModel libraryVm)
            {
                libraryVm.SelectedPlatform = platform;
            }
        }
    }
}
