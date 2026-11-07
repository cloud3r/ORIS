using CustomHttpServer.Attributes;

namespace CustomHttpServer.Controllers;

[Controller("auth")]
public class AuthController
{
    [Get("login")]
    public string Login()
    {
        string viewsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "static",
            "loginform"
        );

        string htmlPath = Path.Combine(viewsPath, "login.html");

        return File.Exists(htmlPath)
            ? File.ReadAllText(htmlPath)
            : "<h1>Login page not found!</h1>";
    }
    
    [Post("login")]
    public string Login(string email, string password)
    {
        Console.WriteLine($"Login: {email}");
        Console.WriteLine($"Password: {password}");

        return "OK";
    }
}