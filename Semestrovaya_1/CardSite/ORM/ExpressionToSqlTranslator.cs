using System.Collections;
using System.Linq.Expressions;
using Npgsql;
using CardSite.ORM.Metadata;

namespace CardSite.ORM;

public class ExpressionToSqlTranslator<T> where T : class
{
    private readonly EntityMetadata metadata;
    private readonly EntityMapper mapper;
    private readonly List<NpgsqlParameter> parameters = new();

    public List<NavigationJoinInfo> RequiredNavigations { get; } = new();

    public ExpressionToSqlTranslator(
        EntityMetadata metadata,
        EntityMapper? mapper = null)
    {
        this.metadata = metadata;
        this.mapper = mapper ?? new EntityMapper();
    }

    public (string Sql, List<NpgsqlParameter> Parameters) Translate(
        Expression<Func<T, bool>> expression,
        string? tableAlias = null)
    {
        parameters.Clear();
        RequiredNavigations.Clear();

        string? effectiveAlias = tableAlias;

        if (effectiveAlias == null &&
            ContainsNavigationReference(expression.Body, expression.Parameters[0]))
        {
            effectiveAlias = "t0";
        }

        string sql = TranslateExpression(
            expression.Body,
            expression.Parameters[0],
            effectiveAlias
        );

        return (sql, parameters.ToList());
    }

    private string TranslateExpression(
        Expression expression,
        ParameterExpression parameter,
        string? tableAlias)
    {
        if (expression is BinaryExpression binaryExpression)
        {
            string left = TranslateExpression(
                binaryExpression.Left,
                parameter,
                tableAlias);

            string right = TranslateExpression(
                binaryExpression.Right,
                parameter,
                tableAlias);

            if (binaryExpression.NodeType == ExpressionType.Equal)
            {
                if (right == "NULL")
                    return $"({left} IS NULL)";

                if (left == "NULL")
                    return $"({right} IS NULL)";
            }

            if (binaryExpression.NodeType == ExpressionType.NotEqual)
            {
                if (right == "NULL")
                    return $"({left} IS NOT NULL)";

                if (left == "NULL")
                    return $"({right} IS NOT NULL)";
            }

            string operation = binaryExpression.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",

                _ => throw new NotSupportedException(
                    $"Operator {binaryExpression.NodeType} is not supported.")
            };

            return $"({left} {operation} {right})";
        }

        if (expression is MemberExpression memberExpression)
        {
            var parameterFinder = new ParameterFinder();

            parameterFinder.Visit(memberExpression);

            if (parameterFinder.Found)
            {
                return TranslateMemberExpression(
                    memberExpression,
                    parameter,
                    tableAlias);
            }

            object? value = EvaluateExpression(memberExpression);

            if (value == null)
                return "NULL";

            string parameterName = AddParameter(value);

            return $"@{parameterName}";
        }

        if (expression is MethodCallExpression collectionCall &&
            IsCollectionContains(collectionCall))
        {
            return TranslateCollectionContains(
                collectionCall,
                parameter,
                tableAlias);
        }

        if (expression is MethodCallExpression stringCall &&
            stringCall.Object?.Type == typeof(string))
        {
            string column = TranslateExpression(
                stringCall.Object,
                parameter,
                tableAlias);

            object? value = EvaluateExpression(
                stringCall.Arguments[0]);

            string? pattern = stringCall.Method.Name switch
            {
                "Contains" => $"%{value}%",
                "StartsWith" => $"{value}%",
                "EndsWith" => $"%{value}",

                _ => null
            };

            if (pattern == null)
            {
                throw new NotSupportedException(
                    $"Method {stringCall.Method.Name} is not supported.");
            }

            string parameterName = AddParameter(pattern);

            return $"{column} LIKE @{parameterName}";
        }

        if (expression is ConstantExpression ||
            expression is MemberExpression ||
            expression is UnaryExpression)
        {
            object? value = EvaluateExpression(expression);

            if (value == null)
                return "NULL";

            string parameterName = AddParameter(value);

            return $"@{parameterName}";
        }

        throw new NotSupportedException(
            $"Expression type {expression.NodeType} is not supported.");
    }

    private string TranslateMemberExpression(
        MemberExpression expression,
        ParameterExpression parameter,
        string? tableAlias)
    {
        if (expression.Expression == parameter)
        {
            var property = metadata.Properties.FirstOrDefault(
                p => p.Property.Name == expression.Member.Name);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Property '{expression.Member.Name}' not found.");
            }

            string column = QuoteIdentifier(property.ColumnName);

            return string.IsNullOrEmpty(tableAlias)
                ? column
                : $"{tableAlias}.{column}";
        }

        if (expression.Expression is MemberExpression navigationExpression &&
            navigationExpression.Expression == parameter)
        {
            var navigation = metadata.Navigations.FirstOrDefault(
                n => n.Property.Name == navigationExpression.Member.Name);

            if (navigation == null)
            {
                throw new NotSupportedException(
                    $"Navigation '{navigationExpression.Member.Name}' is not supported.");
            }

            if (string.IsNullOrEmpty(tableAlias))
            {
                throw new InvalidOperationException(
                    "Root table alias is required for navigation filtering.");
            }

            EntityMetadata relatedMetadata =
                mapper.GetMetadata(navigation.RelatedEntityType);

            var relatedProperty = relatedMetadata.Properties.FirstOrDefault(
                p => p.Property.Name == expression.Member.Name);

            if (relatedProperty == null)
            {
                throw new InvalidOperationException(
                    $"Property '{expression.Member.Name}' not found in " +
                    $"'{navigation.RelatedEntityType.Name}'.");
            }

            var foreignKeyProperty = metadata.Properties.FirstOrDefault(
                p => p.Property.Name == navigation.ForeignKeyProperty);

            if (foreignKeyProperty == null)
            {
                throw new InvalidOperationException(
                    $"Foreign key property '{navigation.ForeignKeyProperty}' not found.");
            }

            var join = RequiredNavigations.FirstOrDefault(
                n => n.Navigation.Property.Name == navigation.Property.Name);

            if (join == null)
            {
                string alias = $"t{RequiredNavigations.Count + 1}";

                join = new NavigationJoinInfo(
                    navigation,
                    relatedMetadata,
                    alias,
                    foreignKeyProperty.ColumnName,
                    relatedMetadata.PrimaryKey.ColumnName
                );

                RequiredNavigations.Add(join);
            }

            string column = QuoteIdentifier(relatedProperty.ColumnName);

            return $"{join.Alias}.{column}";
        }

        throw new NotSupportedException(
            $"Member expression '{expression}' is not supported.");
    }

    private bool ContainsNavigationReference(
        Expression expression,
        ParameterExpression parameter)
    {
        var visitor = new NavigationReferenceFinder(metadata, parameter);

        visitor.Visit(expression);

        return visitor.Found;
    }

    private sealed class NavigationReferenceFinder : ExpressionVisitor
    {
        private readonly EntityMetadata metadata;
        private readonly ParameterExpression parameter;

        public bool Found { get; private set; }

        public NavigationReferenceFinder(
            EntityMetadata metadata,
            ParameterExpression parameter)
        {
            this.metadata = metadata;
            this.parameter = parameter;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is MemberExpression navigationExpression &&
                navigationExpression.Expression == parameter)
            {
                if (metadata.Navigations.Any(
                        n => n.Property.Name == navigationExpression.Member.Name))
                {
                    Found = true;
                }
            }

            return base.VisitMember(node);
        }
    }

    private static bool IsCollectionContains(MethodCallExpression expression)
    {
        if (expression.Method.Name != "Contains")
            return false;

        if (expression.Object?.Type == typeof(string))
            return false;

        return true;
    }

    private string TranslateCollectionContains(
        MethodCallExpression expression,
        ParameterExpression parameter,
        string? tableAlias)
    {
        Expression collectionExpression;
        Expression itemExpression;

        if (expression.Object != null)
        {
            collectionExpression = expression.Object;
            itemExpression = expression.Arguments[0];
        }
        else
        {
            collectionExpression = expression.Arguments[0];
            itemExpression = expression.Arguments[1];
        }

        collectionExpression = UnwrapCollectionExpression(
            collectionExpression);

        object? collectionValue = EvaluateExpression(
            collectionExpression);

        if (collectionValue is not IEnumerable collection ||
            collectionValue is string)
        {
            throw new InvalidOperationException(
                "Contains() requires a valid collection.");
        }

        string column = TranslateExpression(
            itemExpression,
            parameter,
            tableAlias);

        var values = collection.Cast<object?>().ToList();

        if (values.Count == 0)
            return "FALSE";

        var nonNullValues = values
            .Where(value => value != null)
            .ToList();

        bool containsNull = values.Any(value => value == null);

        var conditions = new List<string>();

        if (nonNullValues.Count > 0)
        {
            var parameterNames = new List<string>();

            foreach (var value in nonNullValues)
            {
                parameterNames.Add(AddParameter(value!));
            }

            conditions.Add(
                $"{column} IN ({string.Join(", ", parameterNames.Select(name => $"@{name}"))})");
        }

        if (containsNull)
        {
            conditions.Add($"{column} IS NULL");
        }

        return conditions.Count == 1
            ? $"({conditions[0]})"
            : $"({string.Join(" OR ", conditions)})";
    }

    private static Expression UnwrapCollectionExpression(
        Expression expression)
    {
        while (true)
        {
            if (expression is UnaryExpression unary &&
                (unary.NodeType == ExpressionType.Convert ||
                 unary.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unary.Operand;
                continue;
            }

            if (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == "op_Implicit" &&
                    methodCall.Type.IsByRefLike &&
                    methodCall.Arguments.Count == 1)
                {
                    expression = methodCall.Arguments[0];
                    continue;
                }

                if (methodCall.Method.Name == "AsSpan" &&
                    methodCall.Arguments.Count >= 1)
                {
                    expression = methodCall.Arguments[0];
                    continue;
                }
            }

            return expression;
        }
    }

    private static object? EvaluateExpression(Expression expression)
    {
        var parameterVisitor = new ParameterFinder();

        parameterVisitor.Visit(expression);

        if (parameterVisitor.Found)
        {
            throw new NotSupportedException(
                "Cannot evaluate an expression that depends on the entity parameter.");
        }

        var convertedExpression = Expression.Convert(
            expression,
            typeof(object));

        return Expression
            .Lambda<Func<object?>>(convertedExpression)
            .Compile()();
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return node;
        }
    }

    private string AddParameter(object value)
    {
        if (value is Enum enumValue)
        {
            value = Convert.ChangeType(
                enumValue,
                Enum.GetUnderlyingType(enumValue.GetType()))!;
        }

        string parameterName = $"p{parameters.Count}";

        parameters.Add(
            new NpgsqlParameter(parameterName, value));

        return parameterName;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
