using System.Net;
using CustomHttpServer.Framework.Results;
using CustomHttpServer.UnitTests;
using MiniTemplateEngine;
using Xunit;

namespace UnitTests;

public class ResultsTests
{

    [Fact]
    public void HtmlResult_ByDefault_Has200StatusAndHtmlContentType()
    {
        var result = new HtmlResult("<h1>Hi</h1>");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("text/html", result.ContentType);
        Assert.Equal("<h1>Hi</h1>", result.Body);
    }

    [Fact]
    public void HtmlResult_CustomStatus_IsPreserved()
    {
        var result = new HtmlResult("x", HttpStatusCode.NotFound);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public void HtmlResult_NullHtml_BecomesEmptyBody()
    {
        var result = new HtmlResult(null!);

        Assert.Equal("", result.Body);
    }
    

    [Fact]
    public void JsonResult_Object_IsSerializedWithCamelCaseNames()
    {
        var result = new JsonResult(new { Name = "Student", Course = "ORIS" });

        Assert.Equal("{\"name\":\"Student\",\"course\":\"ORIS\"}", result.Body);
    }

    [Fact]
    public void JsonResult_Cyrillic_IsNotEscaped()
    {
        var result = new JsonResult(new { Message = "Сообщение отправлено" });

        Assert.Contains("Сообщение отправлено", result.Body);
    }

    [Fact]
    public void JsonResult_ByDefault_Has200StatusAndJsonContentType()
    {
        var result = new JsonResult(new { A = 1 });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
    }

    [Fact]
    public void JsonResult_CustomStatus_IsPreserved()
    {
        var result = new JsonResult(new { success = false }, HttpStatusCode.BadRequest);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void JsonResult_NullData_SerializesToNull()
    {
        Assert.Equal("null", new JsonResult(null).Body);
    }
    

    [Fact]
    public void TextResult_Defaults_ArePlainTextWith200()
    {
        var result = new TextResult("ok");

        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("ok", result.Body);
    }

    [Fact]
    public void PageResult_Render_ReturnsRendererOutput()
    {
        var renderer = new FakeRenderer { Output = "<p>rendered</p>" };
        var page = new PageResult("Templates/User/Profile.html", null, renderer);

        var html = page.Render();

        Assert.Equal("<p>rendered</p>", html);
    }

    [Fact]
    public void PageResult_Render_PassesPathAndModelToRenderer()
    {
        var renderer = new FakeRenderer();
        var model = new { Name = "Ann" };
        var page = new PageResult("some/path.html", model, renderer);

        page.Render();

        Assert.Equal("some/path.html", renderer.LastPath);
        Assert.Same(model, renderer.LastModel);
    }

    [Fact]
    public void PageResult_DefaultStatusIs200_AndCustomStatusIsKept()
    {
        var renderer = new FakeRenderer();

        var ok = new PageResult("p", null, renderer);
        var notFound = new PageResult("p", null, renderer, HttpStatusCode.NotFound);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public void PageResult_RendererThrows_ExceptionPropagates()
    {
        var renderer = new FakeRenderer { ToThrow = new TemplateException("bad template") };
        var page = new PageResult("p", null, renderer);

        var ex = Assert.Throws<TemplateException>(() => page.Render());

        Assert.Equal("bad template", ex.Message);
    }

    [Fact]
    public void PageResult_ImplementsIHttpResult()
    {
        Assert.IsAssignableFrom<IHttpResult>(new PageResult("p", null, new FakeRenderer()));
    }

    [Fact]
    public void BaseController_Html_ReturnsHtmlResult()
    {
        var result = new HelperController().CallHtml("<b>x</b>");

        var html = Assert.IsType<HtmlResult>(result);
        Assert.Equal("<b>x</b>", html.Body);
    }

    [Fact]
    public void BaseController_Json_ReturnsJsonResultWithStatus()
    {
        var result = new HelperController().CallJson(new { A = 1 }, HttpStatusCode.Created);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(HttpStatusCode.Created, json.StatusCode);
    }

    [Fact]
    public void BaseController_Text_ReturnsTextResult()
    {
        Assert.IsType<TextResult>(new HelperController().CallText("t"));
    }

    [Fact]
    public void BaseController_Page_ReturnsPageResult()
    {
        Assert.IsType<PageResult>(new HelperController().CallPage());
    }
}