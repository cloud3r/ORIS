using CustomHttpServer.Attributes;

namespace CustomHttpServer.Controllers;

[Controller("auth")]
public class AuthController
{
    [Get("login")]
    public string Login()
    {
        return "Hello from AuthController!";
    }
    
    [Post("login")]
    public string Login(string email, string password)
    {
        Console.WriteLine($"Login: {email}");
        Console.WriteLine($"Password: {password}");
        return "OK";
    }
    
}