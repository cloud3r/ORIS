using System.Globalization;
using System.Net;
using System.Reflection;
using System.Web;
using CustomHttpServer.Framework.Attributes;

namespace CustomHttpServer.Framework.Handlers;

public class ControllerHandler : RequestHandler
{
    private readonly object[] _services;

    private readonly Dictionary<string, Type> _controllers =
        new(StringComparer.OrdinalIgnoreCase);

    public ControllerHandler(object[]? services = null)
    {
        _services = services ?? Array.Empty<object>();

        Assembly assembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "Не удалось получить сборку приложения."
            );

        foreach (Type type in assembly.GetTypes())
        {
            ControllerAttribute? attribute =
                type.GetCustomAttribute<ControllerAttribute>();

            if (attribute == null || type.IsAbstract)
                continue;

            string route = "/" + attribute.Route.Trim('/');

            _controllers[route] = type;
        }
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        bool responseStarted = false;

        async Task Send(
            int status,
            string body,
            string contentType = "text/plain")
        {
            if (responseStarted)
                return;

            responseStarted = true;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);

            response.StatusCode = status;
            response.ContentType = $"{contentType}; charset=utf-8";
            response.ContentLength64 = bytes.Length;

            await response.OutputStream.WriteAsync(bytes);

            response.Close();
        }

        try
        {
            Console.WriteLine($"Method: {request.HttpMethod}");
            Console.WriteLine($"ContentType: {request.ContentType}");
            Console.WriteLine($"ContentLength: {request.ContentLength64}");

            // Получаем части URL
            string[] segments = (request.Url?.AbsolutePath ?? "/")
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Маршрут контроллера должен состоять из двух частей
            if (segments.Length != 2)
            {
                await HandleNextAsync(context);
                return;
            }

            string controllerRoute = "/" + segments[0];
            string methodRoute = segments[1];

            // Ищем контроллер
            if (!_controllers.TryGetValue(
                    controllerRoute,
                    out Type? controllerType))
            {
                await HandleNextAsync(context);
                return;
            }

            // Ищем метод по HTTP-атрибуту
            MethodInfo? targetMethod = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(method =>
                {
                    GetAttribute? get =
                        method.GetCustomAttribute<GetAttribute>();

                    PostAttribute? post =
                        method.GetCustomAttribute<PostAttribute>();

                    return
                        (request.HttpMethod.Equals(
                            "GET",
                            StringComparison.OrdinalIgnoreCase) &&
                         get?.Route.Equals(
                             methodRoute,
                             StringComparison.OrdinalIgnoreCase) == true)

                        ||

                        (request.HttpMethod.Equals(
                            "POST",
                            StringComparison.OrdinalIgnoreCase) &&
                         post?.Route.Equals(
                             methodRoute,
                             StringComparison.OrdinalIgnoreCase) == true);
                });

            if (targetMethod == null)
            {
                await Send(405, "Method Not Allowed");
                return;
            }

            // Получаем параметры запроса
            Dictionary<string, string> values =
                await ReadParametersAsync(request);

            // Преобразуем параметры для метода контроллера
            ParameterInfo[] parameters =
                targetMethod.GetParameters();

            object?[] arguments = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                string parameterName =
                    parameter.Name ?? $"arg{i}";

                if (values.TryGetValue(
                        parameterName,
                        out string? value))
                {
                    arguments[i] = ConvertValue(
                        value,
                        parameter.ParameterType
                    );
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else
                {
                    await Send(
                        400,
                        $"Missing parameter: {parameterName}"
                    );

                    return;
                }
            }

            // Создаём экземпляр контроллера
            object controller = CreateController(controllerType);

            // Вызываем метод через Reflection
            object? result = targetMethod.Invoke(
                controller,
                arguments
            );

            // Обрабатываем асинхронные методы
            if (result is Task task)
            {
                await task;

                Type taskType = task.GetType();

                if (taskType.IsGenericType)
                {
                    result = taskType
                        .GetProperty("Result")?
                        .GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            string responseBody = result?.ToString() ?? "";

            await Send(
                200,
                responseBody,
                DetectContentType(responseBody)
            );
        }
        catch (TargetInvocationException ex)
        {
            Exception actualException =
                ex.InnerException ?? ex;

            Console.WriteLine(actualException);

            if (!responseStarted)
            {
                await Send(500, "Internal server error");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            if (!responseStarted)
            {
                await Send(500, "Internal server error");
            }
        }
    }

    // Создание контроллера с внедрением зависимостей
    private object CreateController(Type controllerType)
    {
        ConstructorInfo[] constructors =
            controllerType.GetConstructors();

        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] parameters =
                constructor.GetParameters();

            object?[] arguments = new object?[parameters.Length];

            bool canCreate = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;

                object? service = _services.FirstOrDefault(
                    s => parameterType.IsInstanceOfType(s)
                );

                if (service == null)
                {
                    canCreate = false;
                    break;
                }

                arguments[i] = service;
            }

            if (canCreate)
            {
                return constructor.Invoke(arguments);
            }
        }

        throw new InvalidOperationException(
            $"Не удалось создать контроллер {controllerType.Name}."
        );
    }

    // Получение Query и POST Body параметров
    private static async Task<Dictionary<string, string>>
        ReadParametersAsync(HttpListenerRequest request)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        // Query parameters
        foreach (string? key in request.QueryString.AllKeys)
        {
            if (key != null)
            {
                result[key] = request.QueryString[key] ?? "";
            }
        }

        // POST form parameters
        if (request.HasEntityBody &&
            request.ContentType?.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            using var reader = new StreamReader(
                request.InputStream,
                request.ContentEncoding
            );

            string body = await reader.ReadToEndAsync();

            var form = HttpUtility.ParseQueryString(body);

            foreach (string? key in form.AllKeys)
            {
                if (key != null)
                {
                    result[key] = form[key] ?? "";
                }
            }
        }

        return result;
    }

    // Преобразование параметров к типам C#
    private static object? ConvertValue(
        string value,
        Type targetType)
    {
        Type actualType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType == typeof(string))
            return value;

        if (actualType.IsEnum)
            return Enum.Parse(actualType, value, true);

        if (actualType == typeof(Guid))
            return Guid.Parse(value);

        return Convert.ChangeType(
            value,
            actualType,
            CultureInfo.InvariantCulture
        );
    }

    // Определение Content-Type ответа
    private static string DetectContentType(string body)
    {
        string value = body.TrimStart();

        if (value.StartsWith("<"))
            return "text/html";

        if (value.StartsWith("{") || value.StartsWith("["))
            return "application/json";

        return "text/plain";
    }
}