using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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

        // Wire up rescan trigger — subscribe with error handler to prevent crash
        if (DataContext is LibraryViewModel libraryVm)
        {
            dialog.OnSourceAdded = () =>
            {
                libraryVm.RescanCommand.Execute().Subscribe(
                    _ => { },
                    ex => Log.Error(ex, "Rescan failed after adding ROM source"));
            };
        }

        await dialog.ShowDialog(window);
    }
}
