using CardSite.Models;
using CardSite.ORM;

namespace CardSite.UnitTests;

[TestFixture]
[Category("Integration")]
public class DbSetIntegrationTests
{
    private CardSiteContext context = null!;
    private DbSet<CardSet> cardSets = null!;

    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=cardsitetestdb;Username=postgres;Password=1337";

    [SetUp]
    public void SetUp()
    {
        context = new CardSiteContext(ConnectionString);
        cardSets = context.CardSets;
    }

    [Test]
    public async Task OrderBy_ShouldSortRecordsAscending()
    {
        var first = new CardSet
        {
            Name = "Integration Sort A",
            Code = $"SA_{Guid.NewGuid():N}"[..15]
        };

        var second = new CardSet
        {
            Name = "Integration Sort B",
            Code = $"SB_{Guid.NewGuid():N}"[..15]
        };

        var third = new CardSet
        {
            Name = "Integration Sort C",
            Code = $"SC_{Guid.NewGuid():N}"[..15]
        };

        var records = new List<CardSet> { third, first, second };

        try
        {
            foreach (var record in records)
                await cardSets.AddAsync(record);

            var result = await cardSets
                .Where(x =>
                    x.Code == first.Code ||
                    x.Code == second.Code ||
                    x.Code == third.Code)
                .OrderBy(x => x.Name)
                .ToListAsync();

            Assert.That(
                result.Select(x => x.Name),
                Is.EqualTo(new[]
                {
                    "Integration Sort A",
                    "Integration Sort B",
                    "Integration Sort C"
                })
            );
        }
        finally
        {
            foreach (var record in records)
            {
                if (record.Id > 0)
                    await cardSets.RemoveAsync(record.Id);
            }
        }
    }

    [Test]
    public async Task OrderByDescending_ShouldSortRecordsDescending()
    {
        var first = new CardSet
        {
            Name = "Integration Desc A",
            Code = $"DA_{Guid.NewGuid():N}"[..15]
        };

        var second = new CardSet
        {
            Name = "Integration Desc B",
            Code = $"DB_{Guid.NewGuid():N}"[..15]
        };

        var third = new CardSet
        {
            Name = "Integration Desc C",
            Code = $"DC_{Guid.NewGuid():N}"[..15]
        };

        var records = new List<CardSet> { first, second, third };

        try
        {
            foreach (var record in records)
                await cardSets.AddAsync(record);

            var result = await cardSets
                .Where(x =>
                    x.Code == first.Code ||
                    x.Code == second.Code ||
                    x.Code == third.Code)
                .OrderByDescending(x => x.Name)
                .ToListAsync();

            Assert.That(
                result.Select(x => x.Name),
                Is.EqualTo(new[]
                {
                    "Integration Desc C",
                    "Integration Desc B",
                    "Integration Desc A"
                })
            );
        }
        finally
        {
            foreach (var record in records)
            {
                if (record.Id > 0)
                    await cardSets.RemoveAsync(record.Id);
            }
        }
    }

    [Test]
    public async Task Take_ShouldLimitNumberOfRecords()
    {
        var records = Enumerable.Range(1, 3)
            .Select(i => new CardSet
            {
                Name = $"Integration Take {i}",
                Code = $"TK_{Guid.NewGuid():N}"[..15]
            })
            .ToList();

        try
        {
            foreach (var record in records)
                await cardSets.AddAsync(record);

            var result = await cardSets
                .Where(x =>
                    x.Code == records[0].Code ||
                    x.Code == records[1].Code ||
                    x.Code == records[2].Code)
                .Take(2)
                .ToListAsync();

            Assert.That(result, Has.Count.EqualTo(2));
        }
        finally
        {
            foreach (var record in records)
            {
                if (record.Id > 0)
                    await cardSets.RemoveAsync(record.Id);
            }
        }
    }

    [Test]
    public async Task SkipAndTake_ShouldReturnCorrectPage()
    {
        var records = Enumerable.Range(1, 5)
            .Select(i => new CardSet
            {
                Name = $"Integration Page {i}",
                Code = $"PG_{Guid.NewGuid():N}"[..15]
            })
            .ToList();

        try
        {
            foreach (var record in records)
                await cardSets.AddAsync(record);

            var result = await cardSets
                .Where(x =>
                    x.Code == records[0].Code ||
                    x.Code == records[1].Code ||
                    x.Code == records[2].Code ||
                    x.Code == records[3].Code ||
                    x.Code == records[4].Code)
                .OrderBy(x => x.Name)
                .Skip(2)
                .Take(2)
                .ToListAsync();

            Assert.That(result, Has.Count.EqualTo(2));

            Assert.That(
                result[0].Name,
                Is.EqualTo("Integration Page 3")
            );

            Assert.That(
                result[1].Name,
                Is.EqualTo("Integration Page 4")
            );
        }
        finally
        {
            foreach (var record in records)
            {
                if (record.Id > 0)
                    await cardSets.RemoveAsync(record.Id);
            }
        }
    }
}