using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private async void OnBrowseEmulatorClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EmulatorRowViewModel row)
        {
            var result = await TopLevel.GetTopLevel(this)!
                .StorageProvider
                .OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = $"Select Emulator for {row.PlatformDisplayName}",
                    AllowMultiple = false
                });
            if (result.Count > 0)
                row.ExePath = result[0].Path.LocalPath;
        }
    }

    private void OnAddEmulatorPlatformClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        var available = vm.GetAvailablePlatformsForAdd();
        if (available.Count == 0) return;

        var menu = new ContextMenu();
        foreach (var (id, name) in available)
        {
            var platformId = id;
            var platformName = name;
            var item = new MenuItem { Header = name };
            item.Click += (_, _) => vm.AddEmulatorRow(platformId, platformName);
            menu.Items.Add(item);
        }
        if (sender is Button button)
        {
            menu.PlacementTarget = button;
            menu.Open(button);
        }
    }
}
