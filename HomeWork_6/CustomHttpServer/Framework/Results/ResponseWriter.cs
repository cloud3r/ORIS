using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace CustomHttpServer.Framework.Results;

internal static class ResponseWriter
{
    private static readonly ConditionalWeakTable<HttpListenerResponse, object> Completed = new();

    public static bool IsCompleted(HttpListenerResponse response) =>
        Completed.TryGetValue(response, out _);

    public static async Task WriteAsync(
        HttpListenerContext context,
        HttpStatusCode status,
        string body,
        string contentType)
    {
        HttpListenerResponse response = context.Response;

        lock (Completed)
        {
            if (Completed.TryGetValue(response, out _))
                return;

            Completed.Add(response, new object());
        }

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);

            response.StatusCode = (int)status;
            response.ContentType = $"{contentType}; charset=utf-8";
            response.ContentLength64 = bytes.Length;

            await response.OutputStream.WriteAsync(bytes);
        }
        finally
        {
            try { response.Close(); }
            catch { response.Abort(); }
        }
    }
}