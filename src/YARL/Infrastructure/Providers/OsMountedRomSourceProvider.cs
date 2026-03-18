using System.Runtime.CompilerServices;
using YARL.Domain.Enums;
using YARL.Domain.Interfaces;
using YARL.Domain.Models;

namespace YARL.Infrastructure.Providers;

/// <summary>
/// Handles OS-mounted network shares (SMB, NFS) that appear as local paths.
/// Implements LIB-08: User can tag a path as "remote" (OS-mounted network share).
/// Behavior is identical to LocalRomSourceProvider for now — the distinction is
/// in the SourceType tag, which Phase 7 (cache management) will use to decide
/// whether to cache ROMs locally before launch.
/// </summary>
public class OsMountedRomSourceProvider : IRomSourceProvider
{
    public SourceType SupportedType => SourceType.OsMounted;

    public bool CanHandle(RomSource source) =>
        source.SourceType == SourceType.OsMounted;

    public async IAsyncEnumerable<string> EnumerateRomsAsync(
        RomSource source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(source.Path))
            yield break;

        await Task.CompletedTask;
        foreach (var file in Directory.EnumerateFiles(source.Path, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            yield return file;
        }
    }
}
