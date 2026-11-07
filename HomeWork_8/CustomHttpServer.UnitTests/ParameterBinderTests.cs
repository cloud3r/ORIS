using System.Collections.Specialized;
using System.Reflection;
using CustomHttpServer.Routing;
using Xunit;

namespace CustomHttpServer.UnitTests;

public class ParameterBinderTests
{
    private enum Color { Red, Green }

    private class Target
    {
        public void Action(string name, int age, bool check = false) { }
        public void Optional(string title = "default") { }
    }

    private static MethodInfo Method(string name) => typeof(Target).GetMethod(name)!;

    private static Dictionary<string, string> Values(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Merge_QueryOnly_ReturnsQueryValues()
    {
        var query = new NameValueCollection { ["a"] = "1", ["b"] = "2" };

        var result = ParameterBinder.Merge(query, null);

        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
    }

    [Fact]
    public void Merge_FormBody_IsParsedAndUrlDecoded()
    {
        var result = ParameterBinder.Merge(new NameValueCollection(), "name=Ann+Lee&email=a%40b.c");

        Assert.Equal("Ann Lee", result["name"]);
        Assert.Equal("a@b.c", result["email"]);
    }

    [Fact]
    public void Merge_BodyAndQuery_BodyOverridesSameKey()
    {
        var query = new NameValueCollection { ["name"] = "fromQuery", ["q"] = "1" };

        var result = ParameterBinder.Merge(query, "name=fromBody");

        Assert.Equal("fromBody", result["name"]);
        Assert.Equal("1", result["q"]);
    }

    [Fact]
    public void Merge_KeysAreCaseInsensitive()
    {
        var result = ParameterBinder.Merge(new NameValueCollection { ["Name"] = "x" }, null);

        Assert.Equal("x", result["name"]);
    }

    [Fact]
    public void Merge_EmptyBody_ReturnsOnlyQuery()
    {
        var result = ParameterBinder.Merge(new NameValueCollection { ["a"] = "1" }, "");

        Assert.Single(result);
    }

    [Fact]
    public void Bind_AllParametersProvided_ConvertsTypes()
    {
        var values = Values(("name", "Ann"), ("age", "20"), ("check", "true"));

        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), values);

        Assert.True(result.Success);
        Assert.Equal(new object?[] { "Ann", 20, true }, result.Arguments);
    }

    [Fact]
    public void Bind_MissingOptionalParameter_UsesDefault()
    {
        var values = Values(("name", "Ann"), ("age", "20"));

        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), values);

        Assert.Equal(false, result.Arguments![2]);
    }

    [Fact]
    public void Bind_NoParameters_UsesDefaultValue()
    {
        var result = ParameterBinder.Bind(Method(nameof(Target.Optional)), Values());

        Assert.Equal("default", result.Arguments![0]);
    }

    [Fact]
    public void Bind_MissingRequiredParameter_ReturnsErrorWithName()
    {
        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), Values(("name", "Ann")));

        Assert.False(result.Success);
        Assert.Equal("Missing parameter: age", result.Error);
    }

    [Fact]
    public void Bind_InvalidNumber_ReturnsInvalidParameterError()
    {
        var values = Values(("name", "Ann"), ("age", "abc"));

        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), values);

        Assert.False(result.Success);
        Assert.Equal("Invalid parameter: age", result.Error);
    }

    [Fact]
    public void Bind_NumberOutOfRange_ReturnsInvalidParameterError()
    {
        var values = Values(("name", "Ann"), ("age", "99999999999"));

        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), values);

        Assert.Equal("Invalid parameter: age", result.Error);
    }

    [Fact]
    public void Bind_FormCheckboxOn_IsTrue()
    {
        var values = Values(("name", "Ann"), ("age", "1"), ("check", "on"));

        var result = ParameterBinder.Bind(Method(nameof(Target.Action)), values);

        Assert.Equal(true, result.Arguments![2]);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("on", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("off", false)]
    [InlineData("", false)]
    public void ConvertValue_Bool_RecognizesFormValues(string input, bool expected)
    {
        Assert.Equal(expected, ParameterBinder.ConvertValue(input, typeof(bool)));
    }

    [Fact]
    public void ConvertValue_UnknownBool_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ParameterBinder.ConvertValue("maybe", typeof(bool)));
    }

    [Fact]
    public void ConvertValue_Double_UsesInvariantCulture()
    {
        Assert.Equal(1.5, ParameterBinder.ConvertValue("1.5", typeof(double)));
    }

    [Fact]
    public void ConvertValue_NullableInt_ReturnsInt()
    {
        Assert.Equal(7, ParameterBinder.ConvertValue("7", typeof(int?)));
    }

    [Fact]
    public void ConvertValue_EnumIgnoringCase_ReturnsEnum()
    {
        Assert.Equal(Color.Green, ParameterBinder.ConvertValue("green", typeof(Color)));
    }

    [Fact]
    public void ConvertValue_Guid_ParsesGuid()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(guid, ParameterBinder.ConvertValue(guid.ToString(), typeof(Guid)));
    }

    [Fact]
    public void ConvertValue_InvalidEnum_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ParameterBinder.ConvertValue("blue", typeof(Color)));
    }
}