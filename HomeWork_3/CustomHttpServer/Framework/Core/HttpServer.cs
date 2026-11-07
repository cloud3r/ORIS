using System.Net;
using CustomHttpServer.Content;
using CustomHttpServer.Framework.Configuation;
using CustomHttpServer.Framework.Helpers;

namespace CustomHttpServer.Framework.Core;

public class HttpServer
{
    private readonly ConfigurationManager _settings;
    private readonly HttpListener _listener;
    private readonly string _staticDirectory;

    public HttpServer(ConfigurationManager settings)
    {
        _settings = settings;
        _listener = new HttpListener();

        _staticDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "static"
        );

        if (!Directory.Exists(_staticDirectory))
            throw new DirectoryNotFoundException(
                "Каталог static не найден!"
            );

        string prefix =
            $"http://{_settings.Server.Host}:{_settings.Server.Port}/";

        Console.WriteLine($"Адрес сервера: {prefix}");

        _listener.Prefixes.Add(prefix);
    }

    public Task StartAsync()
    {
        _listener.Start();

        Console.WriteLine("Сервер успешно запущен!");

        return ListenAsync();
    }

    public void Stop()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
            Console.WriteLine("Сервер остановлен.");
        }
    }

    private async Task ListenAsync()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context =
                    await _listener.GetContextAsync();

                await HandleRequest(context);
            }
        }
        catch (HttpListenerException)
        {
            if (!_listener.IsListening)
                Console.WriteLine("Прослушивание сервера завершено.");
            else
                throw;
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Сервер завершил работу.");
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        Console.WriteLine(
            $"Получен запрос: {request.HttpMethod} {request.Url}"
        );

        try
        {
            string requestedPath = Uri.UnescapeDataString(
                request.Url!.AbsolutePath
            );

            string relativePath = requestedPath.TrimStart('/');
        
            if (relativePath.Equals(
                    "404.html",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendNotFound(response);
                return;
            }
        
            if (string.IsNullOrEmpty(relativePath))
            {
                relativePath = "SearcherFinder/search-engine.html";
            }
        
            string? filePath = UriHelper.ResolveSafePath(
                relativePath,
                _staticDirectory
            );

            if (filePath == null)
            {
                await SendNotFound(response);
                return;
            }
        
            if (Directory.Exists(filePath))
            {
                filePath = Path.Combine(filePath, "index.html");
            }
        
            if (!File.Exists(filePath))
            {
                await SendNotFound(response);
                return;
            }
        
            byte[] buffer = await File.ReadAllBytesAsync(filePath);
        
            response.ContentType = MimeTypeProvider.GetMimeType(filePath);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);

            Console.WriteLine($"Файл отправлен: {filePath}");
            Console.WriteLine("HTTP 200 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");

            if (response.OutputStream.CanWrite)
            {
                response.StatusCode =
                    (int)HttpStatusCode.InternalServerError;
            }
        }
        finally
        {
            response.Close();
        }
    }

    private async Task SendNotFound(HttpListenerResponse response)
    {
        string notFoundPath = Path.Combine(
            _staticDirectory,
            "404.html"
        );

        response.StatusCode = (int)HttpStatusCode.NotFound;

        if (File.Exists(notFoundPath))
        {
            byte[] buffer = await File.ReadAllBytesAsync(notFoundPath);

            response.ContentType = MimeTypeProvider.GetMimeType(notFoundPath);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
        }
        else
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(
                "404 - Page Not Found"
            );

            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
        }

        Console.WriteLine("404 Not Found: запрашиваемый файл отсутствует.");
    }
}