using System.Text.Json;
using MyHttpServer.Configuration;

public class Program
{
    public static async Task Main()
    {
        string json = await File.ReadAllTextAsync("settings.json");

        ServerSettings? settings = JsonSerializer.Deserialize<ServerSettings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (settings == null)
        {
            Console.WriteLine("Не удалось загрузить настройки.");
            return;
        }

        string address =
            $"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}";

        HttpServer server = new HttpServer(address);

        Task serverTask = server.Start();

        Console.WriteLine("Введите 'stop' для остановки сервера.");

        while (true)
        {
            string? command = Console.ReadLine();

            if (command?.ToLower() == "stop")
            {
                server.Stop();
                break;
            }
        }
        await serverTask;
    }
}