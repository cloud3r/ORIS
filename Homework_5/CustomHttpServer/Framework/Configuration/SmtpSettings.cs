using Microsoft.Extensions.Configuration;

namespace CustomHttpServer.Helpers;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";

    public static SmtpSettings Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("secret.json", optional: false)
            .Build();

        var settings = new SmtpSettings();
        config.GetSection("Smtp").Bind(settings);
        return settings;
    }
}