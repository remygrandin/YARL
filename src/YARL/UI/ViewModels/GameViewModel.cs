using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Domain.Enums;
using YARL.Domain.Models;

namespace YARL.UI.ViewModels;

public partial class GameViewModel : ReactiveObject
{
    public int Id { get; }
    public string Title { get; }
    public string RawTitle { get; }
    public string PlatformId { get; }
    public string? Region { get; }
    public GameStatus Status { get; }
    public DateTime CreatedAt { get; }

    [Reactive] private bool _isFavorite;
    [Reactive] private DateTime? _lastPlayedAt;

    public GameViewModel(Game game)
    {
        Id = game.Id;
        Title = game.Title;
        RawTitle = game.RawTitle;
        PlatformId = game.PlatformId;
        Region = game.Region;
        Status = game.Status;
        CreatedAt = game.CreatedAt;
        _isFavorite = game.IsFavorite;
        _lastPlayedAt = game.LastPlayedAt;
    }
}
