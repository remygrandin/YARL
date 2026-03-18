using System.Text.RegularExpressions;

namespace YARL.Infrastructure.Scanning;

public static class FilenameParser
{
    private static readonly Regex TagsRegex = new(
        @"\s*[\(\[][^\)\]]*[\)\]]",
        RegexOptions.Compiled);

    private static readonly Regex RegionRegex = new(
        @"[\(\[](USA|Europe|Japan|World|En|Fr|De|Es|It|Pt|Brazil|Korea|China|Asia|Australia)[,\s\)\]]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Strips all parenthesized and bracketed tags from the filename (without extension), then trims whitespace.
    /// </summary>
    public static string CleanTitle(string fileNameWithoutExtension)
        => TagsRegex.Replace(fileNameWithoutExtension, "").Trim();

    /// <summary>
    /// Extracts the region tag from the filename (without extension). Returns null if not found.
    /// </summary>
    public static string? ExtractRegion(string fileNameWithoutExtension)
    {
        var match = RegionRegex.Match(fileNameWithoutExtension);
        return match.Success ? match.Groups[1].Value : null;
    }
}
