using System.Reflection;

namespace CardSite.ORM.Metadata;

public class PropertyMetadata
{
    public PropertyInfo Property { get; }

    public string ColumnName { get; }

    public bool IsPrimaryKey { get; }

    public PropertyMetadata(
        PropertyInfo property,
        string columnName,
        bool isPrimaryKey)
    {
        Property = property;
        ColumnName = columnName;
        IsPrimaryKey = isPrimaryKey;
    }
}