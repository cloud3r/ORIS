using System.Net;

namespace CustomHttpServer.Framework.Results;

public class TextResult : IHttpResult
{
    private readonly string _text;
    public HttpStatusCode StatusCode { get; }

    public TextResult(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _text = text ?? string.Empty;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context) =>
        ResponseWriter.WriteAsync(context, StatusCode, _text, "text/plain");
}