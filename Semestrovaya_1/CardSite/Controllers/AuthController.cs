using System.Net;
using System.Text;
using CardSite.Server;
using CardSite.Services;
using MiniTemplateEngine.Interfaces;
using CardSite.Services.Interfaces;

namespace CardSite.Controllers;

public class AuthController
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;
    private readonly IHtmlTemplateRenderer _templateRenderer;
    private readonly string _viewsPath;
    
    public AuthController(
        IAuthService authService,
        ISessionService sessionService,
        IHtmlTemplateRenderer templateRenderer,
        string viewsPath)
    {
        _authService = authService;
        _sessionService = sessionService;
        _templateRenderer = templateRenderer;
        _viewsPath = viewsPath;
    }

    public async Task ShowLoginPageAsync(HttpListenerContext context)
    {
        try
        {
            string templatePath = Path.Combine(_viewsPath, "Admin", "Login.html");
            string html = await File.ReadAllTextAsync(templatePath);

            string result = _templateRenderer.RenderFromString(
                html,
                new { ErrorMessage = string.Empty });

            byte[] buffer = Encoding.UTF8.GetBytes(result);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        finally
        {
            context.Response.Close();
        }
    }

    public async Task LoginAsync(HttpListenerContext context)
    {
        try
        {
            var formValues = await FormDataReader.ReadFormValuesAsync(
                context.Request);

            var form = formValues.ToDictionary(
                item => item.Key,
                item => item.Value.FirstOrDefault() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            form.TryGetValue("username", out string? username);
            form.TryGetValue("password", out string? password);

            var admin = await _authService.AuthenticateAsync(
                username ?? string.Empty,
                password ?? string.Empty);

            if (admin == null)
            {
                await ShowErrorAsync(context);
                return;
            }

            string token = _sessionService.CreateSession(admin.Username);

            context.Response.Cookies.Add(new Cookie(
                "CardSiteSession",
                token,
                "/")
            {
                HttpOnly = true,
                Secure = false
            });

            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.RedirectLocation = "/admin";
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task ShowErrorAsync(HttpListenerContext context)
    {
        string templatePath = Path.Combine(_viewsPath, "Admin", "Login.html");
        string html = await File.ReadAllTextAsync(templatePath);

        string result = _templateRenderer.RenderFromString(
            html,
            new { ErrorMessage = "Неверный логин или пароль." });

        byte[] buffer = Encoding.UTF8.GetBytes(result);

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
    }
    
    public async Task LogoutAsync(HttpListenerContext context)
    {
        try
        {
            string? token =
                context.Request.Cookies["CardSiteSession"]?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                _sessionService.RemoveSession(token);
            }

            context.Response.Cookies.Add(new Cookie(
                "CardSiteSession",
                string.Empty,
                "/")
            {
                HttpOnly = true,
                Secure = false,
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.RedirectLocation = "/admin/login";
        }
        finally
        {
            context.Response.Close();
        }
    }
}