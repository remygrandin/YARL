using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace YARL.UI.ViewModels;

public partial class PlatformViewModel : ReactiveObject
{
    public string Id { get; }
    public string Name { get; }

    [Reactive] private int _gameCount;

    public PlatformViewModel(string id, string name)
    {
        Id = id;
        Name = name;
    }
}
