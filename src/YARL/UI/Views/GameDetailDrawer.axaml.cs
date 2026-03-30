using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using YARL.UI.Desktop;

namespace YARL.UI.Views;

public partial class GameDetailDrawer : UserControl
{
    public GameDetailDrawer()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            if (DataContext is YARL.UI.ViewModels.GameDetailViewModel vm)
                vm.CloseCommand.Execute().Subscribe();
            e.Handled = true;
        }
    }

    private void OnConfigureEmulatorClicked(object? sender, RoutedEventArgs e)
    {
        // Navigate to Settings view — find DesktopShell ancestor and trigger settings nav
        var shell = this.FindAncestorOfType<DesktopShell>();
        shell?.NavigateToSettings();
    }
}
