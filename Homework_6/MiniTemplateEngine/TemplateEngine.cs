using MiniTemplateEngine.Interfaces;

namespace MiniTemplateEngine;

/// <summary>
/// Реализует обработку HTML-шаблонов
/// </summary>
public class TemplateEngine : IHtmlTemplateRenderer
{
    /// <summary>
    /// Обрабатывает HTML-шаблон, переданный в виде строки
    /// </summary>
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        // Создаём парсер для обработки HTML-шаблона.
        var parser = new TemplateParser();

        // Передаём шаблон и модель данных парсеру.
        return parser.Parse(htmlTemplate, dataModel);
    }

    /// <summary>
    /// Загружает HTML-шаблон из файла и обрабатывает его.
    /// </summary>
    public string RenderFromFile(string filePath, object dataModel)
    {
        // Проверяем существование исходного файла.
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Template file not found.", filePath);

        // Считываем содержимое HTML-файла.
        string htmlTemplate = File.ReadAllText(filePath);

        // Передаём прочитанный HTML на обработку.
        return RenderFromString(htmlTemplate, dataModel);
    }

    /// <summary>
    /// Обрабатывает шаблон и сохраняет результат в файл.
    /// </summary>
    public string RenderToFile(
        string inputFilePath,
        string outputFilePath,
        object dataModel)
    {
        // Обрабатываем исходный HTML-файл.
        string result = RenderFromFile(inputFilePath, dataModel);

        // Получаем директорию выходного файла.
        string? directory = Path.GetDirectoryName(outputFilePath);

        // Создаём директорию, если она указана и ещё не существует.
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Записываем обработанный HTML в выходной файл.
        File.WriteAllText(outputFilePath, result);

        // Возвращаем обработанный HTML.
        return result;
    }
}