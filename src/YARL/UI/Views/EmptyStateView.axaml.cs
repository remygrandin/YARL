using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Splat;
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

        var scopeFactory = Locator.Current.GetService<IServiceScopeFactory>();
        var dialog = new AddRomSourceDialog(scopeFactory);

        // Wire up rescan trigger
        if (DataContext is LibraryViewModel libraryVm)
        {
            dialog.OnSourceAdded = () =>
            {
                libraryVm.RescanCommand.Execute().Subscribe();
            };
        }

        await dialog.ShowDialog(window);
    }
}
