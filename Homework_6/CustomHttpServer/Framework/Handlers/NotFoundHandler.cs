using System.Net;
using System.Text;

namespace CustomHttpServer.Framework.Handlers;

public class NotFoundHandler : RequestHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerResponse response = context.Response;

        string message = "404 - Requested resource not found";

        byte[] buffer = Encoding.UTF8.GetBytes(message);

        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);

        Console.WriteLine("HTTP 404 Not Found");

        response.Close();
    }
}