using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;
using YARL.UI.Dialogs;
using YARL.UI.ViewModels;

namespace YARL.UI.Views;

public partial class EmptyStateView : UserControl
{
    public EmptyStateView()
    {
        InitializeComponent();
    }

    private async void OnAddRomSourceClicked(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is null) return;

        if (DataContext is not LibraryViewModel libraryVm) return;

        var dialog = new AddRomSourceDialog
        {
            // Route persistence through the ViewModel — avoids Splat resolving
            // framework-internal IServiceScopeFactory which returns null in this app.
            SaveSource = libraryVm.AddRomSourceAsync,
            OnSourceAdded = () =>
            {
                libraryVm.RescanCommand.Execute().Subscribe(
                    _ => { },
                    ex => Log.Error(ex, "Rescan failed after adding ROM source"));
            }
        };

        await dialog.ShowDialog(window);
    }
}
