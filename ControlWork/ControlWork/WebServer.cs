using System.Net;
using System.Text;
using System.Text.Json;

namespace ControlWork;

public class WebServer
{
    private readonly HttpListener listener = new();
    private readonly WebsiteService service = new();

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebServer(string prefix)
    {
        listener.Prefixes.Add(prefix);
    }

    public async Task Start()
    {
        listener.Start();

        Console.WriteLine("Server started!");

        while (true)
        {
            HttpListenerContext context =
                await listener.GetContextAsync();

            _ = Task.Run(() => HandleRequest(context));
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod;
        string path = context.Request.Url!.AbsolutePath;

        try
        {
            if (method == "GET" && path == "/websites")
            {
                await GetWebsites(context);
            }
            else if (method == "GET" && path == "/websites/details")
            {
                await GetWebsiteDetails(context);
            }
            else if (method == "POST" && path == "/websites/create")
            {
                await CreateWebsite(context);
            }
            else if (method == "PUT" && path == "/websites/update")
            {
                await UpdateWebsite(context);
            }
            else
            {
                await SendResponse(context, 404, new
                {
                    message = "Route not found"
                });
            }
        }
        catch (JsonException)
        {
            await SendResponse(context, 400, new
            {
                message = "Invalid JSON format"
            });
        }
        catch (Exception ex)
        {
            await SendResponse(context, 500, new
            {
                message = ex.Message
            });
        }
    }

    // GET /websites?type=Search
private async Task GetWebsites(HttpListenerContext context)
{
    string? type = context.Request.QueryString["type"];
    string? scheme = context.Request.QueryString["scheme"];

    var websites = service.GetAll(type);

    // Фильтрация по схеме URL
    if (scheme == "http" || scheme == "https")
    {
        websites = websites
            .Where(w => Uri.TryCreate(w.Url, UriKind.Absolute, out var uri)
                        && uri.Scheme == scheme)
            .ToList();
    }

    StringBuilder html = new StringBuilder();

    html.Append("""
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <title>Websites Catalog</title>
        </head>
        <body>
            <h1>Websites Catalog</h1>
        """);

    foreach (var website in websites)
    {
        html.Append($"""
            <div class="website">
                <h2>{WebUtility.HtmlEncode(website.Name)}</h2>
                <p>URL: 
                    <a href="{WebUtility.HtmlEncode(website.Url)}">
                        {WebUtility.HtmlEncode(website.Url)}
                    </a>
                </p>
                <p>Type: {WebUtility.HtmlEncode(website.Type)}</p>
                <p class="{WebUtility.HtmlEncode(website.Status)}">
                    Status: {WebUtility.HtmlEncode(website.Status)}
                </p>
            </div>
            """);
    }

    html.Append("""
        </body>
        </html>
        """);

    byte[] buffer = Encoding.UTF8.GetBytes(html.ToString());

    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength64 = buffer.Length;

    await context.Response.OutputStream.WriteAsync(buffer);

    context.Response.Close();
}

    // GET /websites/details?id=1
    private async Task GetWebsiteDetails(HttpListenerContext context)
    {
        string? idString = context.Request.QueryString["id"];

        if (!int.TryParse(idString, out int id))
        {
            await SendResponse(context, 400, new
            {
                message = "Invalid ID"
            });

            return;
        }

        Website? website = service.GetById(id);

        if (website == null)
        {
            await SendResponse(context, 404, new
            {
                message = "Website not found"
            });

            return;
        }

        await SendResponse(context, 200, website);
    }

    // POST /websites/create
    private async Task CreateWebsite(HttpListenerContext context)
    {
        using StreamReader reader = new(context.Request.InputStream);

        string body = await reader.ReadToEndAsync();

        var data = JsonSerializer.Deserialize<CreateWebsiteRequest>(
            body, jsonOptions);

        if (data == null)
        {
            await SendResponse(context, 400, new
            {
                message = "Invalid JSON"
            });
            return;
        }

        string? error;

        error = WebsiteValidator.ValidateName(data.Name);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        error = WebsiteValidator.ValidateType(data.Type);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        error = WebsiteValidator.ValidateUrl(data.Url);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        Website website = service.Create(
            data.Name,
            data.Url,
            data.Type
        );

        await SendResponse(context, 201, website);
    }

    // PUT /websites/update?id=1
    private async Task UpdateWebsite(HttpListenerContext context)
    {
        string? idString = context.Request.QueryString["id"];

        if (!int.TryParse(idString, out int id))
        {
            await SendResponse(context, 400, new
            {
                message = "Invalid ID"
            });
            return;
        }

        using StreamReader reader = new(context.Request.InputStream);

        string body = await reader.ReadToEndAsync();

        var data = JsonSerializer.Deserialize<UpdateWebsiteRequest>(
            body, jsonOptions);

        if (data == null)
        {
            await SendResponse(context, 400, new
            {
                message = "Invalid JSON"
            });
            return;
        }

        string? error;

        error = WebsiteValidator.ValidateName(data.Name);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        error = WebsiteValidator.ValidateType(data.Type);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        error = WebsiteValidator.ValidateUrl(data.Url);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        error = WebsiteValidator.ValidateStatus(data.Status);
        if (error != null)
        {
            await SendResponse(context, 400, new { message = error });
            return;
        }

        bool updated = service.Update(
            id,
            data.Name,
            data.Url,
            data.Type,
            data.Status
        );

        if (!updated)
        {
            await SendResponse(context, 404, new
            {
                message = "Website not found"
            });
            return;
        }

        await SendResponse(context, 200, new
        {
            message = "Website updated successfully"
        });
    }
    
    private async Task SendResponse(
        HttpListenerContext context,
        int statusCode,
        object data)
    {
        string json = JsonSerializer.Serialize(data);

        byte[] buffer = Encoding.UTF8.GetBytes(json);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);

        context.Response.Close();
    }
}