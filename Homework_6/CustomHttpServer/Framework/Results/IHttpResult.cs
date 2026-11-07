using System.Net;

namespace CustomHttpServer.Framework.Results;

public interface IHttpResult
{
    Task ExecuteAsync(HttpListenerContext context);
}