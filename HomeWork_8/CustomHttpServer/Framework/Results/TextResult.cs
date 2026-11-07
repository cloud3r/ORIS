using System.Net;

namespace CustomHttpServer.Framework.Results;

public class TextResult : IHttpResult
{
    public string Body { get; }
    public string ContentType => "text/plain";
    public HttpStatusCode StatusCode { get; }

    public TextResult(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Body = text ?? string.Empty;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context) =>
        ResponseWriter.WriteAsync(context, StatusCode, Body, ContentType);
}