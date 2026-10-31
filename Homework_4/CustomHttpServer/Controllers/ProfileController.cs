using CustomHttpServer.Attributes;
using CustomHttpServer.Models;
using CustomHttpServer.Services;

namespace CustomHttpServer.Controllers;

[Controller("profile")]
public class ProfileController
{
    [Get("index")]
    public string Index()
    {
        var user = new User
        {
            Name = "Вася",
            Group = "11-507",
            Age = 21,
            Gender = true
        };

        var renderer = new HtmlTemplateRenderer();

        return renderer.RenderFromFile(
            "Templates/profile.html",
            user);
    }
}