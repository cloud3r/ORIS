using MiniTemplateEngine;
using Xunit;

namespace CustomHttpServer.UnitTests;

public class RenderFileTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "tpl_tests_" + Guid.NewGuid().ToString("N"));

    private readonly HtmlTemplateRenderer _renderer = new();

    public RenderFileTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private string WriteTemplate(string content, string name = "t.html")
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void RenderFromFile_ExistingTemplate_ReturnsRenderedHtml()
    {
        string path = WriteTemplate("<p>${Name}</p>");

        var result = _renderer.RenderFromFile(path, new { Name = "Ann" });

        Assert.Equal("<p>Ann</p>", result);
    }

    [Fact]
    public void RenderFromFile_CyrillicContent_IsReadCorrectly()
    {
        string path = WriteTemplate("Привет, ${Name}!");

        var result = _renderer.RenderFromFile(path, new { Name = "Мир" });

        Assert.Equal("Привет, Мир!", result);
    }

    [Fact]
    public void RenderFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        string path = Path.Combine(_dir, "missing.html");

        Assert.Throws<FileNotFoundException>(() => _renderer.RenderFromFile(path, new { }));
    }

    [Fact]
    public void RenderToFile_WritesRenderedHtmlToOutput()
    {
        string template = WriteTemplate("<p>${Name}</p>");
        string output = Path.Combine(_dir, "out.html");

        _renderer.RenderToFile(template, output, new { Name = "Ann" });

        Assert.Equal("<p>Ann</p>", File.ReadAllText(output));
    }

    [Fact]
    public void RenderToFile_MissingOutputDirectory_CreatesIt()
    {
        string template = WriteTemplate("x");
        string output = Path.Combine(_dir, "nested", "deeper", "out.html");

        _renderer.RenderToFile(template, output, new { });

        Assert.True(File.Exists(output));
    }

    [Fact]
    public void RenderToFile_InvalidTemplate_ThrowsAndDoesNotCreateOutput()
    {
        string template = WriteTemplate("$if(A)\nno end\n");
        string output = Path.Combine(_dir, "out.html");

        Assert.Throws<TemplateException>(() => _renderer.RenderToFile(template, output, new { A = true }));
        Assert.False(File.Exists(output));
    }
}