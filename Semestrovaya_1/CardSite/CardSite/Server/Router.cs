using System.Net;

namespace CardSite.Server;

public class Router
{
    private readonly Dictionary<string, Func<HttpListenerContext, Task>> _routes = new();

    public void MapGet(
        string path,
        Func<HttpListenerContext, Task> handler)
    {
        _routes[$"GET:{path}"] = handler;
    }
    
    public void MapPost(string path, Func<HttpListenerContext, Task> handler)
    {
        _routes[$"POST:{path}"] = handler;
    }

    public bool TryGetHandler(
        string method,
        string path,
        out Func<HttpListenerContext, Task>? handler)
    {
        string key = $"{method}:{path}";

        if (_routes.TryGetValue(key, out handler))
            return true;

        foreach (var route in _routes)
        {
            string[] routeParts = route.Key.Split(':', 2);
            string routeMethod = routeParts[0];
            string routePath = routeParts[1];

            if (routeMethod != method)
                continue;

            string[] templateSegments = routePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            string[] pathSegments = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (templateSegments.Length != pathSegments.Length)
                continue;

            bool matches = true;

            for (int i = 0; i < templateSegments.Length; i++)
            {
                if (templateSegments[i].StartsWith('{') &&
                    templateSegments[i].EndsWith('}'))
                    continue;

                if (templateSegments[i] != pathSegments[i])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                handler = route.Value;
                return true;
            }
        }

        handler = null;
        return false;
    }
}