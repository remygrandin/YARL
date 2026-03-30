using System;
using Avalonia.Controls;
using YARL.Infrastructure.Launch;
using YARL.UI.ViewModels;

namespace YARL.UI.Controls;

public partial class LaunchOverlay : UserControl
{
    public LaunchOverlay()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is LaunchOverlayViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.State))
                    UpdatePanelVisibility(vm.State);
            };
            UpdatePanelVisibility(vm.State);
        }
    }

    private void UpdatePanelVisibility(LaunchOverlayState state)
    {
        var launching = this.FindControl<StackPanel>("LaunchingPanel");
        var running = this.FindControl<StackPanel>("RunningPanel");
        var failed = this.FindControl<StackPanel>("FailedPanel");
        if (launching != null) launching.IsVisible = state == LaunchOverlayState.Launching;
        if (running != null) running.IsVisible = state == LaunchOverlayState.Running;
        if (failed != null) failed.IsVisible = state == LaunchOverlayState.Failed;
    }
}
