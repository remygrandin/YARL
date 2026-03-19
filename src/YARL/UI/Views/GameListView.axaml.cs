using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using YARL.UI.ViewModels;

namespace YARL.UI.Views;

public partial class GameListView : UserControl
{
    public GameListView()
    {
        InitializeComponent();
    }

    private void OnFavoriteClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: GameViewModel gvm }) return;
        if (DataContext is not LibraryViewModel vm) return;
        e.Handled = true; // prevent row click from also firing
        vm.ToggleFavoriteCommand.Execute(gvm).Subscribe();
    }

    private void OnBackClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LibraryViewModel vm)
        {
            vm.ShowFavoritesOnly = false;
            vm.ShowAllGames = false;
            vm.SelectedPlatform = null;
        }
    }
}
