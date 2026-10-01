using System.Net;
using System.Text;

public class HttpServer
{
    private readonly HttpListener _listener;
    private bool _isRunning;

    public HttpServer(string address)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(address);
    }

    public async Task Start()
    {
        _listener.Start();
        _isRunning = true;

        Console.WriteLine("Сервер запущен.");
        Console.WriteLine("Адрес: " + _listener.Prefixes.First());

        while (_isRunning)
        {
            try
            {
                HttpListenerContext context = await _listener.GetContextAsync();

                await ProcessRequest(context);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "search-engine.html"
        );

        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = 404;

            byte[] errorBytes = Encoding.UTF8.GetBytes(
                "Файл search-engine.html не найден."
            );

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = errorBytes.Length;

            await context.Response.OutputStream.WriteAsync(errorBytes);
            context.Response.Close();

            return;
        }

        string html = await File.ReadAllTextAsync(filePath);

        byte[] buffer = Encoding.UTF8.GetBytes(html);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);

        context.Response.Close();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _listener.Stop();
        _listener.Close();

        Console.WriteLine("Сервер остановлен.");
    }
}