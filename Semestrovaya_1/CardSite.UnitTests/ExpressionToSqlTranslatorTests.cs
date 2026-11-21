using CardSite.Models;
using CardSite.ORM;
using NUnit.Framework;

namespace CardSite.UnitTests;

[TestFixture]
public class ExpressionToSqlTranslatorTests
{
    private EntityMapper mapper = null!;

    [SetUp]
    public void SetUp()
    {
        mapper = new EntityMapper();
    }

    private ExpressionToSqlTranslator<Card> CreateTranslator()
    {
        var metadata = mapper.GetMetadata<Card>();

        return new ExpressionToSqlTranslator<Card>(metadata, mapper);
    }

    [Test]
    public void Translate_GreaterThan_ReturnsCorrectSql()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(c => c.Cost > 3);

        Assert.That(result.Sql, Does.Contain("\"cost\""));
        Assert.That(result.Sql, Does.Contain(">"));
        Assert.That(result.Sql, Does.Contain("@p0"));

        Assert.That(result.Parameters, Has.Count.EqualTo(1));
        Assert.That(result.Parameters[0].Value, Is.EqualTo(3));
    }

    [Test]
    public void Translate_StringEquality_ReturnsCorrectSql()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(c => c.Name == "Goku");

        Assert.That(result.Sql, Does.Contain("\"name\""));
        Assert.That(result.Sql, Does.Contain("="));
        Assert.That(result.Sql, Does.Contain("@p0"));

        Assert.That(result.Sql, Does.Not.Contain("Goku"));
        Assert.That(result.Parameters, Has.Count.EqualTo(1));
        Assert.That(result.Parameters[0].Value, Is.EqualTo("Goku"));
    }

    [Test]
    public void Translate_AndAlso_ReturnsCorrectSql()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.Cost > 3 && c.Power > 10000);

        Assert.That(result.Sql, Does.Contain("AND"));
        Assert.That(result.Sql, Does.Contain("\"cost\""));
        Assert.That(result.Sql, Does.Contain("\"power\""));

        Assert.That(result.Parameters, Has.Count.EqualTo(2));
        Assert.That(result.Parameters[0].Value, Is.EqualTo(3));
        Assert.That(result.Parameters[1].Value, Is.EqualTo(10000));
    }

    [Test]
    public void Translate_OrElse_ReturnsCorrectSql()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.Cost == 3 || c.Cost == 5);

        Assert.That(result.Sql, Does.Contain("OR"));
        Assert.That(result.Parameters, Has.Count.EqualTo(2));
        Assert.That(result.Parameters[0].Value, Is.EqualTo(3));
        Assert.That(result.Parameters[1].Value, Is.EqualTo(5));
    }

    [Test]
    public void Translate_NullEquality_ReturnsIsNull()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.SpecifiedCost == null);

        Assert.That(result.Sql, Does.Contain("\"specifiedcost\""));
        Assert.That(result.Sql, Does.Contain("IS NULL"));
        Assert.That(result.Parameters, Is.Empty);
    }

    [Test]
    public void Translate_StringContains_ReturnsLike()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.Name.Contains("Goku"));

        Assert.That(result.Sql, Does.Contain("LIKE"));
        Assert.That(result.Sql, Does.Contain("\"name\""));
        Assert.That(result.Sql, Does.Contain("@p0"));

        Assert.That(result.Sql, Does.Not.Contain("Goku"));
        Assert.That(result.Parameters, Has.Count.EqualTo(1));
        Assert.That(result.Parameters[0].Value, Is.EqualTo("%Goku%"));
    }

    [Test]
    public void Translate_StringStartsWith_ReturnsLike()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.Name.StartsWith("Son"));

        Assert.That(result.Sql, Does.Contain("LIKE"));
        Assert.That(result.Parameters, Has.Count.EqualTo(1));
        Assert.That(result.Parameters[0].Value, Is.EqualTo("Son%"));
    }

    [Test]
    public void Translate_StringEndsWith_ReturnsLike()
    {
        var translator = CreateTranslator();

        var result = translator.Translate(
            c => c.Name.EndsWith("Goku"));

        Assert.That(result.Sql, Does.Contain("LIKE"));
        Assert.That(result.Parameters, Has.Count.EqualTo(1));
        Assert.That(result.Parameters[0].Value, Is.EqualTo("%Goku"));
    }

    [Test]
    public void Translate_CollectionContains_ReturnsIn()
    {
        var translator = CreateTranslator();

        int[] ids = { 1, 2, 3 };

        var result = translator.Translate(
            c => ids.Contains(c.Id));

        Assert.That(result.Sql, Does.Contain("IN"));
        Assert.That(result.Sql, Does.Contain("\"id\""));

        Assert.That(result.Parameters, Has.Count.EqualTo(3));
        Assert.That(result.Parameters[0].Value, Is.EqualTo(1));
        Assert.That(result.Parameters[1].Value, Is.EqualTo(2));
        Assert.That(result.Parameters[2].Value, Is.EqualTo(3));
    }

    [Test]
    public void Translate_EmptyCollection_ReturnsFalse()
    {
        var translator = CreateTranslator();

        int[] ids = Array.Empty<int>();

        var result = translator.Translate(
            c => ids.Contains(c.Id));

        Assert.That(result.Sql, Is.EqualTo("FALSE"));
        Assert.That(result.Parameters, Is.Empty);
    }
}