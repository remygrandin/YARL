using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace YARL.UI.Views;

/// <summary>
/// A single search result row displayed in the interactive search dialog.
/// </summary>
public class SearchResultItem
{
    public string Title { get; set; } = "";
    public string Year { get; set; } = "";
    public string? MatchId { get; set; }
}

/// <summary>
/// Simple view-model for the interactive search dialog (AXAML binding target).
/// </summary>
public class InteractiveSearchDialogModel : INotifyPropertyChanged
{
    private bool _hasResults;
    public bool HasResults
    {
        get => _hasResults;
        set { _hasResults = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasResults))); }
    }

    public ObservableCollection<SearchResultItem> Results { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Modal dialog for manual scraper match selection.
/// AVLN3001: requires explicit parameterless constructor.
/// </summary>
public partial class InteractiveSearchDialog : Window
{
    private readonly InteractiveSearchDialogModel _model = new();
    private Func<string, Task<IReadOnlyList<SearchResultItem>>>? _searchFunc;

    /// <summary>
    /// The match selected by the user (null if dialog was dismissed without selection).
    /// </summary>
    public SearchResultItem? SelectedMatch { get; private set; }

    /// <summary>
    /// AVLN3001-compliant parameterless constructor for AXAML hot-reload support.
    /// </summary>
    public InteractiveSearchDialog()
    {
        InitializeComponent();
        DataContext = _model;
    }

    /// <summary>
    /// Constructor used at runtime: pre-fills the search box with the game title
    /// and provides a search delegate backed by the scraper infrastructure.
    /// </summary>
    public InteractiveSearchDialog(
        string gameTitle,
        Func<string, Task<IReadOnlyList<SearchResultItem>>>? searchFunc = null)
        : this()
    {
        _searchFunc = searchFunc;

        if (DialogTitle is not null)
            DialogTitle.Text = $"Find correct match for: {gameTitle}";
        if (SearchBox is not null)
            SearchBox.Text = gameTitle;
    }

    private async void OnSearchClicked(object? sender, RoutedEventArgs e)
    {
        var term = SearchBox?.Text?.Trim();
        if (string.IsNullOrEmpty(term)) return;

        _model.Results.Clear();
        _model.HasResults = false;

        if (_searchFunc is null) return;

        var results = await _searchFunc(term);
        foreach (var r in results)
            _model.Results.Add(r);

        _model.HasResults = _model.Results.Count > 0;
    }

    private void OnSelectResultClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SearchResultItem item }) return;
        SelectedMatch = item;
        Close();
    }
}
