using System.Reactive;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using YARL.Domain.Enums;
using YARL.Infrastructure.Persistence;

namespace YARL.UI.ViewModels;

public partial class GameDetailViewModel : ReactiveObject
{
    private readonly IServiceScopeFactory? _scopeFactory;

    // View-mode read-only data (from selected GameViewModel)
    [Reactive] private GameViewModel? _game;
    [Reactive] private bool _isEditMode;
    [Reactive] private bool _isSaving;

    // Editable fields (bound to TextBoxes in edit mode)
    [Reactive] private string? _editTitle;
    [Reactive] private string? _editDescription;
    [Reactive] private string? _editGenre;
    [Reactive] private int? _editReleaseYear;
    [Reactive] private string? _editDeveloper;
    [Reactive] private string? _editPublisher;
    [Reactive] private string? _editCoverArtPath;

    // Regional variants (read-only in detail drawer)
    [Reactive] private bool _isAliasesExpanded;

    // Region override
    [Reactive] private string? _preferredRegion;

    // Commands
    public ReactiveCommand<Unit, Unit> EnterEditModeCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> DiscardCommand { get; }
    public ReactiveCommand<Unit, Unit> RevertOverrideCommand { get; }
    public ReactiveCommand<string, Unit> PickCoverArtCommand { get; }

    public GameDetailViewModel(IServiceScopeFactory? scopeFactory = null)
    {
        _scopeFactory = scopeFactory;

        EnterEditModeCommand = ReactiveCommand.Create(() =>
        {
            if (_game == null) return;
            _editTitle = _game.Title;
            _editDescription = _game.Description;
            _editGenre = _game.Genre;
            _editReleaseYear = _game.ReleaseYear;
            _editDeveloper = _game.Developer;
            _editPublisher = _game.Publisher;
            _editCoverArtPath = _game.CoverArtPath;
            IsEditMode = true;
        });

        var canSave = this.WhenAnyValue(x => x.IsEditMode, x => x.IsSaving,
            (edit, saving) => edit && !saving);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveOverrideAsync, canSave);

        DiscardCommand = ReactiveCommand.Create(() => { IsEditMode = false; });

        RevertOverrideCommand = ReactiveCommand.CreateFromTask(RevertOverrideAsync);

        PickCoverArtCommand = ReactiveCommand.Create<string>(path =>
        {
            EditCoverArtPath = path;
        });
    }

    public void LoadGame(GameViewModel? game)
    {
        Game = game;
        IsEditMode = false;
    }

    private async Task SaveOverrideAsync()
    {
        if (_game == null || _scopeFactory == null) return;
        IsSaving = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

            var entity = await db.Games.FindAsync(_game.Id);
            if (entity == null) return;

            entity.Description = EditDescription;
            entity.Genre = EditGenre;
            entity.ReleaseYear = EditReleaseYear;
            entity.Developer = EditDeveloper;
            entity.Publisher = EditPublisher;
            entity.IsMetadataOverridden = true;
            entity.ScrapeStatus = ScrapeStatus.Override;

            if (EditCoverArtPath != _game.CoverArtPath)
            {
                entity.CoverArtPath = EditCoverArtPath;
            }

            await db.SaveChangesAsync();

            // Update the GameViewModel in-place so UI reflects changes
            _game.CoverArtPath = entity.CoverArtPath;
            _game.ScrapeStatus = entity.ScrapeStatus;
            _game.IsMetadataOverridden = true;

            IsEditMode = false;
            Log.Information("[GameDetailViewModel] Saved metadata override for game id={Id}", _game.Id);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task RevertOverrideAsync()
    {
        if (_game == null || _scopeFactory == null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YarlDbContext>();

        await db.Games
            .Where(g => g.Id == _game.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.IsMetadataOverridden, false)
                .SetProperty(g => g.ScrapeStatus, ScrapeStatus.Pending));

        _game.IsMetadataOverridden = false;
        _game.ScrapeStatus = ScrapeStatus.Pending;

        Log.Information("[GameDetailViewModel] Reverted metadata override for game id={Id}", _game.Id);
    }
}
