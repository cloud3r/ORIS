using System.Text.Json;
using CustomHttpServer.Framework.Configuation;

public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;

    public ServerSettings Server { get; }

    private ConfigurationManager()
    {
        string pathToSettingsJson = Path.Combine(
            AppContext.BaseDirectory,
            "settings.json"
        );

        if (!File.Exists(pathToSettingsJson))
            throw new FileNotFoundException("settings.json не найден!");

        string settingsJson = File.ReadAllText(pathToSettingsJson);

        ServerSettings? settings =
            JsonSerializer.Deserialize<ServerSettings>(settingsJson);

        if (settings == null ||
            string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.Path) ||
            settings.Port < 1 ||
            settings.Port > 65535)
        {
            throw new Exception("Некорректные настройки сервера!");
        }

        Server = settings;
    }

    public static ConfigurationManager GetInstance()
    {
        _instance ??= new ConfigurationManager();
        return _instance;
    }
}