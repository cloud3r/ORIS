namespace MyHttpServer.Configuration;

public class ServerSettings
{
    public Server Server { get; set; } = new();
}

public class Server
{
    public string Port { get; set; } = "";
    public string Host { get; set; } = "";
    public string Path { get; set; } = "";
    
}