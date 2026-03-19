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
        Log.Debug("[EmptyStateView] Add ROM Source clicked");
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is null)
        {
            Log.Warning("[EmptyStateView] TopLevel is null — cannot open dialog");
            return;
        }

        if (DataContext is not LibraryViewModel libraryVm)
        {
            Log.Warning("[EmptyStateView] DataContext is not LibraryViewModel (was: {Type})", DataContext?.GetType().Name ?? "null");
            return;
        }

        Log.Debug("[EmptyStateView] Opening AddRomSourceDialog");
        var dialog = new AddRomSourceDialog
        {
            SaveSource = libraryVm.AddRomSourceAsync,
            OnSourceAdded = () =>
            {
                Log.Information("[EmptyStateView] Source saved — firing RescanCommand");
                libraryVm.RescanCommand.Execute().Subscribe(
                    _ => Log.Debug("[EmptyStateView] RescanCommand completed"),
                    ex => Log.Error(ex, "[EmptyStateView] RescanCommand threw after add"));
            }
        };

        await dialog.ShowDialog(window);
        Log.Debug("[EmptyStateView] Dialog closed");
    }
}
