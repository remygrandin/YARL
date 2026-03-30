using System.Text.Json;

namespace YARL.Infrastructure.Scanning;

public class PlatformRegistry
{
    private readonly Dictionary<string, PlatformDefinition> _byName;
    private readonly Dictionary<string, PlatformDefinition> _byAlias;

    public IReadOnlyList<PlatformDefinition> AllPlatforms { get; }

    public PlatformRegistry(IEnumerable<PlatformDefinition> platforms)
    {
        var list = platforms.ToList();
        AllPlatforms = list.AsReadOnly();
        _byName = new Dictionary<string, PlatformDefinition>(StringComparer.OrdinalIgnoreCase);
        _byAlias = new Dictionary<string, PlatformDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var platform in list)
        {
            _byName[platform.Name] = platform;
            foreach (var alias in platform.Aliases)
                _byAlias[alias] = platform;
        }
    }

    /// <summary>
    /// Resolves a folder name to a PlatformDefinition via case-insensitive name or alias lookup.
    /// Returns null if no match found.
    /// </summary>
    public PlatformDefinition? Resolve(string folderName)
    {
        if (_byName.TryGetValue(folderName, out var byName))
            return byName;
        if (_byAlias.TryGetValue(folderName, out var byAlias))
            return byAlias;
        return null;
    }

    /// <summary>
    /// Returns true if the file extension is in the platform's allowed extensions list (case-insensitive).
    /// </summary>
    public static bool IsAllowedExtension(PlatformDefinition platform, string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return platform.Extensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads platforms.json and creates a PlatformRegistry from the deserialized definitions.
    /// </summary>
    public static PlatformRegistry LoadFromJson(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<PlatformsRoot>(json, options)
            ?? throw new InvalidOperationException($"Failed to deserialize platforms.json at {jsonPath}");
        return new PlatformRegistry(root.Platforms);
    }
}
