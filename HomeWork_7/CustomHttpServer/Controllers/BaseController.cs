using System.Net;
using CustomHttpServer.Framework.Results;
using MiniTemplateEngine;

namespace CustomHttpServer.Controllers;

public abstract class BaseController
{
    private static readonly IHtmlTemplateRenderer Renderer = new HtmlTemplateRenderer();

    public HttpListenerContext Context { get; internal set; } = null!;

    protected IHttpResult Html(string html, HttpStatusCode status = HttpStatusCode.OK) =>
        new HtmlResult(html, status);

    protected IHttpResult Json(object? data, HttpStatusCode status = HttpStatusCode.OK) =>
        new JsonResult(data, status);

    protected IHttpResult Text(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new TextResult(text, status);

    // Page("Profile", model)      -> Templates/<Controller>/Profile.html
    // Page("Shared/Error", model) -> Templates/Shared/Error.html
    protected IHttpResult Page(
        string template,
        object? model = null,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        string relative;

        if (template.Contains('/'))
        {
            relative = template + ".html";
        }
        else
        {
            string name = GetType().Name;
            string folder = name.EndsWith("Controller", StringComparison.Ordinal)
                ? name[..^"Controller".Length]
                : name;
            relative = Path.Combine(folder, template + ".html");
        }

        string path = Path.Combine(AppContext.BaseDirectory, "Templates", relative);

        return new PageResult(path, model, Renderer, status);
    }
}