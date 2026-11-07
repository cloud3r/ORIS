using System.Net;
using CustomHttpServer.Framework.Configuation;
using CustomHttpServer.Framework.Handlers;

namespace CustomHttpServer.Framework.Core;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly ConfigurationManager _settings;
    private readonly RequestHandler _handler;

    public HttpServer(
        ConfigurationManager settings,
        RequestHandler handler)
    {
        _settings = settings;
        _handler = handler;

        _listener = new HttpListener();

        string prefix =
            $"http://{_settings.Server.Host}:{_settings.Server.Port}/";

        _listener.Prefixes.Add(prefix);

        Console.WriteLine($"Адрес сервера: {prefix}");
    }
    
    public Task StartAsync()
    {
        _listener.Start();

        Console.WriteLine("HTTP-сервер успешно запущен!");

        return ListenAsync();
    }
    
    private async Task ListenAsync()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context =
                    await _listener.GetContextAsync();
                
                _ = Task.Run(() => HandleRequestAsync(context));
            }
        }
        catch (HttpListenerException)
        {
            if (!_listener.IsListening)
            {
                Console.WriteLine("Прослушивание сервера завершено.");
            }
            else
            {
                throw;
            }
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Сервер завершил работу.");
        }
    }
    
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            Console.WriteLine(
                $"Получен запрос: {context.Request.HttpMethod} " +
                $"{context.Request.Url}"
            );

            await _handler.HandleAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Ошибка обработки запроса: {ex.Message}"
            );

            context.Response.Abort();
        }
    }
    
    public void Stop()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
            Console.WriteLine("HTTP-сервер остановлен.");
        }
    }
}