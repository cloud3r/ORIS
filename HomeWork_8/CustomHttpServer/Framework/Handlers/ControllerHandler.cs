using System.Net;
using System.Reflection;
using CustomHttpServer.Controllers;
using CustomHttpServer.Framework.Results;
using CustomHttpServer.Routing;

namespace CustomHttpServer.Framework.Handlers;

public class ControllersHandler : RequestHandler
{
    private readonly object[] _services;
    private readonly ControllerRegistry _registry;

    public ControllersHandler(object[]? services = null, Assembly? assembly = null)
    {
        _services = services ?? Array.Empty<object>();

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Не удалось получить сборку приложения.");

        _registry = new ControllerRegistry(assembly);
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
                !_registry.TryGetController(segments[0], out Type? controllerType))
            {
                await HandleNextAsync(context);
                return;
            }

            MethodInfo? targetMethod =
                _registry.FindMethod(controllerType!, request.HttpMethod, segments[1]);

            if (targetMethod == null)
            {
                await new TextResult("Method Not Allowed", HttpStatusCode.MethodNotAllowed)
                    .ExecuteAsync(context);
                return;
            }

            string? formBody = await ReadFormBodyAsync(request);
            var values = ParameterBinder.Merge(request.QueryString, formBody);
            BindResult bound = ParameterBinder.Bind(targetMethod, values);

            if (!bound.Success)
            {
                await new TextResult(bound.Error!, HttpStatusCode.BadRequest)
                    .ExecuteAsync(context);
                return;
            }

            BaseController controller = CreateController(controllerType!);
            controller.Context = context;

            object? result = targetMethod.Invoke(controller, bound.Arguments);

            if (result is Task task)
            {
                await task;

                result = task.GetType().IsGenericType
                    ? task.GetType().GetProperty("Result")?.GetValue(task)
                    : null;
            }

            if (result is not IHttpResult httpResult)
                throw new InvalidOperationException(
                    $"Метод {controllerType!.Name}.{targetMethod.Name} должен возвращать IHttpResult.");

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

    private static async Task<string?> ReadFormBodyAsync(HttpListenerRequest request)
    {
        if (!request.HasEntityBody ||
            request.ContentType?.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase) != true)
            return null;

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        return await reader.ReadToEndAsync();
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

                if (service == null) { canCreate = false; break; }
                arguments[i] = service;
            }

            if (canCreate)
                return (BaseController)constructor.Invoke(arguments);
        }

        throw new InvalidOperationException($"Не удалось создать контроллер {controllerType.Name}.");
    }
}