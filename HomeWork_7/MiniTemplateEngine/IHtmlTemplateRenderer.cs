namespace MiniTemplateEngine;

public interface IHtmlTemplateRenderer
{
    string RenderFromString(string template, object? model);
    string RenderFromFile(string templatePath, object? model);
    void RenderToFile(string templatePath, string outputPath, object? model);
}