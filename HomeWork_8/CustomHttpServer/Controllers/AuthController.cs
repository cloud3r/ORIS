using System.Net;
using System.Net.Mail;
using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Framework.Results;
using CustomHttpServer.Services;

namespace CustomHttpServer.Controllers;

[Controller("auth")]
public class AuthController : BaseController
{
    private readonly IEmailService _emailService;

    public AuthController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [Get("login")]
    public IHttpResult GetLoginPage()
    {
        return Html("""
                    <!DOCTYPE html>
                    <html lang="ru">
                    <head>
                        <meta charset="UTF-8">
                        <meta http-equiv="refresh" content="0; url=/Rockstar/">
                        <title>Redirecting...</title>
                    </head>
                    <body>
                        <p>Перенаправление на страницу авторизации...</p>
                    </body>
                    </html>
                    """);
    }

    [Post("login")]
    public IHttpResult Login(string email, string password, bool check = false)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[SIGN IN]");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Remember me: {check}");
        Console.ResetColor();

        string safeEmail = WebUtility.HtmlEncode(email);

        return Html($"""
                     <h1>Login successful!</h1>
                     <p>You signed in as {safeEmail}</p>
                     <p>Remember me: {(check ? "yes" : "no")}</p>
                     """);
    }

    [Get("send")]
    [Post("send")]
    public IHttpResult SendEmail(string to = "")
    {
        if (!MailAddress.TryCreate(to, out _))
        {
            return Json(
                new { success = false, message = "Некорректный адрес получателя" },
                HttpStatusCode.BadRequest);
        }

        try
        {
            _emailService.Send(to, "Тестовое письмо", "Тестовое сообщение от CustomHttpServer");

            return Json(new { success = true, message = "Сообщение отправлено" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex}");

            return Json(
                new { success = false, message = "Не удалось отправить сообщение" },
                HttpStatusCode.InternalServerError);
        }
    }
}