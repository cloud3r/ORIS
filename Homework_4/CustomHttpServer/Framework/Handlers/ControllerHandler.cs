using System.Net;
using System.Reflection;
using System.Web;
using CustomHttpServer.Attributes;

namespace CustomHttpServer.Framework.Handlers;

public class ControllerHandler : Handler
{
    public async override Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        Console.WriteLine($"Method: {request.HttpMethod}");
        Console.WriteLine($"ContentType: {request.ContentType}");
        Console.WriteLine($"ContentLength: {request.ContentLength64}");

        try
        {
            bool isFile = request.Url!.LocalPath.Contains('.');

            if (isFile)
            {
                if (Successor != null) await Successor.HandleRequest(context);
                else await Send(response, 404, "Not found");
                return;
            }

            string[] segments = request.Url.Segments
                .Select(s => s.Trim('/'))
                .Where(s => s.Length > 0)
                .ToArray();

            if (segments.Length < 2)
            {
                await Send(response, 404, "Not found");
                return;
            }

            string controllerRoute = segments[0];
            string methodRoute = segments[1];
            string attributeName =
                $"{request.HttpMethod[0]}{request.HttpMethod[1..].ToLower()}Attribute";

            var controller = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.GetCustomAttribute<ControllerAttribute>()?.Route == controllerRoute);

            if (controller == null)
            {
                await Send(response, 404, "Controller not found");
                return;
            }

            var method = controller.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttributes(true)
                    .Any(a => a.GetType().Name == attributeName
                              && (a as dynamic).Route == methodRoute));

            if (method == null)
            {
                await Send(response, 404, "Method not found");
                return;
            }

            var parameters = method.GetParameters();
            var form = await ReadFormAsync(request);
            var urlArgs = segments.Skip(2).ToArray();
            int urlIndex = 0;

            object?[] queryParams = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.Name != null && form.TryGetValue(p.Name, out var value))
                    queryParams[i] = Convert.ChangeType(value, p.ParameterType);
                else if (urlIndex < urlArgs.Length)
                    queryParams[i] = Convert.ChangeType(urlArgs[urlIndex++], p.ParameterType);
                else if (p.HasDefaultValue)
                    queryParams[i] = p.DefaultValue;
                else
                {
                    await Send(response, 400, $"Missing parameter: {p.Name}");
                    return;
                }
            }

            var ret = method.Invoke(Activator.CreateInstance(controller), queryParams);

            if (ret is Task t)
            {
                await t;
                ret = t.GetType().IsGenericType ? ((dynamic)t).Result : null;
            }

            await Send(response, 200, ret?.ToString() ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            try
            {
                await Send(response, 500, "Internal server error");
            }
            catch
            {
            }
        }

        static async Task Send(HttpListenerResponse response, int status, string body)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
            response.StatusCode = status;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
        }
        static async Task<Dictionary<string, string>> ReadFormAsync(HttpListenerRequest request)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // данные из query string, если есть (?email=...)
            foreach (string? key in request.QueryString.AllKeys)
                if (key != null) result[key] = request.QueryString[key] ?? "";

            // данные из тела формы (POST, application/x-www-form-urlencoded)
            if (request.HasEntityBody &&
                request.ContentType?.StartsWith("application/x-www-form-urlencoded",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var parsed = HttpUtility.ParseQueryString(body);

                foreach (string? key in parsed.AllKeys)
                    if (key != null) result[key] = parsed[key] ?? "";
            }

            return result;
        }
    }
}
