namespace CardSite.ORM.Metadata;

public class NavigationJoinInfo
{
    public NavigationMetadata Navigation { get; }

    public EntityMetadata RelatedMetadata { get; }

    public string Alias { get; }

    public string ForeignKeyColumn { get; }

    public string RelatedPrimaryKeyColumn { get; }

    public NavigationJoinInfo(
        NavigationMetadata navigation,
        EntityMetadata relatedMetadata,
        string alias,
        string foreignKeyColumn,
        string relatedPrimaryKeyColumn)
    {
        Navigation = navigation;
        RelatedMetadata = relatedMetadata;
        Alias = alias;
        ForeignKeyColumn = foreignKeyColumn;
        RelatedPrimaryKeyColumn = relatedPrimaryKeyColumn;
    }
}