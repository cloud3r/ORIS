using CustomHttpServer.Framework.Content;
using Xunit;

namespace CustomHttpServer.UnitTests;

public class MimeTypeProviderTests
{
    [Theory]
    [InlineData("index.html", "text/html")]
    [InlineData("style.css", "text/css")]
    [InlineData("app.js", "application/javascript")]
    [InlineData("logo.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("INDEX.HTML", "text/html")]
    public void GetMimeType_KnownExtension_ReturnsExpectedType(string file, string expected)
    {
        var result = MimeTypeProvider.GetMimeType(file);

        Assert.StartsWith(expected, result);
    }

    [Fact]
    public void GetMimeType_UnknownExtension_ReturnsBinaryFallback()
    {
        var result = MimeTypeProvider.GetMimeType("file.unknownext");

        Assert.Equal("application/octet-stream", result);
    }
}