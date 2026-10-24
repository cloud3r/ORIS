using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CustomHttpServer.Interfaces;

namespace CustomHttpServer.Services;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex VariableRegex = new Regex(@"\$\{(?<expr>[^}]*)\}");

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        if (htmlTemplate == null) throw new ArgumentNullException(nameof(htmlTemplate));
        
        return VariableRegex.Replace(htmlTemplate, match =>
        {
            string expr = match.Groups["expr"].Value.Trim();
            object value = GetValue(expr, dataModel);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        });
    }

    private static object GetValue(string expr, object model)
    {
        string[] parts = expr.Split('.');
        
        if (parts.Length == 1) return model;

        object current = model;
        
        for (int i = 1; i < parts.Length; i++)
        {
            if (current == null) return null;

            string propertyName = parts[i].Trim();

            PropertyInfo prop = current.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null)
                throw new InvalidOperationException(
                    $"У типа {current.GetType().Name} нет свойства «{propertyName}».");

            current = prop.GetValue(current);
        }

        return current;
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(filePath));
        
        string template = File.ReadAllText(filePath, Encoding.UTF8);

        return RenderFromString(template, dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
            throw new ArgumentException("Путь для сохранения не задан.", nameof(outputFilePath));
        
        string result = RenderFromFile(inputFilePath, dataModel);
        
        string directory = Path.GetDirectoryName(Path.GetFullPath(outputFilePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        File.WriteAllText(outputFilePath, result, new UTF8Encoding(false));

        return result;
    }
}