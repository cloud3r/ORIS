using CustomHttpServer.Framework.Configuation;
using CustomHttpServer.Framework.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        HttpServer? server = null;

        try
        {
            ConfigurationManager settings =
                ConfigurationManager.GetInstance();

            server = new HttpServer(settings);

            Task serverTask = server.StartAsync();

            Console.WriteLine("Введите 'stop' для остановки сервера.");

            while (!serverTask.IsCompleted)
            {
                Task<string?> commandTask = Console.In.ReadLineAsync();

                Task completedTask = await Task.WhenAny(
                    serverTask,
                    commandTask
                );

                if (completedTask == serverTask)
                    break;

                string? command = await commandTask;

                if (command == null)
                    break;

                if (command.Trim().Equals(
                        "stop",
                        StringComparison.OrdinalIgnoreCase))
                {
                    server.Stop();
                    break;
                }

                Console.WriteLine("Неизвестная команда. Введите 'stop'.");
            }

            await serverTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка приложения: {ex.Message}");
        }
        finally
        {
            server?.Stop();
        }
    }
}