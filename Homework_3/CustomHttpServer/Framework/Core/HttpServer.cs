using System.Net;
using CustomHttpServer.Helpers;

public class HttpServer
{
    private readonly ConfigurationManager _settings;
    private readonly HttpListener _listener;

    public HttpServer(ConfigurationManager settings)
    {
        _settings = settings;
        _listener = new HttpListener();
        string prefixes = $"http://{_settings.Server.Host}:{_settings.Server.Port}{_settings.Server.Path}";
        _listener.Prefixes.Add(prefixes);
    }

    public Task StartAsync()
    {
        _listener.Start();
        return ListenAsync();
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private async Task ListenAsync()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                await HandleRequest(context);
            }
        }
        catch (HttpListenerException)
        {
            Console.WriteLine("Сервер остановлен");
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        Console.WriteLine($"REQUEST: {context.Request.HttpMethod} {context.Request.Url}");
        HttpListenerResponse response = context.Response;
        HttpListenerRequest request = context.Request;
        string pathBaseDirectory = AppContext.BaseDirectory;
        string localPath = request.Url.LocalPath;
        string pathNotFound = Path.Combine(pathBaseDirectory, "static", "404.html");
        string pathToStatic = Path.Combine(pathBaseDirectory, "static");
        string? path = UriHelper.ResolveSafePath(
            localPath,
            pathToStatic
        );

        if (path == null)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            byte[] bufferNotFound = await File.ReadAllBytesAsync(pathNotFound);
            response.ContentLength64 = bufferNotFound.Length;
            response.ContentType = MimeHandler.GetMimeType(pathNotFound);
            await response.OutputStream.WriteAsync(bufferNotFound, 0, bufferNotFound.Length);
            response.OutputStream.Close();
            return;
        }
        
        if (Directory.Exists(path))
        {
            path = Path.Combine(path, "index.html");
        }
        
        if (!File.Exists(path))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            byte[] bufferNotFound = await File.ReadAllBytesAsync(pathNotFound);
            response.ContentLength64 = bufferNotFound.Length;
            response.ContentType = MimeHandler.GetMimeType(pathNotFound);
            await response.OutputStream.WriteAsync(bufferNotFound, 0, bufferNotFound.Length);
            response.OutputStream.Close();
            return;
        }
        byte[]  buffer = await File.ReadAllBytesAsync(path);
        response.ContentLength64 = buffer.Length;
        response.ContentType = MimeHandler.GetMimeType(path);
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
        
    }
}