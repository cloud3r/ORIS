using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using Npgsql;
using CardSite.ORM.Metadata;

namespace CardSite.ORM;

public class DbSet<T> where T : class, new()
{
    private readonly DbConnectionFactory connectionFactory;
    private readonly EntityMapper mapper;
    private readonly EntityMetadata metadata;
    private readonly QueryBuilder queryBuilder;
    private readonly List<string> includedNavigations = new();
    private readonly ExpressionToSqlTranslator<T> expressionTranslator;
    private string? whereClause;
    private Expression<Func<T, bool>>? whereExpression;
    private string? orderByClause;
    private string? orderByColumn;
    public int LastQueryCount { get; private set; }
    private bool orderByDescending;
    private int? skipCount;
    private int? takeCount;

    public DbSet(
        DbConnectionFactory connectionFactory,
        EntityMapper mapper,
        QueryBuilder queryBuilder)
    {
        this.connectionFactory = connectionFactory;
        this.mapper = mapper;
        this.queryBuilder = queryBuilder;

        metadata = mapper.GetMetadata<T>();

        expressionTranslator = new ExpressionToSqlTranslator<T>(
            metadata,
            mapper
        );
    }
    
    private void ResetQueryState()
    {
        whereClause = null;
        whereExpression = null;

        orderByClause = null;
        orderByColumn = null;
        orderByDescending = false;

        skipCount = null;
        takeCount = null;

        includedNavigations.Clear();
    }

    private static DbType GetDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        if (type == typeof(int))
            return DbType.Int32;

        if (type == typeof(long))
            return DbType.Int64;

        if (type == typeof(short))
            return DbType.Int16;

        if (type == typeof(string))
            return DbType.String;

        if (type == typeof(bool))
            return DbType.Boolean;

        if (type == typeof(decimal))
            return DbType.Decimal;

        if (type == typeof(double))
            return DbType.Double;

        if (type == typeof(float))
            return DbType.Single;

        if (type == typeof(DateTime))
            return DbType.DateTime;

        if (type == typeof(Guid))
            return DbType.Guid;

        if (type == typeof(DateOnly))
            return DbType.Date;

        if (type == typeof(TimeOnly))
            return DbType.Time;

        throw new NotSupportedException(
            $"Тип {type.Name} не поддерживается ORM.");
    }
    
    private DbType GetAdditionalDbType(Type type)
    {
        if (type == typeof(Guid))
            return DbType.Guid;

        if (type == typeof(DateOnly))
            return DbType.Date;

        if (type == typeof(TimeOnly))
            return DbType.Time;

        throw new NotSupportedException(
            $"Type '{type.Name}' is not supported by the ORM."
        );
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        Type actualType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType.IsEnum)
        {
            return Enum.ToObject(actualType, value);
        }

        if (actualType == typeof(Guid))
        {
            return Guid.Parse(value.ToString()!);
        }

        if (actualType == typeof(DateOnly))
        {
            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            return DateOnly.Parse(value.ToString()!);
        }

        if (actualType == typeof(TimeOnly))
        {
            if (value is TimeSpan timeSpan)
                return TimeOnly.FromTimeSpan(timeSpan);

            return TimeOnly.Parse(value.ToString()!);
        }

        return Convert.ChangeType(
            value,
            actualType,
            CultureInfo.InvariantCulture
        );
    }

    public async Task<List<T>> ToListAsync()
{
    LastQueryCount = 0;

    var currentWhereExpression = whereExpression;
    var currentOrderByColumn = orderByColumn;
    var currentOrderByDescending = orderByDescending;
    var currentSkipCount = skipCount;
    var currentTakeCount = takeCount;
    var currentIncludedNavigations = includedNavigations.ToList();

    try
    {
        var result = new List<T>();

        var navigationIncludes = metadata.Navigations
            .Where(n => currentIncludedNavigations.Contains(n.Property.Name))
            .ToList();

        var collectionIncludes = metadata.CollectionNavigations
            .Where(n => currentIncludedNavigations.Contains(n.Property.Name))
            .ToList();

        string? filter = null;
        var parameters = new List<NpgsqlParameter>();
        var filterNavigations = new List<NavigationJoinInfo>();

        if (currentWhereExpression != null)
        {
            var translated = expressionTranslator.Translate(
                currentWhereExpression,
                navigationIncludes.Count > 0 ? "t0" : null
            );

            filter = translated.Sql;
            parameters = translated.Parameters;

            filterNavigations =
                expressionTranslator.RequiredNavigations.ToList();
        }

        string? orderBy = null;

        if (!string.IsNullOrWhiteSpace(currentOrderByColumn))
        {
            var property = metadata.Properties.FirstOrDefault(
                p => p.Property.Name == currentOrderByColumn
            );

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Property '{currentOrderByColumn}' was not found."
                );
            }

            string column = queryBuilder.QuoteIdentifier(property.ColumnName);

            bool useAlias =
                navigationIncludes.Count > 0 ||
                filterNavigations.Count > 0;

            orderBy = useAlias
                ? $"t0.{column} {(currentOrderByDescending ? "DESC" : "ASC")}"
                : $"{column} {(currentOrderByDescending ? "DESC" : "ASC")}";
        }

        string sql = queryBuilder.BuildSelectAllWithNavigationJoins(
            metadata,
            mapper,
            filterNavigations,
            navigationIncludes,
            filter,
            orderBy
        );
        sql = sql.TrimEnd().TrimEnd(';');

        if (currentTakeCount.HasValue)
        {
            sql += " LIMIT @take";
        }

        if (currentSkipCount.HasValue)
        {
            if (!currentTakeCount.HasValue)
            {
                sql += " LIMIT ALL";
            }

            sql += " OFFSET @skip";
        }

        sql += ";";

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddRange(parameters.ToArray());

        if (currentTakeCount.HasValue)
        {
            command.Parameters.AddWithValue(
                "take",
                currentTakeCount.Value
            );
        }

        if (currentSkipCount.HasValue)
        {
            command.Parameters.AddWithValue(
                "skip",
                currentSkipCount.Value
            );
        }

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                T entity;

                if (navigationIncludes.Count > 0)
                {
                    entity = mapper.MapEntityWithNavigations<T>(
                        reader,
                        navigationIncludes
                    );
                }
                else
                {
                    entity = mapper.MapEntityWithPrefix<T>(
                        reader,
                        ""
                    );
                }

                result.Add(entity);
            }
        }

        if (collectionIncludes.Count > 0 && result.Count > 0)
        {
            await LoadCollectionNavigationsAsync(
                result,
                collectionIncludes
            );
        }

        LastQueryCount = result.Count;

        return result;
    }
    finally
    {
        ResetQueryState();
    }
}
    
    public async Task<T?> FirstOrDefaultAsync()
    {
        var result = await Take(1).ToListAsync();

        if (result.Count == 0)
        {
            return null;
        }

        return result[0];
    }
    
    public async Task<int> CountAsync()
    {
        LastQueryCount = 0;

        try
        {
            string? filter = null;

            var parameters = new List<NpgsqlParameter>();

            if (whereExpression != null)
            {
                var translated = expressionTranslator.Translate(
                    whereExpression
                );

                filter = translated.Sql;
                parameters = translated.Parameters;
            }

            var filterNavigations =
                expressionTranslator.RequiredNavigations.ToList();

            string sql = filterNavigations.Count > 0
                ? queryBuilder.BuildCount(
                    metadata,
                    filterNavigations,
                    filter)
                : queryBuilder.BuildCount(
                    metadata,
                    filter);

            await using var connection =
                await connectionFactory.CreateConnectionAsync();

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddRange(parameters.ToArray());

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(
                result,
                CultureInfo.InvariantCulture
            );
        }
        finally
        {
            ResetQueryState();
        }
    }
    

    public async Task<T?> FindAsync(object id)
{
    ArgumentNullException.ThrowIfNull(id);

    var currentIncludedNavigations = includedNavigations.ToList();

    try
    {
        var navigationIncludes = metadata.Navigations
            .Where(n => currentIncludedNavigations.Contains(n.Property.Name))
            .ToList();

        var collectionIncludes = metadata.CollectionNavigations
            .Where(n => currentIncludedNavigations.Contains(n.Property.Name))
            .ToList();

        string sql = navigationIncludes.Count > 0
            ? queryBuilder.BuildSelectByIdWithJoins(
                metadata,
                mapper,
                navigationIncludes
            )
            : queryBuilder.BuildSelectById(metadata);

        T? entity = null;

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                entity = navigationIncludes.Count > 0
                    ? mapper.MapEntityWithNavigations<T>(
                        reader,
                        navigationIncludes
                    )
                    : mapper.MapEntityWithPrefix<T>(reader, "");
            }
        }

        if (entity == null)
        {
            return null;
        }

        if (collectionIncludes.Count > 0)
        {
            await LoadCollectionNavigationsAsync(
                new List<T> { entity },
                collectionIncludes
            );
        }

        return entity;
    }
    finally
    {
        ResetQueryState();
    }
}

    public async Task<int> AddAsync(T entity)
    {
    ArgumentNullException.ThrowIfNull(entity);

    var originalId = metadata.PrimaryKey.Property.GetValue(entity);

    await using var connection =
        await connectionFactory.CreateConnectionAsync();

    await using var transaction =
        await connection.BeginTransactionAsync();

    bool committed = false;

    try
    {
        await using (var command = new NpgsqlCommand(
            queryBuilder.BuildInsert(metadata),
            connection,
            transaction))
        {
            AddEntityParameters(command, entity);

            var id = await command.ExecuteScalarAsync();

            if (id == null || id == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Failed to insert entity.");
            }

            int generatedId = Convert.ToInt32(
                id,
                CultureInfo.InvariantCulture
            );

            metadata.PrimaryKey.Property.SetValue(entity, generatedId);

            foreach (var navigation in metadata.CollectionNavigations)
            {
                var collection = navigation.Property.GetValue(entity);

                if (collection is not System.Collections.IEnumerable relatedEntities)
                    continue;

                var relatedMetadata = mapper.GetMetadata(
                    navigation.RelatedEntityType
                );

                var processedIds = new HashSet<object>();

                foreach (var relatedEntity in relatedEntities)
                {
                    if (relatedEntity == null)
                        continue;

                    var relatedId = relatedMetadata.PrimaryKey.Property
                        .GetValue(relatedEntity);

                    if (relatedId == null)
                    {
                        throw new InvalidOperationException(
                            $"Related entity {navigation.RelatedEntityType.Name} " +
                            "has no primary key.");
                    }

                    if (!processedIds.Add(relatedId))
                        continue;

                    string sql =
                        $"INSERT INTO {QuoteIdentifier(navigation.JoinTable)} " +
                        $"({QuoteIdentifier(navigation.SourceForeignKey)}, " +
                        $"{QuoteIdentifier(navigation.TargetForeignKey)}) " +
                        "VALUES (@sourceId, @targetId) " +
                        "ON CONFLICT DO NOTHING;";

                    await using var relationCommand = new NpgsqlCommand(
                        sql,
                        connection,
                        transaction
                    );

                    relationCommand.Parameters.AddWithValue(
                        "sourceId",
                        generatedId
                    );

                    relationCommand.Parameters.AddWithValue(
                        "targetId",
                        relatedId
                    );

                    await relationCommand.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();

            committed = true;

            return generatedId;
        }
    }
    catch (Exception operationException)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            throw new AggregateException(
                "The database operation failed, and the transaction rollback also failed.",
                operationException,
                rollbackException
            );
        }

        throw;
    }
    finally
    {
        if (!committed)
        {
            metadata.PrimaryKey.Property.SetValue(entity, originalId);
        }
    }
}

    public async Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var id = metadata.PrimaryKey.Property.GetValue(entity);

        if (id == null)
        {
            throw new InvalidOperationException(
                "Entity primary key cannot be null.");
        }

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(
            queryBuilder.BuildUpdate(metadata),
            connection);

        AddEntityParameters(command, entity);

        command.Parameters.AddWithValue("id", id);

        int affectedRows = await command.ExecuteNonQueryAsync();

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Entity not found. Update failed.");
        }
    }

    public async Task RemoveAsync(object id)
    {
        ArgumentNullException.ThrowIfNull(id);

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(
            queryBuilder.BuildDelete(metadata),
            connection);

        command.Parameters.AddWithValue("id", id);

        int affectedRows = await command.ExecuteNonQueryAsync();

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Entity not found. Delete failed.");
        }
    }
    
    public async Task RemoveWithRelationsAsync(T entity)
{
    ArgumentNullException.ThrowIfNull(entity);

    var sourceId = metadata.PrimaryKey.Property.GetValue(entity);

    if (sourceId == null)
    {
        throw new InvalidOperationException(
            "Entity has no primary key.");
    }

    await using var connection =
        await connectionFactory.CreateConnectionAsync();

    await using var transaction =
        await connection.BeginTransactionAsync();

    try
    {
        foreach (var navigation in metadata.CollectionNavigations)
        {
            string deleteRelationsSql =
                $"DELETE FROM {QuoteIdentifier(navigation.JoinTable)} " +
                $"WHERE {QuoteIdentifier(navigation.SourceForeignKey)} = @sourceId;";

            await using var command = new NpgsqlCommand(
                deleteRelationsSql,
                connection,
                transaction);

            command.Parameters.AddWithValue("sourceId", sourceId);

            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand(
            queryBuilder.BuildDelete(metadata),
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("id", sourceId);

            int affectedRows = await command.ExecuteNonQueryAsync();

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "Entity not found. Delete failed.");
            }
        }

        await transaction.CommitAsync();
    }
    catch (Exception operationException)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            throw new AggregateException(
                "The delete operation failed, and the transaction rollback also failed.",
                operationException,
                rollbackException);
        }

        throw;
    }
}

    private T MapEntity(NpgsqlDataReader reader)
    {
        var entity = new T();

        foreach (var property in metadata.Properties)
        {
            int ordinal = reader.GetOrdinal(property.ColumnName);

            object? value = reader.IsDBNull(ordinal)
                ? null
                : reader.GetValue(ordinal);

            if (value == null &&
                property.Property.PropertyType.IsValueType &&
                Nullable.GetUnderlyingType(
                    property.Property.PropertyType) == null)
            {
                throw new InvalidOperationException(
                    $"Column '{property.ColumnName}' contains NULL, " +
                    $"but property '{property.Property.Name}' is not nullable.");
            }

            object? convertedValue = ConvertValue(
                value,
                property.Property.PropertyType
            );

            property.Property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    private void AddEntityParameters(NpgsqlCommand command, T entity)
    {
        foreach (var property in metadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            object? value = property.Property.GetValue(entity);

            Type propertyType = Nullable.GetUnderlyingType(
                property.Property.PropertyType
            ) ?? property.Property.PropertyType;

            if (value != null && value.GetType().IsEnum)
            {
                value = Convert.ChangeType(
                    value,
                    Enum.GetUnderlyingType(propertyType),
                    CultureInfo.InvariantCulture
                );
            }

            var parameter = new NpgsqlParameter
            {
                ParameterName = property.Property.Name,
                DbType = GetDbType(property.Property.PropertyType),
                Value = value ?? DBNull.Value
            };

            command.Parameters.Add(parameter);
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    public DbSet<T> Include(string navigationProperty)
    {
        var navigation = metadata.Navigations
            .FirstOrDefault(n =>
                n.Property.Name == navigationProperty);

        var collectionNavigation = metadata.CollectionNavigations
            .FirstOrDefault(n =>
                n.Property.Name == navigationProperty);

        if (navigation == null && collectionNavigation == null)
        {
            throw new InvalidOperationException(
                $"Navigation property '{navigationProperty}' not found.");
        }

        if (!includedNavigations.Contains(navigationProperty))
        {
            includedNavigations.Add(navigationProperty);
        }

        return this;
    }

    private async Task<object?> LoadRelatedEntityAsync(
        Type entityType,
        object id)
    {
        Type dbSetType = typeof(DbSet<>).MakeGenericType(entityType);

        var dbSet = Activator.CreateInstance(
            dbSetType,
            connectionFactory,
            mapper,
            queryBuilder
        );

        if (dbSet == null)
        {
            throw new InvalidOperationException(
                $"Не удалось создать DbSet для типа {entityType.Name}."
            );
        }

        var findMethod = dbSetType.GetMethod(nameof(FindAsync));

        if (findMethod == null)
        {
            throw new InvalidOperationException(
                $"Метод FindAsync не найден для типа {entityType.Name}."
            );
        }

        var task = findMethod.Invoke(
            dbSet,
            new[] { id }
        ) as Task;

        if (task == null)
        {
            throw new InvalidOperationException(
                $"Не удалось выполнить FindAsync для типа {entityType.Name}."
            );
        }

        await task;

        var resultProperty = task.GetType().GetProperty("Result");

        return resultProperty?.GetValue(task);
    }

    private async Task LoadNavigationsAsync(
        T entity,
        List<string> navigations)
    {
        foreach (var navigationName in navigations)
        {
            var navigation = metadata.Navigations
                .First(n => n.Property.Name == navigationName);

            var foreignKeyProperty = typeof(T).GetProperty(
                navigation.ForeignKeyProperty);

            if (foreignKeyProperty == null)
            {
                throw new InvalidOperationException(
                    $"Foreign key '{navigation.ForeignKeyProperty}' not found.");
            }

            var foreignKeyValue = foreignKeyProperty.GetValue(entity);

            if (foreignKeyValue == null)
            {
                navigation.Property.SetValue(entity, null);
                continue;
            }

            object? relatedEntity = await LoadRelatedEntityAsync(
                navigation.RelatedEntityType,
                foreignKeyValue
            );

            navigation.Property.SetValue(entity, relatedEntity);
        }
    }

    public DbSet<T> Include<TProperty>(
        Expression<Func<T, TProperty>> navigationExpression)
    {
        Expression body = navigationExpression.Body;

        if (body is UnaryExpression unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        if (body is not MemberExpression memberExpression)
        {
            throw new InvalidOperationException(
                "Include must reference a navigation property.");
        }

        string propertyName = memberExpression.Member.Name;

        return Include(propertyName);
    }
    
    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var firstBody = new ReplaceParameterVisitor(
            first.Parameters[0],
            parameter
        ).Visit(first.Body)!;

        var secondBody = new ReplaceParameterVisitor(
            second.Parameters[0],
            parameter
        ).Visit(second.Body)!;

        var combinedBody = Expression.AndAlso(
            firstBody,
            secondBody
        );

        return Expression.Lambda<Func<T, bool>>(
            combinedBody,
            parameter
        );
    }
    
    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression oldParameter;
        private readonly ParameterExpression newParameter;

        public ReplaceParameterVisitor(
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
        {
            this.oldParameter = oldParameter;
            this.newParameter = newParameter;
        }

        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == oldParameter
                ? newParameter
                : base.VisitParameter(node);
        }
    }

    public DbSet<T> Where(Expression<Func<T, bool>> predicate)
    {
        whereExpression = whereExpression == null
            ? predicate
            : CombineExpressions(whereExpression, predicate);

        return this;
    }
    
    

    public DbSet<T> OrderBy<TKey>(
        Expression<Func<T, TKey>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            throw new InvalidOperationException(
                "OrderBy supports only property expressions.");
        }

        var property = metadata.Properties.FirstOrDefault(
            p => p.Property.Name == memberExpression.Member.Name);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{memberExpression.Member.Name}' not found.");
        }

        orderByColumn = property.Property.Name;
        orderByDescending = false;

        return this;
    }

    public DbSet<T> OrderByDescending<TKey>(
        Expression<Func<T, TKey>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            throw new InvalidOperationException(
                "OrderByDescending supports only property expressions.");
        }

        var property = metadata.Properties.FirstOrDefault(
            p => p.Property.Name == memberExpression.Member.Name);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{memberExpression.Member.Name}' not found.");
        }

        orderByColumn = property.Property.Name;
        orderByDescending = true;

        return this;
    }
    
    public DbSet<T> Skip(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Skip count cannot be negative.");
        }

        skipCount = count;

        return this;
    }

    public DbSet<T> Take(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Take count cannot be negative.");
        }

        takeCount = count;

        return this;
    }

    private async Task LoadCollectionNavigationsAsync(
    List<T> entities,
    List<CollectionNavigationMetadata> navigations)
{
    if (entities.Count == 0 || navigations.Count == 0)
        return;

    await using var connection =
        await connectionFactory.CreateConnectionAsync();

    foreach (var navigation in navigations)
    {
        var relatedMetadata = mapper.GetMetadata(
            navigation.RelatedEntityType);

        var sourceEntities = new List<(T Entity, object Id)>();

        foreach (var entity in entities)
        {
            var sourceId = metadata.PrimaryKey.Property.GetValue(entity);

            if (sourceId != null)
            {
                sourceEntities.Add((entity, sourceId));
            }
        }

        var relatedEntitiesBySourceId =
            new Dictionary<object, List<object>>();

        foreach (var source in sourceEntities)
        {
            relatedEntitiesBySourceId[source.Id] = new List<object>();
        }

        if (sourceEntities.Count == 0)
            continue;

        var parameterNames = new List<string>();

        for (int i = 0; i < sourceEntities.Count; i++)
        {
            parameterNames.Add($"@id{i}");
        }

        string sql =
            $"SELECT related.*, " +
            $"junction.{QuoteIdentifier(navigation.SourceForeignKey)} AS \"__source_id\" " +
            $"FROM {QuoteIdentifier(relatedMetadata.TableName)} AS related " +
            $"INNER JOIN {QuoteIdentifier(navigation.JoinTable)} AS junction " +
            $"ON related.{QuoteIdentifier(relatedMetadata.PrimaryKey.ColumnName)} " +
            $"= junction.{QuoteIdentifier(navigation.TargetForeignKey)} " +
            $"WHERE junction.{QuoteIdentifier(navigation.SourceForeignKey)} " +
            $"IN ({string.Join(", ", parameterNames)});";

        await using var command = new NpgsqlCommand(sql, connection);

        for (int i = 0; i < sourceEntities.Count; i++)
        {
            command.Parameters.AddWithValue(
                $"id{i}",
                sourceEntities[i].Id);
        }

        await using (var reader = await command.ExecuteReaderAsync())
        {
            int sourceIdOrdinal = reader.GetOrdinal("__source_id");

            while (await reader.ReadAsync())
            {
                var sourceId = reader.GetValue(sourceIdOrdinal);

                var relatedEntity = mapper.MapRelatedEntityForLoading(
                    reader,
                    navigation.RelatedEntityType);

                relatedEntitiesBySourceId[sourceId].Add(relatedEntity);
            }
        }

        var collectionType = typeof(List<>).MakeGenericType(
            navigation.RelatedEntityType);

        foreach (var source in sourceEntities)
        {
            var collection = Activator.CreateInstance(collectionType);

            var addMethod = collectionType.GetMethod("Add");

            foreach (var relatedEntity in relatedEntitiesBySourceId[source.Id])
            {
                addMethod!.Invoke(collection, new[] { relatedEntity });
            }

            navigation.Property.SetValue(source.Entity, collection);
        }
    }
}
    
    public async Task AddManyToManyAsync(
        string navigationProperty,
        object sourceId,
        object targetId)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        ArgumentNullException.ThrowIfNull(targetId);

        var navigation = metadata.CollectionNavigations
            .FirstOrDefault(n => n.Property.Name == navigationProperty);

        if (navigation == null)
        {
            throw new InvalidOperationException(
                $"Many-to-Many navigation '{navigationProperty}' not found.");
        }

        string sql =
            $"INSERT INTO {QuoteIdentifier(navigation.JoinTable)} " +
            $"({QuoteIdentifier(navigation.SourceForeignKey)}, " +
            $"{QuoteIdentifier(navigation.TargetForeignKey)}) " +
            "VALUES (@sourceId, @targetId) " +
            "ON CONFLICT DO NOTHING;";

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("sourceId", sourceId);
        command.Parameters.AddWithValue("targetId", targetId);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveManyToManyAsync(
        string navigationProperty,
        object sourceId,
        object targetId)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        ArgumentNullException.ThrowIfNull(targetId);

        var navigation = metadata.CollectionNavigations
            .FirstOrDefault(n => n.Property.Name == navigationProperty);

        if (navigation == null)
        {
            throw new InvalidOperationException(
                $"Many-to-Many navigation '{navigationProperty}' not found.");
        }

        string sql =
            $"DELETE FROM {QuoteIdentifier(navigation.JoinTable)} " +
            $"WHERE {QuoteIdentifier(navigation.SourceForeignKey)} = @sourceId " +
            $"AND {QuoteIdentifier(navigation.TargetForeignKey)} = @targetId;";

        await using var connection =
            await connectionFactory.CreateConnectionAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("sourceId", sourceId);
        command.Parameters.AddWithValue("targetId", targetId);

        await command.ExecuteNonQueryAsync();
    }
    
   public async Task UpdateWithRelationsAsync(T entity)
    {
    ArgumentNullException.ThrowIfNull(entity);

    var sourceId = metadata.PrimaryKey.Property.GetValue(entity);

    if (sourceId == null)
    {
        throw new InvalidOperationException(
            "Entity has no primary key.");
    }

    await using var connection =
        await connectionFactory.CreateConnectionAsync();

    await using var transaction =
        await connection.BeginTransactionAsync();

    try
    {
        await using (var command = new NpgsqlCommand(
            queryBuilder.BuildUpdate(metadata),
            connection,
            transaction))
        {
            AddEntityParameters(command, entity);

            command.Parameters.AddWithValue("id", sourceId);

            int affectedRows = await command.ExecuteNonQueryAsync();

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "Entity not found. Update failed.");
            }
        }

        foreach (var navigation in metadata.CollectionNavigations)
        {
            var collection = navigation.Property.GetValue(entity);

            if (collection == null)
                continue;

            var relatedMetadata = mapper.GetMetadata(
                navigation.RelatedEntityType);

            var relatedIds = new HashSet<object>();

            foreach (var relatedEntity in
                     (System.Collections.IEnumerable)collection)
            {
                if (relatedEntity == null)
                    continue;

                var relatedId = relatedMetadata.PrimaryKey.Property
                    .GetValue(relatedEntity);

                if (relatedId == null)
                {
                    throw new InvalidOperationException(
                        $"Related entity {navigation.RelatedEntityType.Name} " +
                        "has no primary key.");
                }

                relatedIds.Add(relatedId);
            }

            string deleteSql =
                $"DELETE FROM {QuoteIdentifier(navigation.JoinTable)} " +
                $"WHERE {QuoteIdentifier(navigation.SourceForeignKey)} = @sourceId;";

            await using (var deleteCommand = new NpgsqlCommand(
                deleteSql,
                connection,
                transaction))
            {
                deleteCommand.Parameters.AddWithValue(
                    "sourceId",
                    sourceId);

                await deleteCommand.ExecuteNonQueryAsync();
            }

            string insertSql =
                $"INSERT INTO {QuoteIdentifier(navigation.JoinTable)} " +
                $"({QuoteIdentifier(navigation.SourceForeignKey)}, " +
                $"{QuoteIdentifier(navigation.TargetForeignKey)}) " +
                "VALUES (@sourceId, @targetId) " +
                "ON CONFLICT DO NOTHING;";

            foreach (var relatedId in relatedIds)
            {
                await using var insertCommand = new NpgsqlCommand(
                    insertSql,
                    connection,
                    transaction);

                insertCommand.Parameters.AddWithValue(
                    "sourceId",
                    sourceId);

                insertCommand.Parameters.AddWithValue(
                    "targetId",
                    relatedId);

                await insertCommand.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
    }
    catch (Exception operationException)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            throw new AggregateException(
                "The database operation failed, and the transaction rollback also failed.",
                operationException,
                rollbackException
            );
        }

        throw;
    }
}
}
