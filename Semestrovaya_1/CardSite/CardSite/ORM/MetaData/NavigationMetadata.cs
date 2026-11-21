using System.Reflection;

namespace CardSite.ORM.Metadata;

public class NavigationMetadata
{
    public PropertyInfo Property { get; }

    public string ForeignKeyProperty { get; }

    public Type RelatedEntityType { get; }

    public NavigationMetadata(
        PropertyInfo property,
        string foreignKeyProperty,
        Type relatedEntityType)
    {
        Property = property;
        ForeignKeyProperty = foreignKeyProperty;
        RelatedEntityType = relatedEntityType;
    }
}