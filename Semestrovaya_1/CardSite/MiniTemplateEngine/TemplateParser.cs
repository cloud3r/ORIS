using System.Reflection;

namespace MiniTemplateEngine;

public class TemplateParser
{
    public object? GetObject(object data, string propertyPath)
    {
        object? currentObject = data;

        string[] properties = propertyPath.Split('.');

        foreach (string propertyName in properties)
        {
            if (currentObject == null)
                return null;

            PropertyInfo? property = currentObject.GetType()
                .GetProperty(propertyName);

            if (property == null)
                return null;

            currentObject = property.GetValue(currentObject);
        }

        return currentObject;
    }

    public string GetValue(object data, string propertyPath)
    {
        object? value = GetObject(data, propertyPath);

        return value?.ToString() ?? string.Empty;
    }
}