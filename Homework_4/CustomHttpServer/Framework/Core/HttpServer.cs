using System.Net;
using CustomHttpServer.Framework.Handlers;
using CustomHttpServer.Helpers;

public class HttpServer
{
    private readonly ConfigurationManager _settings;
    private readonly HttpListener _listener;
    private readonly Handler _chain;

    public HttpServer(ConfigurationManager settings)
    {
        _settings = settings;
        _listener = new HttpListener();

        string path = _settings.Server.Path;
        if (!path.EndsWith("/")) path += "/";
        _listener.Prefixes.Add($"http://{_settings.Server.Host}:{_settings.Server.Port}{path}");

        Handler staticFiles = new StaticFilesHandler();
        Handler controllers = new ControllerHandler();
        staticFiles.Successor = controllers;
        _chain = staticFiles;
    }

    public Task StartAsync()
    {
        _listener.Start();
        return ListenAsync();
    }

    public void Stop() => _listener.Stop();

    private async Task ListenAsync()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context));
            }
        }
        catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
        {
            Console.WriteLine("Сервер остановлен");
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            await _chain.HandleRequest(context);
            Console.WriteLine($"Обработан запрос: {context.Request.Url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            try
            {
                context.Response.StatusCode = 500;
            }
            catch {  }
        }
        finally
        {
            try { context.Response.Close(); } catch { }
        }
    }
}