using System.Net;
using CustomHttpServer.Framework.Content;
using CustomHttpServer.Framework.Helpers;

namespace CustomHttpServer.Framework.Handlers;

public class StaticFilesHandler : RequestHandler
{
    private readonly string _staticDirectory;

    public StaticFilesHandler()
    {
        _staticDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "static"
        );
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string requestedPath = Uri.UnescapeDataString(
            request.Url!.AbsolutePath
        );
        
        if (!requestedPath.EndsWith('/'))
        {
            string? directoryPath = UriHelper.ResolveSafePath(
                requestedPath.TrimStart('/'),
                _staticDirectory
            );

            if (directoryPath != null && Directory.Exists(directoryPath))
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.RedirectLocation = requestedPath + "/";
                response.Close();
                return;
            }
        }

        string relativePath = requestedPath.TrimStart('/');
        
        if (relativePath.Equals(
                "SearcherFinder",
                StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals(
                "SearcherFinder/",
                StringComparison.OrdinalIgnoreCase))
        {
            relativePath = "SearcherFinder/search-engine.html";
        }
        
        if (string.IsNullOrEmpty(relativePath))
        {
            relativePath = "SearcherFinder/search-engine.html";
        }
        
        string? filePath = UriHelper.ResolveSafePath(
            relativePath,
            _staticDirectory
        );

        if (filePath == null)
        {
            await HandleNextAsync(context);
            return;
        }
        
        if (Directory.Exists(filePath))
        {
            filePath = Path.Combine(filePath, "index.html");
        }
        
        if (!File.Exists(filePath))
        {
            await HandleNextAsync(context);
            return;
        }
        
        byte[] buffer = await File.ReadAllBytesAsync(filePath);
        
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = MimeTypeProvider.GetMimeType(filePath);
        response.ContentLength64 = buffer.Length;
        
        await response.OutputStream.WriteAsync(buffer);

        Console.WriteLine($"Файл отправлен: {filePath}");
        Console.WriteLine("HTTP 200 OK");

        response.Close();
    }
}