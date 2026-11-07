using System.Reflection;
using CustomHttpServer.Controllers;
using CustomHttpServer.Framework.Attributes;

namespace CustomHttpServer.Routing;

public class ControllerRegistry
{
    private readonly Dictionary<string, Type> _controllers =
        new(StringComparer.OrdinalIgnoreCase);

    public ControllerRegistry(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            ControllerAttribute? attribute = type.GetCustomAttribute<ControllerAttribute>();

            if (attribute == null || type.IsAbstract || !typeof(BaseController).IsAssignableFrom(type))
                continue;

            string route = attribute.Route
                           ?? (type.Name.EndsWith("Controller", StringComparison.Ordinal)
                               ? type.Name[..^"Controller".Length]
                               : type.Name);

            _controllers["/" + route.Trim('/')] = type;
        }
    }

    public bool TryGetController(string segment, out Type? controllerType) =>
        _controllers.TryGetValue("/" + segment.Trim('/'), out controllerType);

    public MethodInfo? FindMethod(Type controllerType, string httpMethod, string route)
    {
        bool isGet = httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
        bool isPost = httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase);

        return controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m =>
                (isGet && m.GetCustomAttribute<GetAttribute>() is { } g &&
                 (g.Route ?? m.Name).Equals(route, StringComparison.OrdinalIgnoreCase)) ||
                (isPost && m.GetCustomAttribute<PostAttribute>() is { } p &&
                 (p.Route ?? m.Name).Equals(route, StringComparison.OrdinalIgnoreCase)));
    }
}