using UnitTests;
using Xunit;

namespace CustomHttpServer.UnitTests;

public class HandlerChainTests
{
    [Fact]
    public async Task HandleAsync_HandlerCallsNext_PassesControlInChainOrder()
    {
        var log = new List<string>();
        var first = new RecordingHandler("first", log);
        var second = new RecordingHandler("second", log);
        var third = new RecordingHandler("third", log);
        first.SetNext(second);
        second.SetNext(third);

        await first.HandleAsync(null!);

        Assert.Equal(new[] { "first", "second", "third" }, log);
    }

    [Fact]
    public async Task HandleAsync_HandlerDoesNotCallNext_StopsChain()
    {
        var log = new List<string>();
        var first = new RecordingHandler("first", log, callNext: false);
        var second = new RecordingHandler("second", log);
        first.SetNext(second);

        await first.HandleAsync(null!);

        Assert.Equal(new[] { "first" }, log);
    }

    [Fact]
    public async Task HandleAsync_LastHandlerWithoutNext_DoesNotThrow()
    {
        var log = new List<string>();
        var only = new RecordingHandler("only", log);

        var exception = await Record.ExceptionAsync(() => only.HandleAsync(null!));

        Assert.Null(exception);
        Assert.Equal(new[] { "only" }, log);
    }
}