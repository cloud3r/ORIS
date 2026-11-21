using System.Linq.Expressions;
using System.Reflection;
using CardSite.Models;
using CardSite.ORM;
using NUnit.Framework;

namespace CardSite.UnitTests;

[TestFixture]
public class DbSetTests
{
    private DbSet<Card> dbSet = null!;

    [SetUp]
    public void SetUp()
    {
        var connectionFactory = new DbConnectionFactory(
            "Host=localhost;Port=5432;Database=cardsitetestdb;Username=postgres;Password=1337"
        );

        var mapper = new EntityMapper();
        var queryBuilder = new QueryBuilder();

        dbSet = new DbSet<Card>(
            connectionFactory,
            mapper,
            queryBuilder
        );
    }

    private object? GetPrivateField(string fieldName)
    {
        var field = typeof(DbSet<Card>).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);

        return field!.GetValue(dbSet);
    }

    [Test]
    public void Include_WithNavigationProperty_AddsNavigation()
    {
        var result = dbSet.Include("Color");

        var included = (List<string>)GetPrivateField(
            "includedNavigations"
        )!;

        Assert.That(result, Is.SameAs(dbSet));
        Assert.That(included, Does.Contain("Color"));
    }

    [Test]
    public void Include_WithCollectionNavigation_AddsNavigation()
    {
        dbSet.Include("SpecialTraits");

        var included = (List<string>)GetPrivateField(
            "includedNavigations"
        )!;

        Assert.That(included, Does.Contain("SpecialTraits"));
    }

    [Test]
    public void Include_SameNavigationTwice_AddsOnlyOnce()
    {
        dbSet.Include("Color");
        dbSet.Include("Color");

        var included = (List<string>)GetPrivateField(
            "includedNavigations"
        )!;

        Assert.That(
            included.Count(n => n == "Color"),
            Is.EqualTo(1)
        );
    }

    [Test]
    public void Include_WithUnknownProperty_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => dbSet.Include("UnknownProperty")
        );
    }

    [Test]
    public void Include_WithExpression_AddsNavigation()
    {
        dbSet.Include(card => card.Color);

        var included = (List<string>)GetPrivateField(
            "includedNavigations"
        )!;

        Assert.That(included, Does.Contain("Color"));
    }

    [Test]
    public void Where_FirstPredicate_SavesExpression()
    {
        var result = dbSet.Where(card => card.Id > 10);

        var expression =
            (Expression<Func<Card, bool>>)GetPrivateField(
                "whereExpression"
            )!;

        Assert.That(result, Is.SameAs(dbSet));
        Assert.That(expression, Is.Not.Null);

        var predicate = expression.Compile();

        Assert.That(
            predicate(new Card { Id = 15 }),
            Is.True
        );

        Assert.That(
            predicate(new Card { Id = 5 }),
            Is.False
        );
    }

    [Test]
    public void Where_MultiplePredicates_CombinesWithAnd()
    {
        dbSet
            .Where(card => card.Id > 10)
            .Where(card => card.Name == "Goku");

        var expression =
            (Expression<Func<Card, bool>>)GetPrivateField(
                "whereExpression"
            )!;

        var predicate = expression.Compile();

        Assert.That(
            predicate(new Card
            {
                Id = 15,
                Name = "Goku"
            }),
            Is.True
        );

        Assert.That(
            predicate(new Card
            {
                Id = 5,
                Name = "Goku"
            }),
            Is.False
        );

        Assert.That(
            predicate(new Card
            {
                Id = 15,
                Name = "Vegeta"
            }),
            Is.False
        );
    }

    [Test]
    public void OrderBy_SavesPropertyName()
    {
        var result = dbSet.OrderBy(card => card.CardNumber);

        Assert.That(result, Is.SameAs(dbSet));

        Assert.That(
            GetPrivateField("orderByColumn"),
            Is.EqualTo("CardNumber")
        );

        Assert.That(
            GetPrivateField("orderByDescending"),
            Is.False
        );
    }

    [Test]
    public void OrderByDescending_SavesPropertyName()
    {
        var result = dbSet.OrderByDescending(
            card => card.CardNumber
        );

        Assert.That(result, Is.SameAs(dbSet));

        Assert.That(
            GetPrivateField("orderByColumn"),
            Is.EqualTo("CardNumber")
        );

        Assert.That(
            GetPrivateField("orderByDescending"),
            Is.True
        );
    }

    [Test]
    public void OrderBy_WithInvalidExpression_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => dbSet.OrderBy(card => card.Id + 1)
        );
    }

    [Test]
    public void OrderBy_WithUnknownProperty_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => dbSet.OrderBy(card => card.Name.Length)
        );
    }

    [Test]
    public void Skip_WithPositiveValue_SavesCount()
    {
        var result = dbSet.Skip(10);

        Assert.That(result, Is.SameAs(dbSet));

        Assert.That(
            GetPrivateField("skipCount"),
            Is.EqualTo(10)
        );
    }

    [Test]
    public void Skip_WithNegativeValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => dbSet.Skip(-1)
        );
    }

    [Test]
    public void Take_WithPositiveValue_SavesCount()
    {
        var result = dbSet.Take(20);

        Assert.That(result, Is.SameAs(dbSet));

        Assert.That(
            GetPrivateField("takeCount"),
            Is.EqualTo(20)
        );
    }

    [Test]
    public void Take_WithNegativeValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => dbSet.Take(-1)
        );
    }

    [Test]
    public void Skip_WithZeroValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => dbSet.Skip(0));

        Assert.That(
            GetPrivateField("skipCount"),
            Is.EqualTo(0)
        );
    }

    [Test]
    public void Take_WithZeroValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => dbSet.Take(0));

        Assert.That(
            GetPrivateField("takeCount"),
            Is.EqualTo(0)
        );
    }
}