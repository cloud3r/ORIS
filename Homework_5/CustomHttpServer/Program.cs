class Program
{
    static async Task Main(string[] args)
    {
        ConfigurationManager settings = ConfigurationManager.GetInstance();
        HttpServer server = new HttpServer(settings);
        Task serverTask = server.StartAsync();
        string? command = Console.ReadLine();
        
        if (command == "stop")
        {
            server.Stop();
            await serverTask;
        }
    }
}