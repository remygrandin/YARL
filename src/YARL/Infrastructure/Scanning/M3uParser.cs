namespace YARL.Infrastructure.Scanning;

public static class M3uParser
{
    /// <summary>
    /// Reads a .m3u playlist file and returns the absolute paths of the referenced disc files.
    /// Blank lines and comment lines (starting with '#') are skipped.
    /// Relative paths are resolved against the directory containing the .m3u file.
    /// </summary>
    public static IReadOnlyList<string> ParseDiscPaths(string m3uFilePath)
    {
        var directory = Path.GetDirectoryName(m3uFilePath) ?? "";
        var results = new List<string>();

        foreach (var line in File.ReadAllLines(m3uFilePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var fullPath = Path.IsPathRooted(trimmed)
                ? trimmed
                : Path.GetFullPath(Path.Combine(directory, trimmed));

            results.Add(fullPath);
        }

        return results;
    }
}
