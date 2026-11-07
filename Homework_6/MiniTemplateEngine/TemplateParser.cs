using System.Collections;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// Выполняет разбор и обработку HTML-шаблонов.
/// </summary>
public class TemplateParser
{
    // Регулярное выражение для поиска команд шаблонизатора.
    private static readonly Regex DirectiveRegex = new(
        @"\$(if\([^)]+\)|foreach\([^)]+\)|else|endif|endforeach)\b?"
    );

    // Регулярное выражение для поиска переменных в шаблоне.
    private static readonly Regex VariableRegex = new(
        @"\$([a-zA-Z_][a-zA-Z0-9_.]*)"
    );

    /// <summary>
    /// Обрабатывает HTML-шаблон с использованием переданной модели данных.
    /// </summary>
    /// <param name="template">Исходный HTML-шаблон.</param>
    /// <param name="model">Объект с данными для шаблона.</param>
    /// <returns>Обработанный HTML.</returns>
    public string Parse(string template, object model)
    {
        // Проверяем, что шаблон не равен null.
        ArgumentNullException.ThrowIfNull(template);

        // Проверяем, что модель данных не равна null.
        ArgumentNullException.ThrowIfNull(model);

        // Создаём контекст с основной моделью данных.
        Dictionary<string, object?> context = new()
        {
            ["Model"] = model
        };

        // Получаем все публичные свойства основной модели.
        foreach (var property in model.GetType().GetProperties())
        {
            // Добавляем название и значение каждого свойства в контекст.
            context[property.Name] = property.GetValue(model);
        }

        // Запускаем рекурсивную обработку шаблона.
        var result = ParseSection(template, 0, context, out _);

        // Возвращаем готовый HTML.
        return result;
    }

    /// <summary>
    /// Рекурсивно обрабатывает часть шаблона.
    /// </summary>
    /// <param name="template">Исходный шаблон.</param>
    /// <param name="position">Текущая позиция в шаблоне.</param>
    /// <param name="context">Контекст данных.</param>
    /// <param name="stopDirective">Команда, на которой закончилась обработка.</param>
    /// <returns>Обработанная часть HTML.</returns>
    private string ParseSection(
        string template,
        int position,
        Dictionary<string, object?> context,
        out string? stopDirective)
    {
        // Создаём объект для формирования результата.
        var result = new System.Text.StringBuilder();

        // Инициализируем команду остановки значением null.
        stopDirective = null;

        // Продолжаем обработку, пока не достигнем конца шаблона.
        while (position < template.Length)
        {
            // Ищем следующую команду шаблонизатора.
            Match match = DirectiveRegex.Match(template, position);

            // Если команд больше нет, обрабатываем оставшуюся часть шаблона.
            if (!match.Success)
            {
                // Добавляем оставшийся HTML с подстановкой переменных.
                result.Append(RenderVariables(template[position..], context));

                // Завершаем обработку текущей секции.
                return result.ToString();
            }

            // Добавляем HTML перед найденной командой.
            result.Append(RenderVariables(
                template[position..match.Index],
                context
            ));

            // Получаем текст найденной команды без символа $.
            string directive = match.Value[1..];

            // Перемещаем позицию за найденную команду.
            position = match.Index + match.Length;

            // Проверяем, является ли команда концом текущей секции.
            if (directive == "else" ||
                directive == "endif" ||
                directive == "endforeach")
            {
                // Запоминаем найденную команду.
                stopDirective = directive;

                // Возвращаем результат текущей секции.
                return result.ToString();
            }

            // Проверяем, является ли команда условием.
            if (directive.StartsWith("if("))
            {
                // Получаем выражение внутри условия.
                string condition = directive[3..^1].Trim();

                // Обрабатываем первую ветку условия.
                string trueBranch = ParseSection(
                    template,
                    position,
                    context,
                    out string? branchStop
                );

                // Инициализируем вторую ветку условия.
                string falseBranch = "";

                // Если встретили else, обрабатываем альтернативную ветку.
                if (branchStop == "else")
                {
                    // Продолжаем обработку до endif.
                    falseBranch = ParseSection(
                        template,
                        position,
                        context,
                        out branchStop
                    );
                }

                // Проверяем, что условие было корректно закрыто.
                if (branchStop != "endif")
                    throw new FormatException("Missing $endif.");

                // Проверяем значение условия.
                bool conditionResult = EvaluateCondition(condition, context);

                // Добавляем только подходящую ветку.
                result.Append(conditionResult ? trueBranch : falseBranch);

                // Обновляем позицию после закрывающей команды.
                position = FindNextPosition(template, position, "endif");
            }
            // Проверяем, является ли команда циклом.
            else if (directive.StartsWith("foreach("))
            {
                // Получаем содержимое команды foreach.
                string expression = directive[8..^1].Trim();

                // Разделяем имя переменной и название коллекции.
                Match foreachMatch = Regex.Match(
                    expression,
                    @"^([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s+(.+)$"
                );

                // Проверяем корректность выражения цикла.
                if (!foreachMatch.Success)
                    throw new FormatException("Invalid $foreach syntax.");

                // Получаем имя переменной цикла.
                string variableName = foreachMatch.Groups[1].Value;

                // Получаем путь к коллекции.
                string collectionName = foreachMatch.Groups[2].Value.Trim();

                // Обрабатываем содержимое цикла до endforeach.
                string loopBody = ParseSection(
                    template,
                    position,
                    context,
                    out string? loopStop
                );

                // Проверяем наличие закрывающей команды.
                if (loopStop != "endforeach")
                    throw new FormatException("Missing $endforeach.");

                // Получаем коллекцию через Reflection.
                object? collection = GetValue(collectionName, context);

                // Проверяем, что полученное значение является коллекцией.
                if (collection is IEnumerable items && collection is not string)
                {
                    // Перебираем все элементы коллекции.
                    foreach (object? item in items)
                    {
                        // Сохраняем предыдущее значение переменной цикла.
                        context.TryGetValue(variableName, out object? previousValue);

                        // Запоминаем, существовала ли переменная до цикла.
                        bool existed = context.ContainsKey(variableName);

                        // Добавляем текущий элемент в контекст.
                        context[variableName] = item;

                        // Обрабатываем тело цикла с текущим элементом.
                        result.Append(
                            Parse(loopBody, context)
                        );

                        // Восстанавливаем предыдущее значение переменной.
                        if (existed)
                            context[variableName] = previousValue;
                        else
                            context.Remove(variableName);
                    }
                }

                // Обновляем позицию после закрывающей команды.
                position = FindNextPosition(template, position, "endforeach");
            }
        }

        // Возвращаем сформированный HTML.
        return result.ToString();
    }

    /// <summary>
    /// Выполняет подстановку переменных в HTML.
    /// </summary>
    private string RenderVariables(
        string template,
        Dictionary<string, object?> context)
    {
        // Ищем все переменные и заменяем их значениями из контекста.
        return VariableRegex.Replace(template, match =>
        {
            // Получаем название переменной.
            string variableName = match.Groups[1].Value;

            // Получаем значение переменной.
            object? value = GetValue(variableName, context);

            // Возвращаем значение или пустую строку.
            return value?.ToString() ?? "";
        });
    }

    /// <summary>
    /// Получает значение переменной из контекста.
    /// </summary>
    private object? GetValue(
        string name,
        Dictionary<string, object?> context)
    {
        // Разделяем путь к переменной на отдельные части.
        string[] parts = name.Split('.');

        // Ищем первую часть пути в контексте.
        if (!context.TryGetValue(parts[0], out object? value))
            return null;

        // Если путь состоит только из одного имени, возвращаем значение.
        if (parts.Length == 1)
            return value;

        // Формируем оставшуюся часть пути.
        string remainingPath = string.Join(".", parts.Skip(1));

        // Получаем вложенное свойство через Reflection.
        return ReflectionHelper.GetPropertyValue(value, remainingPath);
    }

    /// <summary>
    /// Проверяет значение логического выражения.
    /// </summary>
    private bool EvaluateCondition(
        string condition,
        Dictionary<string, object?> context)
    {
        // Получаем значение свойства из контекста.
        object? value = GetValue(condition, context);

        // Если значение является bool, возвращаем его.
        if (value is bool boolean)
            return boolean;

        // Если значение отсутствует, условие считается ложным.
        if (value == null)
            return false;

        // Для остальных типов проверяем, что значение не равно значению по умолчанию.
        return !value.Equals(Activator.CreateInstance(
            Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType()
        ));
    }

    /// <summary>
    /// Находит позицию после указанной команды.
    /// </summary>
    private int FindNextPosition(
        string template,
        int position,
        string directive)
    {
        // Ищем закрывающую команду в оставшейся части шаблона.
        Match match = Regex.Match(
            template[position..],
            @"\$" + Regex.Escape(directive) + @"\b"
        );

        // Если команда не найдена, выбрасываем исключение.
        if (!match.Success)
            throw new FormatException($"Missing ${directive}.");

        // Возвращаем позицию после найденной команды.
        return position + match.Index + match.Length;
    }
}