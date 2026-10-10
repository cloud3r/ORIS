namespace CustomHttpServer.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class GetAttribute : Attribute
{

    public string Route { get; } 
    public GetAttribute(string route)
    {
        Route = route; 
    }
}