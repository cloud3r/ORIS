using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace CustomHttpServer.Routing;

public sealed record BindResult(object?[]? Arguments, string? Error)
{
    public bool Success => Error == null;
}

public static class ParameterBinder
{
    public static Dictionary<string, string> Merge(NameValueCollection query, string? formBody)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? key in query.AllKeys)
            if (key != null) result[key] = query[key] ?? "";

        if (!string.IsNullOrEmpty(formBody))
        {
            NameValueCollection form = HttpUtility.ParseQueryString(formBody);
            foreach (string? key in form.AllKeys)
                if (key != null) result[key] = form[key] ?? "";
        }

        return result;
    }

    public static BindResult Bind(MethodInfo method, IReadOnlyDictionary<string, string> values)
    {
        ParameterInfo[] parameters = method.GetParameters();
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
                    return new BindResult(null, $"Invalid parameter: {name}");
                }
            }
            else if (parameter.HasDefaultValue)
            {
                arguments[i] = parameter.DefaultValue;
            }
            else
            {
                return new BindResult(null, $"Missing parameter: {name}");
            }
        }

        return new BindResult(arguments, null);
    }

    public static object? ConvertValue(string value, Type targetType)
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