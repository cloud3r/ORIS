using MiniTemplateEngine;

namespace CardSite.UnitTests;

public class TemplateEngineTests
{
    private TemplateEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = new TemplateEngine();
    }

    [Test]
    public void RenderFromString_ReplacesVariable()
    {
        const string template = "<h1>{{Name}}</h1>";
        var data = new { Name = "Son Goten" };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.EqualTo("<h1>Son Goten</h1>"));
    }

    [Test]
    public void RenderFromString_ReplacesNestedProperty()
    {
        const string template = "{{Card.Name}}";
        var data = new
        {
            Card = new
            {
                Name = "Krillin"
            }
        };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.EqualTo("Krillin"));
    }

    [Test]
    public void RenderFromString_EncodesHtmlCharacters()
    {
        const string template = "{{Name}}";
        var data = new { Name = "<script>alert('test')</script>" };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Does.Not.Contain("<script>"));
        Assert.That(result, Does.Contain("&lt;script&gt;"));
    }

    [Test]
    public void RenderFromString_UnknownPropertyReturnsEmptyString()
    {
        const string template = "{{UnknownProperty}}";
        var data = new { Name = "Goku" };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RenderFromString_RendersForeach()
    {
        const string template =
            "{{foreach Cards}}<p>{{Name}}</p>{{/foreach}}";

        var data = new
        {
            Cards = new[]
            {
                new { Name = "Son Goten" },
                new { Name = "Krillin" },
                new { Name = "Son Gohan" }
            }
        };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.EqualTo(
            "<p>Son Goten</p><p>Krillin</p><p>Son Gohan</p>"));
    }

    [Test]
    public void RenderFromString_RendersConditionWhenTrue()
    {
        const string template =
            "{{if IsVisible}}<p>Visible</p>{{/if}}";

        var data = new { IsVisible = true };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.EqualTo("<p>Visible</p>"));
    }

    [Test]
    public void RenderFromString_DoesNotRenderConditionWhenFalse()
    {
        const string template =
            "{{if IsVisible}}<p>Visible</p>{{/if}}";

        var data = new { IsVisible = false };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RenderFromString_EmptyTemplateReturnsEmptyString()
    {
        const string template = "";

        var data = new { Name = "Goku" };

        string result = _engine.RenderFromString(template, data);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RenderFromString_NullDataThrowsArgumentNullException()
    {
        const string template = "{{Name}}";

        void Action() => _engine.RenderFromString(template, null!);

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Test]
    public void RenderFromFile_RendersTemplateFromFile()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.html");

        File.WriteAllText(filePath, "<h1>{{Name}}</h1>");

        try
        {
            var data = new { Name = "Goku" };

            string result = _engine.RenderFromFile(filePath, data);

            Assert.That(result, Is.EqualTo("<h1>Goku</h1>"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Test]
    public void RenderFromFile_MissingFileThrowsFileNotFoundException()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.html");

        var data = new { Name = "Goku" };

        void Action() => _engine.RenderFromFile(filePath, data);

        Assert.Throws<FileNotFoundException>(Action);
    }
}