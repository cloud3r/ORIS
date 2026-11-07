using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Controllers;

[Controller]
public class TestController : BaseController
{
    [Get]
    public IHttpResult Index()
    {
        return Html("<h1>Hello World!</h1>");
    }

    [Get]
    public IHttpResult Info()
    {
        return Json(new
        {
            Name = "Student",
            Course = "ORIS"
        });
    }

    [Get]
    public IHttpResult RequestInfo()
    {
        var request = Context.Request;

        return Json(new
        {
            Method = request.HttpMethod,
            Path = request.Url?.AbsolutePath,
            UserAgent = request.Headers["User-Agent"]
        });
    }
}