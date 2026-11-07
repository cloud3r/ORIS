using System.Globalization;
using System.Net;
using System.Reflection;
using System.Web;
using CustomHttpServer.Controllers;
using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Framework.Handlers;

public class ControllersHandler : RequestHandler
{
    private readonly object[] _services;

    private readonly Dictionary<string, Type> _controllers =
        new(StringComparer.OrdinalIgnoreCase);

    public ControllersHandler(object[]? services = null)
    {
        _services = services ?? Array.Empty<object>();

        Assembly assembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Не удалось получить сборку приложения.");

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

    public override async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;

        try
        {
            string[] segments = (request.Url?.AbsolutePath ?? "/")
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length != 2 ||
                !_controllers.TryGetValue("/" + segments[0], out Type? controllerType))
            {
                await HandleNextAsync(context);
                return;
            }

            string methodRoute = segments[1];
            bool isGet = request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            bool isPost = request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase);
            
            MethodInfo? targetMethod = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    (isGet && m.GetCustomAttribute<GetAttribute>() is { } g &&
                        (g.Route ?? m.Name).Equals(methodRoute, StringComparison.OrdinalIgnoreCase)) ||
                    (isPost && m.GetCustomAttribute<PostAttribute>() is { } p &&
                        (p.Route ?? m.Name).Equals(methodRoute, StringComparison.OrdinalIgnoreCase)));

            if (targetMethod == null)
            {
                await new TextResult("Method Not Allowed", HttpStatusCode.MethodNotAllowed)
                    .ExecuteAsync(context);
                return;
            }
            
            Dictionary<string, string> values = await ReadParametersAsync(request);

            ParameterInfo[] parameters = targetMethod.GetParameters();
            object?[] arguments = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                string name = parameter.Name ?? $"arg{i}";

                if (values.TryGetValue(name, out string? value))
                {
                    try
                    {
                        arguments[i] = ConvertValue(value, parameter.ParameterType);
                    }
                    catch (Exception ex) when (
                        ex is FormatException or OverflowException or
                              ArgumentException or InvalidCastException)
                    {
                        await new TextResult($"Invalid parameter: {name}", HttpStatusCode.BadRequest)
                            .ExecuteAsync(context);
                        return;
                    }
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else
                {
                    await new TextResult($"Missing parameter: {name}", HttpStatusCode.BadRequest)
                        .ExecuteAsync(context);
                    return;
                }
            }

            BaseController controller = CreateController(controllerType);
            controller.Context = context;

            object? result = targetMethod.Invoke(controller, arguments);
            
            if (result is Task task)
            {
                await task;

                result = task.GetType().IsGenericType
                    ? task.GetType().GetProperty("Result")?.GetValue(task)
                    : null;
            }

            if (result is not IHttpResult httpResult)
                throw new InvalidOperationException(
                    $"Метод {controllerType.Name}.{targetMethod.Name} должен возвращать IHttpResult.");

            await httpResult.ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex is TargetInvocationException { InnerException: not null } tie
                ? tie.InnerException
                : ex);
            
            if (!ResponseWriter.IsCompleted(context.Response))
            {
                try
                {
                    await new TextResult("Internal server error", HttpStatusCode.InternalServerError)
                        .ExecuteAsync(context);
                }
                catch
                {
                    context.Response.Abort();
                }
            }
        }
    }

    private BaseController CreateController(Type controllerType)
    {
        foreach (ConstructorInfo constructor in controllerType.GetConstructors())
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            object?[] arguments = new object?[parameters.Length];
            bool canCreate = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                object? service = _services.FirstOrDefault(s => parameterType.IsInstanceOfType(s));

                if (service == null)
                {
                    canCreate = false;
                    break;
                }

                arguments[i] = service;
            }

            if (canCreate)
                return (BaseController)constructor.Invoke(arguments);
        }

        throw new InvalidOperationException($"Не удалось создать контроллер {controllerType.Name}.");
    }

    private static async Task<Dictionary<string, string>> ReadParametersAsync(HttpListenerRequest request)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? key in request.QueryString.AllKeys)
        {
            if (key != null)
                result[key] = request.QueryString[key] ?? "";
        }

        if (request.HasEntityBody &&
            request.ContentType?.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var form = HttpUtility.ParseQueryString(await reader.ReadToEndAsync());

            foreach (string? key in form.AllKeys)
            {
                if (key != null)
                    result[key] = form[key] ?? "";
            }
        }

        return result;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType == typeof(string)) return value;
        if (actualType.IsEnum) return Enum.Parse(actualType, value, true);
        if (actualType == typeof(Guid)) return Guid.Parse(value);
        if (actualType == typeof(bool))
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "true" or "on" or "1" or "yes" => true,
                "false" or "off" or "0" or "no" or "" => false,
                _ => throw new FormatException()
            };
        }

        return Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
    }
}