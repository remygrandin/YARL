using System.Text.Json;

namespace YARL.Infrastructure.Config;

public class AppConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppConfig Load()
    {
        var path = AppPaths.ConfigPath;
        if (!File.Exists(path))
            return new AppConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(AppPaths.ConfigPath, json);
    }
}
