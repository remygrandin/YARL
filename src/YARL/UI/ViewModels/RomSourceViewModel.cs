using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Domain.Enums;
using YARL.Domain.Models;

namespace YARL.UI.ViewModels;

public partial class RomSourceViewModel : ReactiveObject
{
    public int Id { get; }
    public string Path { get; }
    public SourceType SourceType { get; }

    [Reactive] private bool _isEnabled;

    public string SourceTypeLabel => SourceType == SourceType.OsMounted ? "Network Share" : "Local Folder";

    public RomSourceViewModel(RomSource source)
    {
        Id = source.Id;
        Path = source.Path;
        SourceType = source.SourceType;
        _isEnabled = source.IsEnabled;
    }
}
