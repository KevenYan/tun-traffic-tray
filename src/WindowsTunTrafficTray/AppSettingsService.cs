using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace WindowsTunTrafficTray;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;

    public AppSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "WindowsTunTrafficTray", "settings.json");
    }

    public AppSettings Load()
    {
        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings is not null)
            {
                return settings;
            }
        }

        return LoadDefaultsFromClashVerge();
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static AppSettings LoadDefaultsFromClashVerge()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configPath = Path.Combine(appData, "io.github.clash-verge-rev.clash-verge-rev", "config.yaml");
        var settings = new AppSettings();

        if (!File.Exists(configPath))
        {
            return settings;
        }

        var text = File.ReadAllText(configPath);
        var controller = MatchYamlScalar(text, "external-controller");
        var secret = MatchYamlScalar(text, "secret");

        if (!string.IsNullOrWhiteSpace(controller))
        {
            settings.ControllerUrl = controller.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? controller
                : $"http://{controller}";
        }

        if (!string.IsNullOrWhiteSpace(secret))
        {
            settings.Secret = secret;
        }

        return settings;
    }

    private static string MatchYamlScalar(string text, string key)
    {
        var match = Regex.Match(text, $"^{Regex.Escape(key)}:\\s*[\"']?(.*?)[\"']?\\s*$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }
}
