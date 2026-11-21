namespace MiniTemplateEngine.Interfaces;

public interface IHtmlTemplateRenderer
{
    string RenderFromString(string htmlTemplate, object data);

    string RenderFromFile(string filePath, object data);
}