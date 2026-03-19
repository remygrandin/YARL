using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using YARL.Domain.Enums;
using YARL.Domain.Models;
using YARL.Infrastructure.Persistence;

namespace YARL.UI.Dialogs;

public partial class AddRomSourceDialog : Window
{
    private readonly IServiceScopeFactory? _scopeFactory;

    /// <summary>
    /// Action invoked after a source is successfully added (for triggering a rescan).
    /// </summary>
    public Action? OnSourceAdded { get; set; }

    public AddRomSourceDialog() : this(null) { }

    public AddRomSourceDialog(IServiceScopeFactory? scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
        var options = new FolderPickerOpenOptions
        {
            Title = "Select ROM folder",
            AllowMultiple = false
        };

        var result = await StorageProvider.OpenFolderPickerAsync(options);
        if (result is { Count: > 0 } && PathTextBox is not null)
        {
            PathTextBox.Text = result[0].TryGetLocalPath() ?? result[0].Path.LocalPath;
        }
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

        if (_scopeFactory is null)
        {
            ShowValidation("Cannot save: DI services unavailable. Restart the app.");
            return;
        }

        var sourceType = (OsMountedRadio?.IsChecked == true)
            ? SourceType.OsMounted
            : SourceType.Local;

        try
        {
            await SaveRomSourceAsync(path, sourceType);
        }
        catch (Exception ex)
        {
            ShowValidation($"Failed to save: {ex.Message}");
            return;
        }

        OnSourceAdded?.Invoke();
        Close();
    }

    private async Task SaveRomSourceAsync(string path, SourceType sourceType)
    {
        using var scope = _scopeFactory!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();
        db.RomSources.Add(new RomSource
        {
            Path = path,
            SourceType = sourceType,
            IsEnabled = true
        });
        await db.SaveChangesAsync();
    }

    private void ShowValidation(string message)
    {
        if (ValidationMessage is null) return;
        ValidationMessage.Text = message;
        ValidationMessage.IsVisible = true;
    }
}
