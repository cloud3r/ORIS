using System.Text;

namespace MiniTemplateEngine;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string template, object? model)
    {
        ArgumentNullException.ThrowIfNull(template);

        var tokens = Tokenizer.Tokenize(template);
        var nodes = new Parser(tokens).Parse();

        var sb = new StringBuilder();
        Scope scope = Scope.Root(model);

        foreach (Node node in nodes)
            node.Render(sb, scope);

        return sb.ToString();
    }

    public string RenderFromFile(string templatePath, object? model)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Файл шаблона не найден: {templatePath}");

        return RenderFromString(File.ReadAllText(templatePath, Encoding.UTF8), model);
    }

    public void RenderToFile(string templatePath, string outputPath, object? model)
    {
        string html = RenderFromFile(templatePath, model);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, html, new UTF8Encoding(false));
    }
}