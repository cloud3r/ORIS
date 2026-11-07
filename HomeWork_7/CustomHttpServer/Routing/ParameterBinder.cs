using System.Globalization;
using System.Reflection;

namespace CustomHttpServer.Routing;

public static class ParameterBinder
{
    public static bool TryBind(
        ParameterInfo[] parameters,
        IReadOnlyDictionary<string, string> values,
        out object?[] arguments,
        out string error)
    {
        arguments = new object?[parameters.Length];
        error = "";

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            string name = parameter.Name ?? $"arg{i}";

            if (!values.TryGetValue(name, out string? raw))
            {
                if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                    continue;
                }

                error = $"Отсутствует обязательный параметр '{name}'.";
                return false;
            }

            try
            {
                arguments[i] = ConvertValue(
                    raw,
                    parameter.ParameterType
                );
            }
            catch
            {
                error = $"Некорректное значение параметра '{name}': '{raw}'.";
                return false;
            }
        }

        return true;
    }

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

        if (actualType == typeof(DateTime))
            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture
            );

        return Convert.ChangeType(
            value,
            actualType,
            CultureInfo.InvariantCulture
        );
    }
}