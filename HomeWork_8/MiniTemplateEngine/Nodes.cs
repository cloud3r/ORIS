using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;

namespace MiniTemplateEngine;

internal abstract class Node
{
    public abstract void Render(StringBuilder sb, Scope scope);

    protected static void RenderAll(List<Node> nodes, StringBuilder sb, Scope scope)
    {
        foreach (Node node in nodes)
            node.Render(sb, scope);
    }
}

internal sealed class TextNode(string text) : Node
{
    public override void Render(StringBuilder sb, Scope scope) => sb.Append(text);
}

internal sealed class VariableNode(string path, int line) : Node
{
    public override void Render(StringBuilder sb, Scope scope)
    {
        object? value = scope.Resolve(path, line);
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

        // HTML-кодирование: пользовательские данные не станут разметкой или скриптом
        sb.Append(WebUtility.HtmlEncode(text));
    }
}

internal sealed class IfNode(
    string path, bool negate, int line,
    List<Node> thenBranch, List<Node> elseBranch) : Node
{
    public override void Render(StringBuilder sb, Scope scope)
    {
        object? value = scope.Resolve(path, line);

        bool condition = value switch
        {
            bool b => b,
            null => false,
            _ => throw new TemplateException(
                $"Строка {line}: условие '{path}' должно быть логическим (bool), " +
                $"а получено значение типа '{value.GetType().Name}'.")
        };

        if (negate) condition = !condition;

        RenderAll(condition ? thenBranch : elseBranch, sb, scope);
    }
}

internal sealed class ForeachNode(
    string variable, string path, int line, List<Node> body) : Node
{
    public override void Render(StringBuilder sb, Scope scope)
    {
        object? value = scope.Resolve(path, line);

        if (value is null)
            throw new TemplateException($"Строка {line}: коллекция '{path}' равна null.");

        if (value is string || value is not IEnumerable items)
            throw new TemplateException(
                $"Строка {line}: '{path}' не является коллекцией (тип '{value.GetType().Name}').");

        // Пустая коллекция: тело просто не выполняется
        foreach (object? item in items)
            RenderAll(body, sb, scope.Child(variable, item));
    }
}