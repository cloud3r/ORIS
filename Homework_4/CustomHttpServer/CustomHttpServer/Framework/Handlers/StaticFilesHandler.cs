using System.Net;
using CustomHttpServer.Helpers;

namespace CustomHttpServer.Framework.Handlers;

public class StaticFilesHandler : Handler
{
    public override async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        string path = request.Url!.LocalPath;

        Console.WriteLine($"StaticFilesHandler: {request.HttpMethod} {request.Url}");

        try
        {
            string root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "static"));
            string filePath = Path.GetFullPath(Path.Combine(root, path.TrimStart('/')));
            
            if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                await SendTextAsync(response, 403, "Forbidden");
                return;
            }

            if (Directory.Exists(filePath))
                filePath = Path.Combine(filePath, "index.html");

            if (File.Exists(filePath))
            {
                await SendFileAsync(response, filePath, 200);
                return;
            }
            
            if (Path.HasExtension(path))
            {
                string notFound = Path.Combine(root, "404.html");
                if (File.Exists(notFound)) await SendFileAsync(response, notFound, 404);
                else await SendTextAsync(response, 404, "Not found");
                return;
            }

            if (Successor != null)
                await Successor.HandleRequest(context);
            else
                await SendTextAsync(response, 404, "Not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            try { await SendTextAsync(response, 500, ex.Message); } catch { }
        }
    }

    private static async Task SendFileAsync(HttpListenerResponse response, string filePath, int status)
    {
        byte[] buffer = await File.ReadAllBytesAsync(filePath);
        response.StatusCode = status;
        response.ContentType = MimeHandler.GetMimeType(filePath);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    private static async Task SendTextAsync(HttpListenerResponse response, int status, string text)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
        response.StatusCode = status;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
}