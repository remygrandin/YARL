using Xdg.Directories;

namespace YARL.Infrastructure.Config;

public static class AppPaths
{
    private static readonly string DataDir =
        Path.Combine(BaseDirectory.DataHome, "yarl");

    public static string DatabasePath => Path.Combine(DataDir, "library.db");
    public static string ConfigPath => Path.Combine(DataDir, "config.json");
    public static string ArtCacheDir => Path.Combine(DataDir, "art");
    public static string LogDir => Path.Combine(DataDir, "logs");

    static AppPaths()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ArtCacheDir);
        Directory.CreateDirectory(LogDir);
    }
}
