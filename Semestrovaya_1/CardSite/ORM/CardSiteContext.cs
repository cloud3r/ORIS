using CardSite.Models;

namespace CardSite.ORM;

public class CardSiteContext
{
    private readonly DbConnectionFactory connectionFactory;
    private readonly EntityMapper mapper;
    private readonly QueryBuilder queryBuilder;

    public DbSet<SpecialTrait> SpecialTraits =>
        new(connectionFactory, mapper, queryBuilder);

    public DbSet<Card> Cards =>
        new(connectionFactory, mapper, queryBuilder);

    public DbSet<CardSet> CardSets =>
        new(connectionFactory, mapper, queryBuilder);

    public DbSet<CardType> CardTypes =>
        new(connectionFactory, mapper, queryBuilder);
    
    public DbSet<CardColor> CardColors =>
        new(connectionFactory, mapper, queryBuilder);
    public DbSet<CardRarity> CardRarities =>
        new(connectionFactory, mapper, queryBuilder);
    
    public DbSet<AdminUser> AdminUsers => new(connectionFactory, mapper, queryBuilder);
    
    public CardSiteContext(string connectionString)
    {
        connectionFactory = new DbConnectionFactory(connectionString);
        mapper = new EntityMapper();
        queryBuilder = new QueryBuilder();
    }
}