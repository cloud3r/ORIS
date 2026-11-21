using System.Net;
using System.Text;
using CardSite.Controllers;
using CardSite.ORM;
using CardSite.Services;
using CardSite.Repositories;
using MiniTemplateEngine;
using CardSite.Services.Interfaces;
using MiniTemplateEngine.Interfaces;

namespace CardSite.Server;

public class WebServer
{
    private readonly HttpListener _listener = new();
    private readonly string _wwwrootPath;
    private readonly CardSiteContext _context;
    private readonly CardsController _cardsController;
    private readonly AdminController _adminController;
    private readonly Router _router = new();
    private readonly IHtmlTemplateRenderer _templateRenderer = new TemplateEngine();
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService = new SessionService();
    private readonly AuthController _authController;

    public WebServer()
    {
        _wwwrootPath = FindWwwroot();

        _listener.Prefixes.Add("http://+:8080/");

        string connectionString =
            Environment.GetEnvironmentVariable("CARDSITE_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "CARDSITE_CONNECTION_STRING is not configured.");

        _context = new CardSiteContext(connectionString);

        var cardRepository = new CardRepository(_context);

        var cardService = new CardService(cardRepository);

        var adminUserRepository = new AdminUserRepository(_context);
        _authService = new AuthService(adminUserRepository);

        string viewsRoot = Path.Combine(
            Directory.GetParent(_wwwrootPath)!.FullName,
            "Views");

        string cardsViewsPath = Path.Combine(
            viewsRoot,
            "Cards");

        _cardsController = new CardsController(
            cardService,
            _templateRenderer,
            cardsViewsPath);

        _adminController = new AdminController(
            cardService,
            _templateRenderer,
            viewsRoot);
        
        _authController = new AuthController(
            _authService,
            _sessionService,
            _templateRenderer,
            viewsRoot);

        _router.MapGet("/", _cardsController.GetCardsPageAsync);
        _router.MapGet("/cards", _cardsController.GetCardsPageAsync);

        _router.MapGet("/api/cards", _cardsController.GetCardsAsync);
        _router.MapGet("/api/cards/{id}", _cardsController.GetCardByIdAsync);

        _router.MapGet("/cards/{id}", _cardsController.GetCardDetailsPageAsync);
        
        _router.MapGet("/admin/login", _authController.ShowLoginPageAsync);
        _router.MapPost("/admin/login", _authController.LoginAsync);
        _router.MapPost("/admin/logout", _authController.LogoutAsync);
        
        _router.MapGet("/admin", _adminController.ShowCreateCardPageAsync);
        _router.MapPost("/admin", _adminController.CreateCardAsync);
        _router.MapGet("/admin/cards", _adminController.ShowCardsAsync);
        _router.MapGet(
            "/admin/cards/edit",
            _adminController.ShowEditCardPageAsync);
        _router.MapPost(
            "/admin/cards/edit",
            _adminController.UpdateCardAsync);
        _router.MapPost(
            "/admin/cards/delete",
            _adminController.DeleteCardAsync);
    }

    private static string FindWwwroot()
    {
        string current = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(current))
        {
            string candidate = Path.Combine(current, "wwwroot");

            if (Directory.Exists(candidate))
                return candidate;

            DirectoryInfo? parent = Directory.GetParent(current);

            if (parent is null)
                break;

            current = parent.FullName;
        }

        string workingCandidate = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot");

        if (Directory.Exists(workingCandidate))
            return workingCandidate;

        throw new DirectoryNotFoundException(
            "Cannot find wwwroot. Make sure wwwroot is beside the project file, " +
            "or configure it to copy to the output directory.");
    }

    public async Task StartAsync()
    {
        await _authService.EnsureInitialAdminAsync();
        
        _listener.Start();

        Console.WriteLine("CardSite server started at http://localhost:8080/");
        Console.WriteLine($"Static files: {_wwwrootPath}");
        Console.WriteLine("Press Ctrl+C to stop.");

        while (_listener.IsListening)
        {
            try
            {
                HttpListenerContext context =
                    await _listener.GetContextAsync();

                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (HttpListenerException)
            {
                if (!_listener.IsListening)
                    break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Listener error: {ex}");

                if (!_listener.IsListening)
                    break;

                await Task.Delay(100);
            }
        }

        Console.WriteLine("CardSite server stopped.");
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        string path = context.Request.Url?.AbsolutePath ?? "/";

        try
        {
            string method = context.Request.HttpMethod.ToUpperInvariant();

            if (method != "GET" && method != "POST")
            {
                await RedirectToHomeAsync(context);
                return;
            }
            
            if (path.Equals("/admin", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                bool isLoginPage =
                    path.Equals("/admin/login", StringComparison.OrdinalIgnoreCase);

                if (!isLoginPage)
                {
                    string? sessionToken =
                        context.Request.Cookies["CardSiteSession"]?.Value;

                    string? username = sessionToken == null
                        ? null
                        : _sessionService.GetUsername(sessionToken);

                    if (username == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                        context.Response.RedirectLocation = "/admin/login";
                        context.Response.Close();
                        return;
                    }
                }
            }

            if (_router.TryGetHandler(
                    method,
                    path,
                    out var handler))
            {
                await handler!(context);
                return;
            }

            if (method == "GET")
            {
                await SendStaticFileAsync(context, path);
                return;
            }

            await RedirectToHomeAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("========== REQUEST ERROR ==========");
            Console.WriteLine($"URL: {path}");
            Console.WriteLine($"Method: {context.Request.HttpMethod}");
            Console.WriteLine($"Error: {ex}");
            Console.WriteLine("===================================");
            Console.WriteLine();

            if (path == "/")
            {
                await SendFallbackPageAsync(context);
                return;
            }

            await RedirectToHomeAsync(context);
        }
    }

    private async Task SendStaticFileAsync(
        HttpListenerContext context,
        string urlPath)
    {
        string relativePath = Uri.UnescapeDataString(
                urlPath.TrimStart('/'))
            .Replace('/', Path.DirectorySeparatorChar);

        string root = Path.GetFullPath(_wwwrootPath);

        string fullPath = Path.GetFullPath(
            Path.Combine(root, relativePath));

        string rootWithSeparator =
            root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase)
            &&
            !string.Equals(
                fullPath,
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            await RedirectToHomeAsync(context);
            return;
        }

        if (!File.Exists(fullPath))
        {
            await RedirectToHomeAsync(context);
            return;
        }

        byte[] bytes = await File.ReadAllBytesAsync(fullPath);

        context.Response.StatusCode = 200;
        context.Response.ContentType = MimeTypes.GetContentType(fullPath);
        context.Response.ContentLength64 = bytes.Length;

        await context.Response.OutputStream.WriteAsync(bytes);

        context.Response.Close();
    }

    private static async Task RedirectToHomeAsync(
        HttpListenerContext context)
    {
        try
        {
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = "/";
            context.Response.Close();

            await Task.CompletedTask;
        }
        catch
        {
            try
            {
                context.Response.Abort();
            }
            catch
            {
            }
        }
    }

    private static async Task SendFallbackPageAsync(
        HttpListenerContext context)
    {
        const string html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>CardSite</title>
                <style>
                    body {
                        margin: 0;
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        background: #07131b;
                        color: white;
                        font-family: Arial, sans-serif;
                        text-align: center;
                    }

                    a {
                        color: #00c8e9;
                    }
                </style>
            </head>
            <body>
                <main>
                    <h1>CardSite is temporarily unavailable</h1>
                    <p>Please try again later.</p>
                    <a href="/">Try again</a>
                </main>
            </body>
            </html>
            """;

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);

            context.Response.StatusCode = 503;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);

            context.Response.Close();
        }
        catch
        {
            try
            {
                context.Response.Abort();
            }
            catch
            {
            }
        }
    }

    public void Stop()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
            Console.WriteLine("Stopping CardSite server...");
        }
    }
}