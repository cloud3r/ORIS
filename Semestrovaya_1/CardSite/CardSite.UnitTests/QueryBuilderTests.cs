using CardSite.Models;
using CardSite.ORM;
using NUnit.Framework;

namespace CardSite.UnitTests;

[TestFixture]
public class QueryBuilderTests
{
    private QueryBuilder queryBuilder = null!;
    private EntityMapper mapper = null!;

    [SetUp]
    public void SetUp()
    {
        queryBuilder = new QueryBuilder();
        mapper = new EntityMapper();
    }

    [Test]
    public void BuildSelectAll_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildSelectAll(metadata);

        Assert.That(sql, Does.StartWith("SELECT "));
        Assert.That(sql, Does.Contain("FROM \"cards\""));
        Assert.That(sql, Does.EndWith(";"));

        Assert.That(sql, Does.Contain("\"id\""));
        Assert.That(sql, Does.Contain("\"name\""));
        Assert.That(sql, Does.Contain("\"cardnumber\""));
    }

    [Test]
    public void BuildSelectById_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildSelectById(metadata);

        Assert.That(
            sql,
            Does.Contain("WHERE \"id\" = @id")
        );

        Assert.That(sql, Does.Contain("FROM \"cards\""));
        Assert.That(sql, Does.EndWith(";"));
    }

    [Test]
    public void BuildInsert_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildInsert(metadata);

        Assert.That(sql, Does.StartWith("INSERT INTO \"cards\""));

        Assert.That(sql, Does.Contain("\"name\""));
        Assert.That(sql, Does.Contain("\"cardnumber\""));

        Assert.That(sql, Does.Contain("@Name"));
        Assert.That(sql, Does.Contain("@CardNumber"));

        Assert.That(sql, Does.Not.Contain("@Id"));

        Assert.That(sql, Does.Contain("RETURNING \"id\""));
        Assert.That(sql, Does.EndWith(";"));
    }

    [Test]
    public void BuildUpdate_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildUpdate(metadata);

        Assert.That(sql, Does.StartWith("UPDATE \"cards\""));

        Assert.That(sql, Does.Contain("SET "));
        Assert.That(sql, Does.Contain("\"name\" = @Name"));
        Assert.That(sql, Does.Contain("\"cardnumber\" = @CardNumber"));

        Assert.That(sql, Does.Contain("WHERE \"id\" = @id"));

        Assert.That(sql, Does.Not.Contain("\"id\" = @Id"));

        Assert.That(sql, Does.EndWith(";"));
    }

    [Test]
    public void BuildDelete_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildDelete(metadata);

        Assert.That(sql, Does.StartWith("DELETE FROM \"cards\""));

        Assert.That(
            sql,
            Does.Contain("WHERE \"id\" = @id")
        );

        Assert.That(sql, Does.EndWith(";"));
    }

    [Test]
    public void QuoteIdentifier_EscapesDoubleQuotes()
    {
        string result = queryBuilder.QuoteIdentifier("card\"name");

        Assert.That(result, Is.EqualTo("\"card\"\"name\""));
    }

    [Test]
    public void BuildSelectAll_WithWhereAndOrderBy_ReturnsCorrectSql()
    {
        var metadata = mapper.GetMetadata<Card>();

        string sql = queryBuilder.BuildSelectAll(
            metadata,
            "\"cost\" > @cost",
            "\"name\" ASC"
        );

        Assert.That(sql, Does.Contain("FROM \"cards\""));
        Assert.That(sql, Does.Contain("WHERE \"cost\" > @cost"));
        Assert.That(sql, Does.Contain("ORDER BY \"name\" ASC"));

        Assert.That(sql, Does.EndWith(";"));
    }
}