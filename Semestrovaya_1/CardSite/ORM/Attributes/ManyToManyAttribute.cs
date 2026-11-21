namespace CardSite.ORM.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ManyToManyAttribute : Attribute
{
    public string JoinTable { get; }

    public string SourceForeignKey { get; }

    public string TargetForeignKey { get; }

    public ManyToManyAttribute(
        string joinTable,
        string sourceForeignKey,
        string targetForeignKey)
    {
        JoinTable = joinTable;
        SourceForeignKey = sourceForeignKey;
        TargetForeignKey = targetForeignKey;
    }
}