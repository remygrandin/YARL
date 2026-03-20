using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;
using YARL.UI.Dialogs;
using YARL.UI.ViewModels;

namespace YARL.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is SettingsViewModel vm)
        {
            Log.Debug("[SettingsView] OnLoaded — loading ROM sources");
            await vm.LoadSourcesAsync();
        }
    }

    private async void OnAddSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is null) return;

        Log.Debug("[SettingsView] Opening AddRomSourceDialog");
        var dialog = new AddRomSourceDialog
        {
            SaveSource = vm.AddSourceAsync,
            OnSourceAdded = null // vm.AddSourceAsync already adds to RomSources collection
        };

        await dialog.ShowDialog(window);
    }

    private void OnToggleSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RomSourceViewModel vm }) return;
        if (DataContext is not SettingsViewModel settingsVm) return;
        Log.Debug("[SettingsView] Toggle clicked for source id={Id}", vm.Id);
        settingsVm.ToggleSourceCommand.Execute(vm).Subscribe();
    }

    private void OnRemoveSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RomSourceViewModel vm }) return;
        if (DataContext is not SettingsViewModel settingsVm) return;
        Log.Debug("[SettingsView] Remove clicked for source id={Id}", vm.Id);
        settingsVm.RemoveSourceCommand.Execute(vm).Subscribe();
    }
}
