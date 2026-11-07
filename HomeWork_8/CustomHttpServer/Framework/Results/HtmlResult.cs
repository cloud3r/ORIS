using System.Net;

namespace CustomHttpServer.Framework.Results;

public class HtmlResult : IHttpResult
{
    public string Body { get; }
    public string ContentType => "text/html";
    public HttpStatusCode StatusCode { get; }

    public HtmlResult(string html, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Body = html ?? string.Empty;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context) =>
        ResponseWriter.WriteAsync(context, StatusCode, Body, ContentType);
}