using System.Net;
using MiniTemplateEngine;

namespace CustomHttpServer.Framework.Results;

public class PageResult : IHttpResult
{
    private readonly string _templatePath;
    private readonly object? _model;
    private readonly IHtmlTemplateRenderer _renderer;

    public HttpStatusCode StatusCode { get; }

    public PageResult(
        string templatePath,
        object? model,
        IHtmlTemplateRenderer renderer,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _templatePath = templatePath;
        _model = model;
        _renderer = renderer;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context)
    {
        // Сначала рендер целиком: если шаблон с ошибкой, исключение уйдёт в
        // ControllersHandler ДО записи ответа, и клиент получит чистый 500
        string html = _renderer.RenderFromFile(_templatePath, _model);

        return ResponseWriter.WriteAsync(context, StatusCode, html, "text/html");
    }
}