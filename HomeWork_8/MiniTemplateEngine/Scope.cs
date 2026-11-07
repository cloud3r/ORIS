using System.Collections;
using System.Reflection;

namespace MiniTemplateEngine;

// Область видимости: корневая модель + локальные переменные циклов
internal sealed class Scope
{
    private readonly object? _root;
    private readonly Scope? _parent;
    private readonly string? _name;
    private readonly object? _value;

    private Scope(object? root, Scope? parent, string? name, object? value)
    {
        _root = root;
        _parent = parent;
        _name = name;
        _value = value;
    }

    public static Scope Root(object? model) => new(model, null, null, null);

    public Scope Child(string name, object? value) => new(_root, this, name, value);

    public object? Resolve(string path, int line)
    {
        string[] parts = path.Split('.');
        object? current = null;
        bool found = false;

        // Сначала ищем локальную переменную (item, user...), затем свойство модели
        for (Scope? s = this; s != null; s = s._parent)
        {
            if (s._name == parts[0])
            {
                current = s._value;
                found = true;
                break;
            }
        }

        if (!found)
            current = GetMember(_root, parts[0], line, "модель не передана (null)");

        string walked = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            current = GetMember(current, parts[i], line, $"'{walked}' равно null");
            walked += "." + parts[i];
        }

        return current;
    }

    private static object? GetMember(object? target, string name, int line, string nullReason)
    {
        if (target is null)
            throw new TemplateException($"Строка {line}: невозможно получить '{name}': {nullReason}.");

        if (target is IDictionary dict)
        {
            if (dict.Contains(name)) return dict[name];
            throw new TemplateException($"Строка {line}: ключ '{name}' не найден в словаре.");
        }

        PropertyInfo? property = target.GetType().GetProperty(
            name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null || property.GetIndexParameters().Length > 0)
            throw new TemplateException(
                $"Строка {line}: свойство '{name}' не найдено в типе '{target.GetType().Name}'.");

        return property.GetValue(target);
    }
}