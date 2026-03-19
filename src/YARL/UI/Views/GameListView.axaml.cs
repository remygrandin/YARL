using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using YARL.UI.ViewModels;

namespace YARL.UI.Views;

public partial class GameListView : UserControl
{
    public GameListView()
    {
        InitializeComponent();
    }

    private Control? GetGridRepeater() => this.FindControl<Control>("GridRepeater");
    private Control? GetListRepeater() => this.FindControl<Control>("ListRepeater");
    private ToggleButton? GetGridToggle() => this.FindControl<ToggleButton>("GridToggle");
    private ToggleButton? GetListToggle() => this.FindControl<ToggleButton>("ListToggle");

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

    private void OnGridToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.IsChecked == true)
        {
            var grid = GetGridRepeater();
            var list = GetListRepeater();
            if (grid is not null) grid.IsVisible = true;
            if (list is not null) list.IsVisible = false;
            var listToggle = GetListToggle();
            if (listToggle is not null) listToggle.IsChecked = false;
        }
    }

    private void OnListToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.IsChecked == true)
        {
            var grid = GetGridRepeater();
            var list = GetListRepeater();
            if (grid is not null) grid.IsVisible = false;
            if (list is not null) list.IsVisible = true;
            var gridToggle = GetGridToggle();
            if (gridToggle is not null) gridToggle.IsChecked = false;
        }
    }
}
