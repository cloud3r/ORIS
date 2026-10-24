namespace CustomHttpServer.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PostAttribute : Attribute
{

    public string Route { get; } 
    public PostAttribute(string route)
    {
        Route = route; 
    }
}