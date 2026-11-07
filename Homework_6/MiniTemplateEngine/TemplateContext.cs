namespace MiniTemplateEngine;

/// <summary>
/// Хранит контекст данных, используемых при обработке HTML-шаблона.
/// </summary>
public class TemplateContext
{
    /// <summary>
    /// Основная модель данных.
    /// </summary>
    public object? Model { get; }

    /// <summary>
    /// Локальные переменные текущего контекста.
    /// </summary>
    private readonly Dictionary<string, object?> _variables = new();

    /// <summary>
    /// Создаёт контекст на основе переданной модели.
    /// </summary>
    public TemplateContext(object? model)
    {
        Model = model;
    }

    /// <summary>
    /// Создаёт дочерний контекст с дополнительной переменной.
    /// </summary>
    public TemplateContext CreateChild(string name, object? value)
    {
        var child = new TemplateContext(Model);

        child._variables[name] = value;

        return child;
    }

    /// <summary>
    /// Получает значение переменной или свойства модели.
    /// </summary>
    public object? GetValue(string name)
    {
        if (_variables.TryGetValue(name, out var value))
            return value;

        return ReflectionHelper.GetPropertyValue(Model, name);
    }
}