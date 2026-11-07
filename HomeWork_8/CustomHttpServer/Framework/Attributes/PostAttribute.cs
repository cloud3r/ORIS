namespace CustomHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PostAttribute : Attribute
{
    public string? Route { get; }

    public PostAttribute(string? route = null)
    {
        Route = route;
    }
}