using System.Reflection;

namespace MiniTemplateEngine;

/// <summary>
/// Предоставляет методы для получения значений свойств объектов через Reflection.
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// Получает значение свойства объекта по его имени.
    /// Поддерживает обращение к вложенным свойствам через точку.
    /// </summary>
    /// <param name="model">Объект, из которого нужно получить значение.</param>
    /// <param name="propertyPath">Путь к свойству объекта.</param>
    /// <returns>Значение найденного свойства или null.</returns>
    public static object? GetPropertyValue(object? model, string propertyPath)
    {
        // Проверяем, что объект и путь к свойству не равны null.
        if (model == null || string.IsNullOrWhiteSpace(propertyPath))
            return null;

        // Разделяем путь к свойству по точкам для получения вложенных свойств.
        string[] properties = propertyPath.Split('.');

        // Начинаем поиск с переданного объекта.
        object? current = model;

        // Последовательно обходим все свойства из пути.
        foreach (string propertyName in properties)
        {
            // Если текущий объект равен null, дальнейший поиск невозможен.
            if (current == null)
                return null;

            // Получаем тип текущего объекта.
            Type type = current.GetType();

            // Ищем публичное свойство с указанным именем без учёта регистра.
            PropertyInfo? property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );

            // Если свойство не найдено, возвращаем null.
            if (property == null)
                return null;

            // Получаем значение найденного свойства.
            current = property.GetValue(current);
        }

        // Возвращаем значение последнего найденного свойства.
        return current;
    }
}