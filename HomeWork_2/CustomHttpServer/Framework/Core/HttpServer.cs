using System.Net;

public class HttpServer
{
    private readonly ConfigurationManager _settings;
    private readonly HttpListener _listener;
    private readonly string _indexPath;

    public HttpServer(ConfigurationManager settings)
    {
        _settings = settings;
        _listener = new HttpListener();

        _indexPath = Path.Combine(
            AppContext.BaseDirectory,
            "search-engine.html"
        );

        if (!File.Exists(_indexPath))
            throw new FileNotFoundException("Файл search-engine.html не найден!");

        string prefix =
            $"http://{_settings.Server.Host}:{_settings.Server.Port}/{_settings.Server.Path}/";

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
        HttpListenerResponse response = context.Response;

        Console.WriteLine(
            $"Получен запрос: {context.Request.HttpMethod} {context.Request.Url}"
        );

        try
        {
            byte[] buffer = await File.ReadAllBytesAsync(_indexPath);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);

            Console.WriteLine("HTTP-запрос успешно обработан.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");

            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            response.Close();
        }
    }
}