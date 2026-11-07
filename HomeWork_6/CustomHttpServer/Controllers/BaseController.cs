using System.Net;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Controllers;

public abstract class BaseController
{
    public HttpListenerContext Context { get; internal set; } = null!;

    protected IHttpResult Html(string html, HttpStatusCode status = HttpStatusCode.OK) =>
        new HtmlResult(html, status);

    protected IHttpResult Json(object? data, HttpStatusCode status = HttpStatusCode.OK) =>
        new JsonResult(data, status);

    protected IHttpResult Text(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new TextResult(text, status);
}