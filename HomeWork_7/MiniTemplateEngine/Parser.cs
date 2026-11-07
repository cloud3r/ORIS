using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

internal sealed class Parser
{
    private static readonly Regex PathRegex =
        new(@"^[A-Za-z_]\w*(\.[A-Za-z_]\w*)*$", RegexOptions.Compiled);

    private static readonly Regex ForeachRegex =
        new(@"^var\s+([A-Za-z_]\w*)\s+in\s+(.+)$", RegexOptions.Compiled);

    private readonly List<Token> _tokens;
    private int _i;

    public Parser(List<Token> tokens) => _tokens = tokens;

    public List<Node> Parse()
    {
        List<Node> nodes = ParseNodes();

        // Сюда можно попасть, только если встретился лишний закрывающий токен
        if (_i < _tokens.Count)
            throw Unexpected(_tokens[_i]);

        return nodes;
    }

    // Разбирает токены до одного из stop-токенов (не съедая его) или до конца
    private List<Node> ParseNodes(params TokenType[] stop)
    {
        var nodes = new List<Node>();

        while (_i < _tokens.Count)
        {
            Token token = _tokens[_i];

            if (stop.Contains(token.Type))
                return nodes;

            switch (token.Type)
            {
                case TokenType.Text:
                    nodes.Add(new TextNode(token.Value));
                    _i++;
                    break;

                case TokenType.Variable:
                    nodes.Add(new VariableNode(
                        ValidatePath(token.Value, token.Line, "выражение ${...}"), token.Line));
                    _i++;
                    break;

                case TokenType.If:
                    nodes.Add(ParseIf());
                    break;

                case TokenType.Foreach:
                    nodes.Add(ParseForeach());
                    break;

                default:
                    throw Unexpected(token);
            }
        }

        return nodes;
    }

    private Node ParseIf()
    {
        Token start = _tokens[_i++];

        string condition = start.Value;
        bool negate = condition.StartsWith('!');
        if (negate) condition = condition[1..].Trim();

        string path = ValidatePath(condition, start.Line, "условие $if");

        List<Node> thenBranch = ParseNodes(TokenType.Else, TokenType.EndIf);
        List<Node> elseBranch = new();

        EnsureNotEnd(start, "$endif", $"$if({start.Value})");

        if (_tokens[_i].Type == TokenType.Else)
        {
            _i++;
            elseBranch = ParseNodes(TokenType.EndIf);
            EnsureNotEnd(start, "$endif", $"$if({start.Value})");
        }

        _i++; // $endif
        return new IfNode(path, negate, start.Line, thenBranch, elseBranch);
    }

    private Node ParseForeach()
    {
        Token start = _tokens[_i++];

        Match match = ForeachRegex.Match(start.Value);
        if (!match.Success)
            throw new TemplateException(
                $"Строка {start.Line}: некорректная конструкция $foreach({start.Value}). " +
                "Ожидается $foreach(var item in Items).");

        string variable = match.Groups[1].Value;
        string path = ValidatePath(match.Groups[2].Value.Trim(), start.Line, "коллекция в $foreach");

        List<Node> body = ParseNodes(TokenType.EndFor);
        EnsureNotEnd(start, "$endfor", $"$foreach({start.Value})");

        _i++; // $endfor
        return new ForeachNode(variable, path, start.Line, body);
    }

    private void EnsureNotEnd(Token start, string expected, string construct)
    {
        if (_i >= _tokens.Count)
            throw new TemplateException(
                $"Строка {start.Line}: не найден {expected} для {construct}.");
    }

    private static string ValidatePath(string path, int line, string what)
    {
        if (!PathRegex.IsMatch(path))
            throw new TemplateException(
                $"Строка {line}: некорректное {what}: '{path}'. " +
                "Допустимы имена и вложенные свойства, например User.Name.");
        return path;
    }

    private static TemplateException Unexpected(Token token)
    {
        string name = token.Type switch
        {
            TokenType.Else => "$else без соответствующего $if (или $else указан дважды)",
            TokenType.EndIf => "$endif без соответствующего $if",
            TokenType.EndFor => "$endfor без соответствующего $foreach (возможно, внутри не закрыт $if)",
            _ => token.Type.ToString()
        };

        return new TemplateException($"Строка {token.Line}: неожиданная конструкция: {name}.");
    }
}