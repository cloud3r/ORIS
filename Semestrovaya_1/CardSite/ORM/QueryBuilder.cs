using CardSite.ORM.Metadata;

namespace CardSite.ORM;

public class QueryBuilder
{
    public string BuildSelectAll(EntityMetadata metadata)
    {
        string columns = string.Join(", ",
            metadata.Properties.Select(p => QuoteIdentifier(p.ColumnName)));

        return $"SELECT {columns} FROM {QuoteIdentifier(metadata.TableName)};";
    }

    public string BuildSelectById(EntityMetadata metadata)
    {
        string columns = string.Join(", ",
            metadata.Properties.Select(p => QuoteIdentifier(p.ColumnName)));

        return $"SELECT {columns} " +
               $"FROM {QuoteIdentifier(metadata.TableName)} " +
               $"WHERE {QuoteIdentifier(metadata.PrimaryKey.ColumnName)} = @id;";
    }

    public string BuildInsert(EntityMetadata metadata)
    {
        var properties = metadata.Properties
            .Where(p => !p.IsPrimaryKey)
            .ToList();

        string columns = string.Join(", ",
            properties.Select(p => QuoteIdentifier(p.ColumnName)));

        string parameters = string.Join(", ",
            properties.Select(p => $"@{p.Property.Name}"));

        return $"INSERT INTO {QuoteIdentifier(metadata.TableName)} " +
               $"({columns}) VALUES ({parameters}) " +
               $"RETURNING {QuoteIdentifier(metadata.PrimaryKey.ColumnName)};";
    }

    public string BuildUpdate(EntityMetadata metadata)
    {
        var properties = metadata.Properties
            .Where(p => !p.IsPrimaryKey)
            .ToList();

        string setClause = string.Join(", ",
            properties.Select(p =>
                $"{QuoteIdentifier(p.ColumnName)} = @{p.Property.Name}"));

        return $"UPDATE {QuoteIdentifier(metadata.TableName)} " +
               $"SET {setClause} " +
               $"WHERE {QuoteIdentifier(metadata.PrimaryKey.ColumnName)} = @id;";
    }

    public string BuildDelete(EntityMetadata metadata)
    {
        return $"DELETE FROM {QuoteIdentifier(metadata.TableName)} " +
               $"WHERE {QuoteIdentifier(metadata.PrimaryKey.ColumnName)} = @id;";
    }

    public string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
    
    public string BuildSelectAllWithJoins(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationMetadata> navigations)
    {
        string rootAlias = "t0";

        var selectColumns = new List<string>();

        foreach (var property in metadata.Properties)
        {
            selectColumns.Add(
                $"{rootAlias}.{QuoteIdentifier(property.ColumnName)} " +
                $"AS {QuoteIdentifier($"t0_{property.ColumnName}")}"
            );
        }

        var joins = new List<string>();

        int index = 1;

        foreach (var navigation in navigations)
        {
            var relatedMetadata = mapper.GetMetadata(
                navigation.RelatedEntityType
            );

            string alias = $"t{index}";

            var foreignKey = metadata.Properties.FirstOrDefault(
                p => p.Property.Name == navigation.ForeignKeyProperty);

            if (foreignKey == null)
            {
                throw new InvalidOperationException(
                    $"Foreign key '{navigation.ForeignKeyProperty}' not found.");
            }

            foreach (var property in relatedMetadata.Properties)
            {
                selectColumns.Add(
                    $"{alias}.{QuoteIdentifier(property.ColumnName)} " +
                    $"AS {QuoteIdentifier($"{navigation.Property.Name}_{property.ColumnName}")}"
                );
            }

            joins.Add(
                $"LEFT JOIN {QuoteIdentifier(relatedMetadata.TableName)} AS {alias} " +
                $"ON {rootAlias}.{QuoteIdentifier(foreignKey.ColumnName)} = " +
                $"{alias}.{QuoteIdentifier(relatedMetadata.PrimaryKey.ColumnName)}"
            );

            index++;
        }

        return
            $"SELECT {string.Join(", ", selectColumns)} " +
            $"FROM {QuoteIdentifier(metadata.TableName)} AS {rootAlias} " +
            $"{string.Join(" ", joins)};";
    }
    
    public string BuildSelectByIdWithJoins(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationMetadata> navigations)
    {
        string sql = BuildSelectAllWithJoins(
            metadata,
            mapper,
            navigations
        );

        sql = sql.TrimEnd(';');

        return sql +
               $" WHERE t0.{QuoteIdentifier(metadata.PrimaryKey.ColumnName)} = @id;";
    }
    
    public string BuildSelectAll(
        EntityMetadata metadata,
        string whereClause,
        string orderByClause)
    {
        string sql = BuildSelectAll(metadata);

        sql = sql.TrimEnd(';');

        return $"{sql} WHERE {whereClause} ORDER BY {orderByClause};";
    }
    
    public string BuildSelectAllWithWhere(
        EntityMetadata metadata,
        string whereClause)
    {
        string sql = BuildSelectAll(metadata);

        sql = sql.TrimEnd(';');

        return $"{sql} WHERE {whereClause};";
    }

    public string BuildSelectAllWithOrderBy(
        EntityMetadata metadata,
        string orderByClause)
    {
        string sql = BuildSelectAll(metadata);

        sql = sql.TrimEnd(';');

        return $"{sql} ORDER BY {orderByClause};";
    }

    public string BuildSelectAllWithJoinsAndWhere(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationMetadata> navigations,
        string whereClause)
    {
        string sql = BuildSelectAllWithJoins(
            metadata,
            mapper,
            navigations);

        sql = sql.TrimEnd(';');

        return $"{sql} WHERE {whereClause};";
    }

    public string BuildSelectAllWithJoinsAndOrderBy(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationMetadata> navigations,
        string orderByClause)
    {
        string sql = BuildSelectAllWithJoins(
            metadata,
            mapper,
            navigations);

        sql = sql.TrimEnd(';');

        return $"{sql} ORDER BY {orderByClause};";
    }

    public string BuildSelectAllWithJoins(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationMetadata> navigations,
        string whereClause,
        string orderByClause)
    {
        string sql = BuildSelectAllWithJoins(
            metadata,
            mapper,
            navigations);

        sql = sql.TrimEnd(';');

        return $"{sql} WHERE {whereClause} ORDER BY {orderByClause};";
    }
    
    public string BuildSelectAllWithNavigationJoins(
        EntityMetadata metadata,
        EntityMapper mapper,
        List<NavigationJoinInfo> filterNavigations,
        List<NavigationMetadata> includeNavigations,
        string? whereClause = null,
        string? orderByClause = null)
{
    const string rootAlias = "t0";

    var selectColumns = new List<string>();
    var joins = new List<string>();

    foreach (var property in metadata.Properties)
    {
        string column = $"{rootAlias}.{QuoteIdentifier(property.ColumnName)}";

        string alias = includeNavigations.Count > 0
            ? $"t0_{property.ColumnName}"
            : property.ColumnName;

        selectColumns.Add(
            $"{column} AS {QuoteIdentifier(alias)}");
    }

    var navigationAliases = new Dictionary<string, NavigationJoinInfo>();

    foreach (var navigation in filterNavigations)
    {
        navigationAliases[navigation.Navigation.Property.Name] = navigation;

        joins.Add(
            $"LEFT JOIN {QuoteIdentifier(navigation.RelatedMetadata.TableName)} " +
            $"AS {navigation.Alias} " +
            $"ON {rootAlias}.{QuoteIdentifier(navigation.ForeignKeyColumn)} = " +
            $"{navigation.Alias}.{QuoteIdentifier(navigation.RelatedPrimaryKeyColumn)}");
    }

    int nextAlias = filterNavigations.Count + 1;

    foreach (var navigation in includeNavigations)
    {
        NavigationJoinInfo join;

        if (navigationAliases.TryGetValue(
                navigation.Property.Name,
                out var existingJoin))
        {
            join = existingJoin;
        }
        else
        {
            var relatedMetadata = mapper.GetMetadata(
                navigation.RelatedEntityType
            );

            var foreignKey = metadata.Properties.FirstOrDefault(
                p => p.Property.Name == navigation.ForeignKeyProperty);

            if (foreignKey == null)
            {
                throw new InvalidOperationException(
                    $"Foreign key '{navigation.ForeignKeyProperty}' not found.");
            }

            string alias = $"t{nextAlias++}";

            join = new NavigationJoinInfo(
                navigation,
                relatedMetadata,
                alias,
                foreignKey.ColumnName,
                relatedMetadata.PrimaryKey.ColumnName);

            navigationAliases.Add(
                navigation.Property.Name,
                join);

            joins.Add(
                $"LEFT JOIN {QuoteIdentifier(relatedMetadata.TableName)} " +
                $"AS {alias} " +
                $"ON {rootAlias}.{QuoteIdentifier(foreignKey.ColumnName)} = " +
                $"{alias}.{QuoteIdentifier(relatedMetadata.PrimaryKey.ColumnName)}");
        }

        foreach (var property in join.RelatedMetadata.Properties)
        {
            selectColumns.Add(
                $"{join.Alias}.{QuoteIdentifier(property.ColumnName)} " +
                $"AS {QuoteIdentifier(
                    $"{navigation.Property.Name}_{property.ColumnName}")}");
        }
    }

    string sql =
        $"SELECT {string.Join(", ", selectColumns)} " +
        $"FROM {QuoteIdentifier(metadata.TableName)} AS {rootAlias}";

    if (joins.Count > 0)
    {
        sql += " " + string.Join(" ", joins);
    }

    if (!string.IsNullOrWhiteSpace(whereClause))
    {
        sql += $" WHERE {whereClause}";
    }

    if (!string.IsNullOrWhiteSpace(orderByClause))
    {
        sql += $" ORDER BY {orderByClause}";
    }

    return sql + ";";
}
    
    public string BuildCount(
        EntityMetadata metadata,
        string? whereClause = null)
    {
        string sql =
            $"SELECT COUNT(*) FROM {QuoteIdentifier(metadata.TableName)}";

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        return sql + ";";
    }
    
    public string BuildCount(
        EntityMetadata metadata,
        List<NavigationJoinInfo> requiredNavigations,
        string? whereClause = null)
    {
        const string rootAlias = "t0";

        string sql =
            $"SELECT COUNT(*) " +
            $"FROM {QuoteIdentifier(metadata.TableName)} AS {rootAlias}";

        foreach (var navigation in requiredNavigations)
        {
            sql +=
                $" LEFT JOIN {QuoteIdentifier(navigation.RelatedMetadata.TableName)} " +
                $"AS {navigation.Alias} " +
                $"ON {rootAlias}.{QuoteIdentifier(navigation.ForeignKeyColumn)} = " +
                $"{navigation.Alias}.{QuoteIdentifier(navigation.RelatedPrimaryKeyColumn)}";
        }

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        return sql + ";";
    }
}