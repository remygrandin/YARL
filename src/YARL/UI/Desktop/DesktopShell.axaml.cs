using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReactiveUI;
using YARL.UI.ViewModels;
using YARL.UI.Views;

namespace YARL.UI.Desktop;

public partial class DesktopShell : Window
{
    private LibraryViewModel? _libraryVm;
    private LibraryView? _libraryView;
    private GameListView? _gameListView;
    private IDisposable? _selectedPlatformSub;

    private enum NavSection { Library, AllGames, Favorites, RecentlyPlayed }
    private NavSection _currentNav = NavSection.Library;

    public DesktopShell()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel mainVm) return;

        _libraryVm = mainVm.LibraryViewModel;
        _libraryView = new LibraryView { DataContext = _libraryVm };
        _gameListView = new GameListView { DataContext = _libraryVm };

        // Watch SelectedPlatform to switch between LibraryView and GameListView
        _selectedPlatformSub?.Dispose();
        _selectedPlatformSub = _libraryVm
            .WhenAnyValue(x => x.SelectedPlatform)
            .Subscribe(platform =>
            {
                if (platform is not null && _currentNav == NavSection.Library)
                    ContentArea.Content = _gameListView;
                else if (platform is null && _currentNav == NavSection.Library)
                    ContentArea.Content = _libraryView;
            });

        // Initial view
        ContentArea.Content = _libraryView;
    }

    private void OnNavLibraryClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.Library;
        SetActiveNav(NavLibrary);

        if (_libraryVm?.SelectedPlatform is not null)
            ContentArea.Content = _gameListView;
        else
            ContentArea.Content = _libraryView;
    }

    private void OnNavAllGamesClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.AllGames;
        SetActiveNav(NavAllGames);

        // Show game list with no platform filter (all games)
        if (_libraryVm is not null)
            _libraryVm.SelectedPlatform = null;
        ContentArea.Content = _gameListView;
    }

    private void OnNavFavoritesClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.Favorites;
        SetActiveNav(NavFavorites);

        // Show game list with favorites filter
        if (_libraryVm is not null)
        {
            _libraryVm.SelectedPlatform = null;
            _libraryVm.ShowFavoritesOnly = true;
        }
        ContentArea.Content = _gameListView;
    }

    private void OnNavRecentlyPlayedClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.RecentlyPlayed;
        SetActiveNav(NavRecentlyPlayed);
        ContentArea.Content = _libraryView;
    }

    private void SetActiveNav(Button activeButton)
    {
        var navButtons = new[] { NavLibrary, NavAllGames, NavFavorites, NavRecentlyPlayed };
        foreach (var btn in navButtons)
        {
            if (btn is null) continue;
            bool isActive = btn == activeButton;
            btn.Background = isActive
                ? new SolidColorBrush(Color.Parse("#0f3460"))
                : new SolidColorBrush(Colors.Transparent);
            btn.BorderBrush = isActive
                ? new SolidColorBrush(Color.Parse("#7C6FF7"))
                : new SolidColorBrush(Colors.Transparent);

            // Update icon and label colors inside the button
            if (btn.Content is Avalonia.Controls.StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    var color = isActive ? "#FFFFFF" : "#8888aa";
                    if (child is Material.Icons.Avalonia.MaterialIcon icon)
                        icon.Foreground = new SolidColorBrush(Color.Parse(color));
                    else if (child is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(Color.Parse(color));
                }
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _selectedPlatformSub?.Dispose();
        base.OnClosed(e);
    }
}
