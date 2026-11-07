namespace CustomHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerAttribute : Attribute
{
    public string? Route { get; }

    public ControllerAttribute(string? route = null)
    {
        Route = route;
    }
}