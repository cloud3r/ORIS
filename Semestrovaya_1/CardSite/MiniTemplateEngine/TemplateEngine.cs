using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using MiniTemplateEngine.Interfaces;

namespace MiniTemplateEngine;

public class TemplateEngine : IHtmlTemplateRenderer
{
    private readonly TemplateParser parser = new();

    public string RenderFromString(string htmlTemplate, object data)
    {
        if (string.IsNullOrEmpty(htmlTemplate))
            return string.Empty;

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        string result = ProcessLoops(htmlTemplate, data);

        result = ProcessConditions(result, data);

        result = ReplaceVariables(result, data);

        return result;
    }

    public string RenderFromFile(string filePath, object data)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Template file was not found.", filePath);
        }

        string htmlTemplate = File.ReadAllText(filePath);

        return RenderFromString(htmlTemplate, data);
    }

    private string ProcessLoops(string template, object data)
    {
        const string pattern =
            @"{{foreach\s+([\w.]+)}}([\s\S]*?){{/foreach}}";

        return Regex.Replace(template, pattern, match =>
        {
            string collectionName = match.Groups[1].Value;
            string loopContent = match.Groups[2].Value;

            object? collection = parser.GetObject(data, collectionName);

            if (collection is not IEnumerable items)
                return string.Empty;

            var result = new StringBuilder();

            foreach (object? item in items)
            {
                if (item == null)
                    continue;

                result.Append(RenderFromString(loopContent, item));
            }

            return result.ToString();
        });
    }

    private string ReplaceVariables(string template, object data)
    {
        return Regex.Replace(
            template,
            @"{{\s*(\w+(?:\.\w+)*)\s*}}",
            match =>
            {
                string propertyPath = match.Groups[1].Value;

                string value = parser.GetValue(data, propertyPath);

                return System.Net.WebUtility.HtmlEncode(value);
            });
    }

    private bool IsTruthy(object? value)
    {
        if (value == null)
            return false;

        if (value is bool boolean)
            return boolean;

        if (value is string text)
            return !string.IsNullOrWhiteSpace(text);

        if (value is int number)
            return number != 0;

        if (value is ICollection collection)
            return collection.Count > 0;

        return true;
    }

    private string ProcessConditions(string template, object data)
    {
        const string pattern =
            @"{{if\s+([\w.]+)}}((?:(?!{{if\s+)[\s\S])*?){{/if}}";

        string previous;

        do
        {
            previous = template;

            template = Regex.Replace(template, pattern, match =>
            {
                string propertyName = match.Groups[1].Value;
                string content = match.Groups[2].Value;

                object? value = parser.GetObject(data, propertyName);

                return IsTruthy(value) ? content : string.Empty;
            });

        } while (template != previous);

        return template;
    }
}