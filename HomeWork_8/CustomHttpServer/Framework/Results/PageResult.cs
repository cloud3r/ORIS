using System.Net;
using MiniTemplateEngine;

namespace CustomHttpServer.Framework.Results;

public class PageResult : IHttpResult
{
    private readonly string _templatePath;
    private readonly object? _model;
    private readonly IHtmlTemplateRenderer _renderer;

    public HttpStatusCode StatusCode { get; }
    public string ContentType => "text/html";

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
    
    public string Render() => _renderer.RenderFromFile(_templatePath, _model);

    public Task ExecuteAsync(HttpListenerContext context)
    {
        string html = Render();
        return ResponseWriter.WriteAsync(context, StatusCode, html, ContentType);
    }
}