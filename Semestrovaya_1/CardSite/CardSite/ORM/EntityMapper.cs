using System.Reflection;
using CardSite.ORM.Attributes;
using CardSite.ORM.Metadata;
using Npgsql;

namespace CardSite.ORM;

public class EntityMapper
{
    public EntityMetadata GetMetadata<T>() where T : class
    {
        return GetMetadata(typeof(T));
    }

    public EntityMetadata GetMetadata(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();

        if (tableAttribute == null)
        {
            throw new InvalidOperationException(
                $"Class {type.Name} does not have TableAttribute.");
        }

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        var propertyMetadataList = new List<PropertyMetadata>();
        var navigationMetadataList = new List<NavigationMetadata>();
        var collectionNavigationMetadataList =
            new List<CollectionNavigationMetadata>();

        foreach (var property in properties)
        {
            var foreignKeyAttribute =
                property.GetCustomAttribute<ForeignKeyAttribute>();

            if (foreignKeyAttribute != null)
            {
                navigationMetadataList.Add(
                    new NavigationMetadata(
                        property,
                        foreignKeyAttribute.ForeignKeyProperty,
                        property.PropertyType
                    )
                );

                continue;
            }

            var manyToManyAttribute =
                property.GetCustomAttribute<ManyToManyAttribute>();

            if (manyToManyAttribute != null)
            {
                if (!property.PropertyType.IsGenericType)
                {
                    throw new InvalidOperationException(
                        $"Property {property.Name} must be a generic collection.");
                }

                Type relatedEntityType =
                    property.PropertyType.GetGenericArguments()[0];

                collectionNavigationMetadataList.Add(
                    new CollectionNavigationMetadata(
                        property,
                        relatedEntityType,
                        manyToManyAttribute.JoinTable,
                        manyToManyAttribute.SourceForeignKey,
                        manyToManyAttribute.TargetForeignKey
                    )
                );

                continue;
            }

            if (property.PropertyType != typeof(string) &&
                typeof(System.Collections.IEnumerable)
                    .IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            var columnAttribute =
                property.GetCustomAttribute<ColumnAttribute>();

            var keyAttribute =
                property.GetCustomAttribute<KeyAttribute>();

            string columnName = columnAttribute?.Name ?? property.Name;

            propertyMetadataList.Add(
                new PropertyMetadata(
                    property,
                    columnName,
                    keyAttribute != null
                )
            );
        }

        var primaryKeys = propertyMetadataList
            .Where(p => p.IsPrimaryKey)
            .ToList();

        if (primaryKeys.Count != 1)
        {
            throw new InvalidOperationException(
                $"Class {type.Name} must have exactly one primary key.");
        }

        return new EntityMetadata(
            tableAttribute.Name,
            propertyMetadataList,
            primaryKeys[0],
            navigationMetadataList,
            collectionNavigationMetadataList
        );
    }

    public T MapEntityWithPrefix<T>(
        NpgsqlDataReader reader,
        string prefix) where T : class, new()
    {
        var metadata = GetMetadata<T>();
        var entity = new T();

        foreach (var property in metadata.Properties)
        {
            string columnAlias = string.IsNullOrEmpty(prefix)
                ? property.ColumnName
                : $"{prefix}_{property.ColumnName}";

            int ordinal;

            try
            {
                ordinal = reader.GetOrdinal(columnAlias);
            }
            catch (IndexOutOfRangeException)
            {
                continue;
            }

            if (reader.IsDBNull(ordinal))
                continue;

            object value = reader.GetValue(ordinal);

            object? convertedValue = ConvertValue(
                value,
                property.Property.PropertyType
            );

            property.Property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    private object? ConvertValue(object value, Type targetType)
    {
        Type actualType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType.IsInstanceOfType(value))
            return value;

        if (actualType.IsEnum)
        {
            if (value is string stringValue)
                return Enum.Parse(actualType, stringValue);

            return Enum.ToObject(actualType, value);
        }

        if (actualType == typeof(Guid))
            return Guid.Parse(value.ToString()!);

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

            if (value is DateTime dateTime)
                return TimeOnly.FromDateTime(dateTime);

            return TimeOnly.Parse(value.ToString()!);
        }

        return Convert.ChangeType(value, actualType);
    }

    private object MapRelatedEntity(
        NpgsqlDataReader reader,
        Type entityType,
        string prefix)
    {
        var metadata = GetMetadata(entityType);

        var entity = Activator.CreateInstance(entityType)
                     ?? throw new InvalidOperationException(
                         $"Cannot create instance of {entityType.Name}.");

        foreach (var property in metadata.Properties)
        {
            string columnAlias = string.IsNullOrEmpty(prefix)
                ? property.ColumnName
                : $"{prefix}_{property.ColumnName}";

            int ordinal = reader.GetOrdinal(columnAlias);

            if (reader.IsDBNull(ordinal))
                continue;

            object value = reader.GetValue(ordinal);

            object? convertedValue = ConvertValue(
                value,
                property.Property.PropertyType
            );

            property.Property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    public T MapEntityWithNavigations<T>(
        NpgsqlDataReader reader,
        List<NavigationMetadata> navigations)
        where T : class, new()
    {
        var entity = MapEntityWithPrefix<T>(reader, "t0");

        foreach (var navigation in navigations)
        {
            string prefix = navigation.Property.Name;

            Type relatedType = navigation.RelatedEntityType;

            var relatedMetadata = GetMetadata(relatedType);

            int primaryKeyOrdinal;

            try
            {
                primaryKeyOrdinal = reader.GetOrdinal(
                    $"{prefix}_{relatedMetadata.PrimaryKey.ColumnName}"
                );
            }
            catch (IndexOutOfRangeException)
            {
                continue;
            }

            if (reader.IsDBNull(primaryKeyOrdinal))
            {
                navigation.Property.SetValue(entity, null);
                continue;
            }

            object relatedEntity = MapRelatedEntity(
                reader,
                relatedType,
                prefix
            );

            navigation.Property.SetValue(entity, relatedEntity);
        }

        return entity;
    }
    
    public object MapRelatedEntityForLoading(
        NpgsqlDataReader reader,
        Type entityType)
    {
        return MapRelatedEntity(reader, entityType, "");
    }
}