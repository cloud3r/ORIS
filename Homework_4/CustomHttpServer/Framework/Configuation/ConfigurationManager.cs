using System.Text.Json;
using CustomHttpServer.Framework.Configuation;

public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;
    public ServerSettings Server { get; }

    private ConfigurationManager()
    {
        string pathToSettingsJson = Path.Combine(AppContext.BaseDirectory, "settings.json");
        string settingsJson = File.ReadAllText(pathToSettingsJson);
        ServerSettings serverSettingsJson = JsonSerializer.Deserialize<ServerSettings>(settingsJson)!;
        Server = serverSettingsJson;
    }
    
    public static ConfigurationManager GetInstance()
    {
        _instance ??= new ConfigurationManager();
        return _instance;
    }
}