using MiniTemplateEngine;
using Xunit;

namespace CustomHttpServer.UnitTests;

public class RenderFromStringTests
{
    private readonly HtmlTemplateRenderer _renderer = new();

    private string Render(string template, object? model = null) =>
        _renderer.RenderFromString(template, model);
    
    private static string[] Lines(string s) =>
        s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string T(params string[] lines) => string.Join("\n", lines) + "\n";

    [Fact]
    public void Render_EmptyTemplate_ReturnsEmptyString()
    {
        var result = Render("", new { });

        Assert.Equal("", result);
    }

    [Fact]
    public void Render_TemplateWithoutVariables_ReturnsTextUnchanged()
    {
        var result = Render("<p>Hello</p>", new { });

        Assert.Equal("<p>Hello</p>", result);
    }

    [Fact]
    public void Render_SingleProperty_SubstitutesValue()
    {
        var result = Render("<p>${Name}</p>", new { Name = "Ann" });

        Assert.Equal("<p>Ann</p>", result);
    }

    [Fact]
    public void Render_MultipleProperties_SubstitutesAll()
    {
        var result = Render("${Name} ${Age}", new { Name = "Ann", Age = 20 });

        Assert.Equal("Ann 20", result);
    }

    [Fact]
    public void Render_SamePropertyTwice_SubstitutesBothOccurrences()
    {
        var result = Render("${Name}-${Name}", new { Name = "Ann" });

        Assert.Equal("Ann-Ann", result);
    }

    [Fact]
    public void Render_NestedProperties_ResolvesChain()
    {
        var model = new { User = new { Name = "Ann", Email = "ann@test.com" } };

        var result = Render("<p>${User.Name}</p><p>${User.Email}</p>", model);

        Assert.Equal("<p>Ann</p><p>ann@test.com</p>", result);
    }

    [Fact]
    public void Render_HtmlInValue_IsEncoded()
    {
        var result = Render("${Name}", new { Name = "<script>alert(1)</script>" });

        Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", result);
    }

    [Fact]
    public void Render_DictionaryModel_ResolvesKeys()
    {
        var model = new Dictionary<string, object> { ["Name"] = "Ann" };

        var result = Render("${Name}", model);

        Assert.Equal("Ann", result);
    }

    [Fact]
    public void Render_IfTrue_RendersBody()
    {
        var result = Render(T("$if(IsActive)", "YES", "$endif"), new { IsActive = true });

        Assert.Equal(new[] { "YES" }, Lines(result));
    }

    [Fact]
    public void Render_IfFalse_RendersNothing()
    {
        var result = Render(T("$if(IsActive)", "YES", "$endif"), new { IsActive = false });

        Assert.Empty(Lines(result));
    }

    [Theory]
    [InlineData(true, "ACTIVE")]
    [InlineData(false, "INACTIVE")]
    public void Render_IfElse_RendersMatchingBranch(bool isActive, string expected)
    {
        var template = T("$if(IsActive)", "ACTIVE", "$else", "INACTIVE", "$endif");

        var result = Render(template, new { IsActive = isActive });

        Assert.Equal(new[] { expected }, Lines(result));
    }

    [Theory]
    [InlineData(false, "YES")]
    [InlineData(true, "NO")]
    public void Render_IfWithNegation_InvertsCondition(bool flag, string expected)
    {
        var template = T("$if(!Flag)", "YES", "$else", "NO", "$endif");

        var result = Render(template, new { Flag = flag });

        Assert.Equal(new[] { expected }, Lines(result));
    }

    [Theory]
    [InlineData(true, true, "AB")]
    [InlineData(true, false, "A-notB")]
    [InlineData(false, true, "notA")]
    [InlineData(false, false, "notA")]
    public void Render_NestedIf_RendersCorrectBranch(bool a, bool b, string expected)
    {
        var template = T(
            "$if(A)",
            "$if(B)", "AB", "$else", "A-notB", "$endif",
            "$else",
            "notA",
            "$endif");

        var result = Render(template, new { A = a, B = b });

        Assert.Equal(new[] { expected }, Lines(result));
    }

    [Fact]
    public void Render_ForeachWithItems_RepeatsBodyForEachItem()
    {
        var model = new { Items = new[] { new { Name = "A" }, new { Name = "B" }, new { Name = "C" } } };
        var template = T("$foreach(var item in Items)", "<li>${item.Name}</li>", "$endfor");

        var result = Render(template, model);

        Assert.Equal(new[] { "<li>A</li>", "<li>B</li>", "<li>C</li>" }, Lines(result));
    }

    [Fact]
    public void Render_ForeachWithEmptyCollection_RendersNothingInside()
    {
        var template = T("<ul>", "$foreach(var item in Items)", "<li>${item}</li>", "$endfor", "</ul>");

        var result = Render(template, new { Items = Array.Empty<string>() });

        Assert.Equal(new[] { "<ul>", "</ul>" }, Lines(result));
    }

    [Fact]
    public void Render_ForeachOverStrings_ExposesItemItself()
    {
        var template = T("$foreach(var s in Names)", "[${s}]", "$endfor");

        var result = Render(template, new { Names = new[] { "x", "y" } });

        Assert.Equal(new[] { "[x]", "[y]" }, Lines(result));
    }

    [Fact]
    public void Render_NestedForeach_RendersAllCombinations()
    {
        var model = new
        {
            Groups = new[]
            {
                new { Name = "A", Items = new[] { 1, 2 } },
                new { Name = "B", Items = new[] { 3 } }
            }
        };
        var template = T(
            "$foreach(var g in Groups)",
            "$foreach(var i in g.Items)",
            "${g.Name}:${i}",
            "$endfor",
            "$endfor");

        var result = Render(template, model);

        Assert.Equal(new[] { "A:1", "A:2", "B:3" }, Lines(result));
    }

    [Fact]
    public void Render_IfInsideForeach_ChoosesBranchPerItem()
    {
        var model = new
        {
            Users = new[]
            {
                new { Name = "Ann", IsActive = true },
                new { Name = "Bob", IsActive = false }
            }
        };
        var template = T(
            "$foreach(var user in Users)",
            "$if(user.IsActive)", "${user.Name} on", "$else", "${user.Name} off", "$endif",
            "$endfor");

        var result = Render(template, model);

        Assert.Equal(new[] { "Ann on", "Bob off" }, Lines(result));
    }

    [Fact]
    public void Render_ForeachInsideIf_RendersOnlyWhenConditionTrue()
    {
        var template = T("$if(Show)", "$foreach(var i in Items)", "${i}", "$endfor", "$endif");

        var shown = Render(template, new { Show = true, Items = new[] { 1, 2 } });
        var hidden = Render(template, new { Show = false, Items = new[] { 1, 2 } });

        Assert.Equal(new[] { "1", "2" }, Lines(shown));
        Assert.Empty(Lines(hidden));
    }

    [Theory]
    [InlineData("$if(IsActive)\ntext\n", "$endif")]
    [InlineData("$foreach(var x in Items)\ntext\n", "$endfor")]
    [InlineData("$endif\n", "неожиданная")]
    [InlineData("$endfor\n", "неожиданная")]
    [InlineData("$else\n", "неожиданная")]
    [InlineData("$foreach(x of Items)\n$endfor\n", "некорректная конструкция $foreach")]
    [InlineData("$if\ntext\n$endif\n", "скобки")]
    [InlineData("$foreach(var x in Items)\n$if(IsActive)\n$endfor\n$endif\n", "неожиданная")]
    public void Render_InvalidSyntax_ThrowsTemplateExceptionWithReason(string template, string expectedPart)
    {
        var model = new { IsActive = true, Items = new[] { 1 } };

        var ex = Assert.Throws<TemplateException>(() => Render(template, model));

        Assert.Contains(expectedPart, ex.Message);
    }

    [Fact]
    public void Render_UnknownProperty_ThrowsTemplateExceptionWithPropertyName()
    {
        var ex = Assert.Throws<TemplateException>(() => Render("${Nope}", new { A = 1 }));

        Assert.Contains("Nope", ex.Message);
    }

    [Fact]
    public void Render_NullModelWithVariable_ThrowsTemplateException()
    {
        Assert.Throws<TemplateException>(() => Render("${Name}", null));
    }

    [Fact]
    public void Render_IfWithNonBooleanValue_ThrowsTemplateException()
    {
        var ex = Assert.Throws<TemplateException>(
            () => Render(T("$if(Name)", "x", "$endif"), new { Name = "Ann" }));

        Assert.Contains("логическим", ex.Message);
    }

    [Fact]
    public void Render_ForeachOverNonCollection_ThrowsTemplateException()
    {
        var ex = Assert.Throws<TemplateException>(
            () => Render(T("$foreach(var x in Name)", "x", "$endfor"), new { Name = "Ann" }));

        Assert.Contains("не является коллекцией", ex.Message);
    }

    [Fact]
    public void Render_ErrorMessage_ContainsLineNumber()
    {
        var template = "line1\nline2\n${Missing}";

        var ex = Assert.Throws<TemplateException>(() => Render(template, new { A = 1 }));

        Assert.Contains("Строка 3", ex.Message);
    }

    [Fact]
    public void Render_NullTemplate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.RenderFromString(null!, new { }));
    }
}