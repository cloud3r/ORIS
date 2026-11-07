using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CustomHttpServer.Framework.Results;

public class JsonResult : IHttpResult
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly object? _data;
    public HttpStatusCode StatusCode { get; }

    public JsonResult(object? data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _data = data;
        StatusCode = statusCode;
    }

    public Task ExecuteAsync(HttpListenerContext context) =>
        ResponseWriter.WriteAsync(
            context, StatusCode,
            JsonSerializer.Serialize(_data, Options),
            "application/json");
}