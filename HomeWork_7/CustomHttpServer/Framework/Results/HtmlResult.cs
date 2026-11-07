using System.Net;

namespace CustomHttpServer.Framework.Results;

public class HtmlResult : IHttpResult
{
    private readonly string _html;
    public HttpStatusCode StatusCode { get; }

    public HtmlResult(string html, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _html = html ?? string.Empty;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context) =>
        ResponseWriter.WriteAsync(context, StatusCode, _html, "text/html");
}