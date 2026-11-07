using CustomHttpServer.Framework.Configuration;
using CustomHttpServer.Framework.Core;
using CustomHttpServer.Framework.Handlers;
using CustomHttpServer.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        HttpServer? server = null;

        try
        {
            ConfigurationManager settings =
                ConfigurationManager.GetInstance();
            
            IEmailService emailService = new EmailService();

            var staticFilesHandler = new StaticFilesHandler();
            var controllersHandler = new ControllersHandler(
                new object[] { emailService }
            );
            var notFoundHandler = new NotFoundHandler();
            
            staticFilesHandler.SetNext(controllersHandler);
            controllersHandler.SetNext(notFoundHandler);

            server = new HttpServer(
                settings,
                staticFilesHandler
            );

            Task serverTask = server.StartAsync();

            Console.WriteLine(
                "Введите 'stop' для остановки сервера."
            );

            while (!serverTask.IsCompleted)
            {
                Task<string?> commandTask =
                    Console.In.ReadLineAsync();

                Task completedTask = await Task.WhenAny(
                    serverTask,
                    commandTask
                );

                if (completedTask == serverTask)
                    break;

                string? command = await commandTask;

                if (command == null)
                {
                    server.Stop();
                    break;
                }

                if (command.Trim().Equals(
                        "stop",
                        StringComparison.OrdinalIgnoreCase))
                {
                    server.Stop();
                    break;
                }

                Console.WriteLine(
                    "Неизвестная команда. Введите 'stop'."
                );
            }

            await serverTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Ошибка приложения: {ex.Message}"
            );
        }
        finally
        {
            server?.Stop();
        }
    }
}