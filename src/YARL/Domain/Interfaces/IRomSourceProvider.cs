using YARL.Domain.Enums;
using YARL.Domain.Models;

namespace YARL.Domain.Interfaces;

public interface IRomSourceProvider
{
    SourceType SupportedType { get; }
    bool CanHandle(RomSource source);
    IAsyncEnumerable<string> EnumerateRomsAsync(RomSource source, CancellationToken ct = default);
}
