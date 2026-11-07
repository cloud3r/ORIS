using System.Net;
using CustomHttpServer.Controllers;
using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Framework.Handlers;
using CustomHttpServer.Framework.Results;
using MiniTemplateEngine;

namespace CustomHttpServer.UnitTests;

[Controller]
public class SampleController : BaseController
{
    [Get] public IHttpResult Index() => Html("x");
    [Get("named")] public IHttpResult Custom() => Html("x");
    [Post("save")] public IHttpResult Save(string name) => Html(name);
    [Get] [Post("both")] public IHttpResult Both() => Html("x");
    public IHttpResult NoAttribute() => Html("x");
}

[Controller("/custom/")]
public class RenamedController : BaseController { }

[Controller("hidden")]
public abstract class AbstractHiddenController : BaseController { }

[Controller("orphan")]
public class OrphanController { }

public class NoAttributeController : BaseController { }

public class HelperController : BaseController
{
    public IHttpResult CallHtml(string html) => Html(html);
    public IHttpResult CallJson(object o, HttpStatusCode s = HttpStatusCode.OK) => Json(o, s);
    public IHttpResult CallText(string t) => Text(t);
    public IHttpResult CallPage() => Page("Profile", new { A = 1 });
}

public class FakeRenderer : IHtmlTemplateRenderer
{
    public string? LastPath { get; private set; }
    public object? LastModel { get; private set; }
    public string Output { get; set; } = "<html>fake</html>";
    public Exception? ToThrow { get; set; }

    public string RenderFromString(string template, object? model) => Output;

    public string RenderFromFile(string templatePath, object? model)
    {
        if (ToThrow != null) throw ToThrow;
        LastPath = templatePath;
        LastModel = model;
        return Output;
    }

    public void RenderToFile(string templatePath, string outputPath, object? model) =>
        throw new NotSupportedException();
}

public class RecordingHandler : RequestHandler
{
    private readonly string _name;
    private readonly List<string> _log;
    private readonly bool _callNext;

    public RecordingHandler(string name, List<string> log, bool callNext = true)
    {
        _name = name;
        _log = log;
        _callNext = callNext;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        _log.Add(_name);
        if (_callNext)
            await HandleNextAsync(context);
    }
}