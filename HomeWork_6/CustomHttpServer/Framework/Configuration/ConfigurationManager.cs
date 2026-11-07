using System.Text.Json;

namespace CustomHttpServer.Framework.Configuration;

public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;

    public ServerSettings Server { get; }
    

    private ConfigurationManager()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        string settingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "settings.json"
        );

        string secretsPath = Path.Combine(
            AppContext.BaseDirectory,
            "secret.json"
        );

        if (!File.Exists(settingsPath))
            throw new FileNotFoundException("Не найден settings.json.");

        if (!File.Exists(secretsPath))
            throw new FileNotFoundException("Не найден secret.json.");

        Server = JsonSerializer.Deserialize<ServerSettings>(
            File.ReadAllText(settingsPath),
            options
        ) ?? throw new InvalidOperationException(
            "Не удалось прочитать settings.json."
        );
    }

    public static ConfigurationManager GetInstance()
    {
        _instance ??= new ConfigurationManager();
        return _instance;
    }
}