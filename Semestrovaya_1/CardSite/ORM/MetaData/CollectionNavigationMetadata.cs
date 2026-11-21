using System.Reflection;

namespace CardSite.ORM.Metadata;

public class CollectionNavigationMetadata
{
    public PropertyInfo Property { get; }

    public Type RelatedEntityType { get; }

    public string JoinTable { get; }

    public string SourceForeignKey { get; }

    public string TargetForeignKey { get; }

    public CollectionNavigationMetadata(
        PropertyInfo property,
        Type relatedEntityType,
        string joinTable,
        string sourceForeignKey,
        string targetForeignKey)
    {
        Property = property;
        RelatedEntityType = relatedEntityType;
        JoinTable = joinTable;
        SourceForeignKey = sourceForeignKey;
        TargetForeignKey = targetForeignKey;
    }
}