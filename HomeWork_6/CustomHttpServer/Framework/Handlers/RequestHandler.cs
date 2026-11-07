using System.Net;

namespace CustomHttpServer.Framework.Handlers;

public abstract class RequestHandler
{
    private RequestHandler? _nextHandler;

    public void SetNext(RequestHandler nextHandler)
    {
        _nextHandler = nextHandler;
    }

    public abstract Task HandleAsync(HttpListenerContext context);

    protected async Task HandleNextAsync(HttpListenerContext context)
    {
        if (_nextHandler != null)
        {
            await _nextHandler.HandleAsync(context);
        }
    }
}