using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using YARL.Domain.Enums;

namespace YARL.UI.Dialogs;

public partial class AddRomSourceDialog : Window
{
    /// <summary>
    /// Delegate that persists the ROM source. Set by the caller.
    /// Returns true on success, false if the scope factory is unavailable.
    /// </summary>
    public Func<string, SourceType, Task<bool>>? SaveSource { get; set; }

    /// <summary>
    /// Invoked after a source is successfully saved (for triggering a rescan).
    /// </summary>
    public Action? OnSourceAdded { get; set; }

    public AddRomSourceDialog()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPathChanged(object? sender, TextChangedEventArgs e)
    {
        if (PathTextBox is null || AddSourceButton is null) return;
        AddSourceButton.IsEnabled = !string.IsNullOrWhiteSpace(PathTextBox.Text);
        if (ValidationMessage is not null)
            ValidationMessage.IsVisible = false;
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select ROM folder",
            AllowMultiple = false
        });

        if (result is { Count: > 0 } && PathTextBox is not null)
            PathTextBox.Text = result[0].TryGetLocalPath() ?? result[0].Path.LocalPath;
    }

    private async void OnAddSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (PathTextBox is null) return;

        var path = PathTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(path))
        {
            ShowValidation("Please enter a folder path.");
            return;
        }

        if (!Directory.Exists(path))
        {
            ShowValidation("Folder not found. Check the path and try again.");
            return;
        }

        if (SaveSource is null)
        {
            ShowValidation("Cannot save: no save handler configured.");
            return;
        }

        var sourceType = (OsMountedRadio?.IsChecked == true)
            ? SourceType.OsMounted
            : SourceType.Local;

        try
        {
            var saved = await SaveSource(path, sourceType);
            if (!saved)
            {
                ShowValidation("Cannot save: database unavailable. Restart the app.");
                return;
            }
        }
        catch (Exception ex)
        {
            ShowValidation($"Failed to save: {ex.Message}");
            return;
        }

        OnSourceAdded?.Invoke();
        Close();
    }

    private void ShowValidation(string message)
    {
        if (ValidationMessage is null) return;
        ValidationMessage.Text = message;
        ValidationMessage.IsVisible = true;
    }
}
