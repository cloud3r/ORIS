namespace CardSite.ORM.Metadata;

public class EntityMetadata
{
    public string TableName { get; }

    public List<PropertyMetadata> Properties { get; }

    public PropertyMetadata PrimaryKey { get; }

    public List<NavigationMetadata> Navigations { get; }

    public List<CollectionNavigationMetadata> CollectionNavigations { get; }

    public EntityMetadata(
        string tableName,
        List<PropertyMetadata> properties,
        PropertyMetadata primaryKey,
        List<NavigationMetadata> navigations,
        List<CollectionNavigationMetadata> collectionNavigations)
    {
        TableName = tableName;
        Properties = properties;
        PrimaryKey = primaryKey;
        Navigations = navigations;
        CollectionNavigations = collectionNavigations;
    }
}