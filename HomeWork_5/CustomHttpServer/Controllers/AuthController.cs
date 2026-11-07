using CustomHttpServer.Framework.Attributes;

namespace CustomHttpServer.Controllers;

[Controller("auth")]
public class AuthController
{
    [Get("login")]
    public string GetLoginPage()
    {
        return """
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
               """;
    }

    [Post("login")]
    public string Login(string email, string password, bool check = false)
    {
        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine("[SIGN IN]");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Remember me: {check}");

        Console.ResetColor();

        return $"""
                <h1>Login successful!</h1>
                <p>You signed in as {email}</p>
                <p>Remember me: {(check ? "yes" : "no")}</p>
                """;
    }
}