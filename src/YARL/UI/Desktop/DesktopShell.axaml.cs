using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReactiveUI;
using Splat;
using YARL.UI.ViewModels;
using YARL.UI.Views;

namespace YARL.UI.Desktop;

public partial class DesktopShell : Window
{
    private LibraryViewModel? _libraryVm;
    private LibraryView? _libraryView;
    private GameListView? _gameListView;
    private SettingsView? _settingsView;
    private IDisposable? _selectedPlatformSub;
    private IDisposable? _selectedGameSub;

    private enum NavSection { Library, AllGames, Favorites, RecentlyPlayed, Settings }
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
        _settingsView = new SettingsView { DataContext = mainVm.SettingsViewModel };

        // Watch SelectedPlatform to switch between LibraryView and GameListView
        _selectedPlatformSub?.Dispose();
        _selectedPlatformSub = _libraryVm
            .WhenAnyValue(x => x.SelectedPlatform, x => x.ShowFavoritesOnly, x => x.ShowAllGames)
            .Subscribe(t =>
            {
                var (platform, favs, all) = (t.Item1, t.Item2, t.Item3);
                // Back button in GameListView resets all flags — return to Systems view
                if (platform is null && !favs && !all && _currentNav != NavSection.Library)
                {
                    _currentNav = NavSection.Library;
                    SetActiveNav(NavLibrary);
                    ContentArea.Content = _libraryView;
                }
                else if (platform is not null && _currentNav == NavSection.Library)
                    ContentArea.Content = _gameListView;
                else if (platform is null && _currentNav == NavSection.Library && !favs && !all)
                    ContentArea.Content = _libraryView;
            });

        // Initial view
        ContentArea.Content = _libraryView;

        // Wire GameDetailDrawer: load game and toggle drawerOpen class on SelectedGame changes
        var detailVm = Locator.Current.GetService<GameDetailViewModel>();
        if (detailVm is not null && DetailDrawer is not null)
        {
            DetailDrawer.DataContext = detailVm;

            _selectedGameSub?.Dispose();
            _selectedGameSub = _libraryVm
                .WhenAnyValue(x => x.SelectedGame)
                .Subscribe(game =>
                {
                    detailVm.LoadGame(game);

                    // Apply/remove drawerOpen class on the inner Border to trigger slide animation
                    if (DetailDrawer.FindControl<Avalonia.Controls.Border>("DrawerBorder") is { } border)
                    {
                        if (game != null)
                            border.Classes.Add("drawerOpen");
                        else
                            border.Classes.Remove("drawerOpen");
                    }
                });
        }
    }

    private void OnNavLibraryClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.Library;
        SetActiveNav(NavLibrary);
        ResetFilters();

        ContentArea.Content = _libraryVm?.SelectedPlatform is not null ? _gameListView : _libraryView;
    }

    private void OnNavAllGamesClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.AllGames;
        SetActiveNav(NavAllGames);
        if (_libraryVm is not null)
        {
            // Set ShowAllGames = true FIRST so no intermediate (null, false, false) fires
            _libraryVm.ShowAllGames = true;
            _libraryVm.ShowFavoritesOnly = false;
            _libraryVm.SelectedPlatform = null;
        }
        ContentArea.Content = _gameListView;
    }

    private void OnNavFavoritesClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.Favorites;
        SetActiveNav(NavFavorites);
        if (_libraryVm is not null)
        {
            // Set ShowFavoritesOnly = true FIRST so no intermediate (null, false, false) fires
            _libraryVm.ShowFavoritesOnly = true;
            _libraryVm.ShowAllGames = false;
            _libraryVm.SelectedPlatform = null;
        }
        ContentArea.Content = _gameListView;
    }

    private void OnNavRecentlyPlayedClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.RecentlyPlayed;
        SetActiveNav(NavRecentlyPlayed);
        ResetFilters();
        ContentArea.Content = _libraryView;
    }

    private void OnNavSettingsClicked(object? sender, RoutedEventArgs e)
    {
        _currentNav = NavSection.Settings;
        SetActiveNav(NavSettings);
        ResetFilters();
        ContentArea.Content = _settingsView;
    }

    private void ResetFilters()
    {
        if (_libraryVm is null) return;
        _libraryVm.ShowFavoritesOnly = false;
        _libraryVm.ShowAllGames = false;
    }

    private void SetActiveNav(Button activeButton)
    {
        var navButtons = new[] { NavLibrary, NavAllGames, NavFavorites, NavRecentlyPlayed, NavSettings };
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

            // Update icon and label colors inside the button.
            if (btn.Content is StackPanel sp)
            {
                var color = isActive ? "#FFFFFF" : "#8888aa";
                foreach (var child in sp.Children)
                {
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
        _selectedGameSub?.Dispose();
        base.OnClosed(e);
    }
}
