using Avalonia.Controls;
using Avalonia.Input;

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
            // Signal parent to close drawer via IsVisible or drawer-open class
            IsVisible = false;
            e.Handled = true;
        }
    }
}
