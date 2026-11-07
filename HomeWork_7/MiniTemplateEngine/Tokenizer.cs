using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

internal enum TokenType { Text, Variable, If, Else, EndIf, Foreach, EndFor }

internal sealed record Token(TokenType Type, string Value, int Line);

internal static class Tokenizer
{
    // ${expr}  |  $if(...)  $foreach(...)  $else  $endif  $endfor
    // Для строк, где только директива, съедаются отступ и перевод строки,
    // чтобы в HTML не оставалось пустых строк.
    private static readonly Regex Pattern = new(
        @"\$\{(?<expr>[^}]*)\}" +
        @"|(?:^[ \t]*)?\$(?:(?<kw>if|foreach)\b[ \t]*(?<args>\([^)]*\))?|(?<kw>else|endif|endfor)\b)(?:[ \t]*\r?\n)?",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public static List<Token> Tokenize(string template)
    {
        var tokens = new List<Token>();
        int pos = 0;

        foreach (Match m in Pattern.Matches(template))
        {
            if (m.Index > pos)
                tokens.Add(new Token(TokenType.Text, template[pos..m.Index], LineAt(template, pos)));

            int line = LineAt(template, m.Index);

            if (m.Groups["expr"].Success)
            {
                tokens.Add(new Token(TokenType.Variable, m.Groups["expr"].Value.Trim(), line));
            }
            else
            {
                switch (m.Groups["kw"].Value)
                {
                    case "if":
                        tokens.Add(new Token(TokenType.If, Args(m, "$if", "$if(IsActive)", line), line));
                        break;
                    case "foreach":
                        tokens.Add(new Token(TokenType.Foreach,
                            Args(m, "$foreach", "$foreach(var item in Items)", line), line));
                        break;
                    case "else":
                        tokens.Add(new Token(TokenType.Else, "", line));
                        break;
                    case "endif":
                        tokens.Add(new Token(TokenType.EndIf, "", line));
                        break;
                    case "endfor":
                        tokens.Add(new Token(TokenType.EndFor, "", line));
                        break;
                }
            }

            pos = m.Index + m.Length;
        }

        if (pos < template.Length)
            tokens.Add(new Token(TokenType.Text, template[pos..], LineAt(template, pos)));

        return tokens;
    }

    private static string Args(Match m, string name, string example, int line)
    {
        if (!m.Groups["args"].Success)
            throw new TemplateException(
                $"Строка {line}: после {name} ожидаются скобки с выражением, например {example}.");

        string value = m.Groups["args"].Value;
        return value[1..^1].Trim();
    }

    private static int LineAt(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
            if (text[i] == '\n') line++;
        return line;
    }
}