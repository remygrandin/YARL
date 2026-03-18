using System.Runtime.CompilerServices;
using YARL.Domain.Enums;
using YARL.Domain.Interfaces;
using YARL.Domain.Models;

namespace YARL.Infrastructure.Providers;

public class LocalRomSourceProvider : IRomSourceProvider
{
    public SourceType SupportedType => SourceType.Local;

    public bool CanHandle(RomSource source) =>
        source.SourceType == SourceType.Local;

    public async IAsyncEnumerable<string> EnumerateRomsAsync(
        RomSource source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(source.Path))
            yield break;

        await Task.CompletedTask; // Ensure async enumerable compiles
        foreach (var file in Directory.EnumerateFiles(source.Path, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            yield return file;
        }
    }
}
