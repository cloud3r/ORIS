namespace CardSite.ORM.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public string ForeignKeyProperty { get; }

    public ForeignKeyAttribute(string foreignKeyProperty)
    {
        ForeignKeyProperty = foreignKeyProperty;
    }
}