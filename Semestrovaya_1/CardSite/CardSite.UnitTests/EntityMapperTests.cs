using CardSite.Models;
using CardSite.ORM;
using CardSite.ORM.Attributes;
using NUnit.Framework;

namespace CardSite.UnitTests;

[TestFixture]
public class EntityMapperTests
{
    private EntityMapper mapper = null!;

    [SetUp]
    public void SetUp()
    {
        mapper = new EntityMapper();
    }

    [Test]
    public void GetMetadata_ReturnsCorrectTableName()
    {
        var metadata = mapper.GetMetadata<Card>();

        Assert.That(metadata.TableName, Is.EqualTo("cards"));
    }

    [Test]
    public void GetMetadata_ReturnsCorrectColumnNames()
    {
        var metadata = mapper.GetMetadata<Card>();

        Assert.That(
            metadata.Properties.Any(p =>
                p.Property.Name == "Id" &&
                p.ColumnName == "id"),
            Is.True);

        Assert.That(
            metadata.Properties.Any(p =>
                p.Property.Name == "Name" &&
                p.ColumnName == "name"),
            Is.True);

        Assert.That(
            metadata.Properties.Any(p =>
                p.Property.Name == "CardNumber" &&
                p.ColumnName == "cardnumber"),
            Is.True);
    }

    [Test]
    public void GetMetadata_ReturnsCorrectPrimaryKey()
    {
        var metadata = mapper.GetMetadata<Card>();

        Assert.That(metadata.PrimaryKey.Property.Name, Is.EqualTo("Id"));
        Assert.That(metadata.PrimaryKey.ColumnName, Is.EqualTo("id"));
        Assert.That(metadata.PrimaryKey.IsPrimaryKey, Is.True);
    }

    [Test]
    public void GetMetadata_DetectsNavigationProperties()
    {
        var metadata = mapper.GetMetadata<Card>();

        Assert.That(metadata.Navigations, Has.Count.EqualTo(4));

        Assert.That(
            metadata.Navigations.Any(n =>
                n.Property.Name == "Color" &&
                n.ForeignKeyProperty == "ColorId"),
            Is.True);

        Assert.That(
            metadata.Navigations.Any(n =>
                n.Property.Name == "Set" &&
                n.ForeignKeyProperty == "SetId"),
            Is.True);

        Assert.That(
            metadata.Navigations.Any(n =>
                n.Property.Name == "Type" &&
                n.ForeignKeyProperty == "TypeId"),
            Is.True);

        Assert.That(
            metadata.Navigations.Any(n =>
                n.Property.Name == "Rarity" &&
                n.ForeignKeyProperty == "RarityId"),
            Is.True);
    }

    [Test]
    public void GetMetadata_DetectsCollectionNavigations()
    {
        var metadata = mapper.GetMetadata<Card>();

        Assert.That(
            metadata.CollectionNavigations,
            Has.Count.EqualTo(2));

        Assert.That(
            metadata.CollectionNavigations.Any(n =>
                n.Property.Name == "SpecialTraits"),
            Is.True);

        Assert.That(
            metadata.CollectionNavigations.Any(n =>
                n.Property.Name == "Products"),
            Is.True);
    }

    [Test]
    public void GetMetadata_WithoutTableAttribute_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => mapper.GetMetadata<NoTableEntity>());
    }

    [Test]
    public void GetMetadata_WithoutPrimaryKey_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => mapper.GetMetadata<NoPrimaryKeyEntity>());
    }

    private class NoTableEntity
    {
        public int Id { get; set; }
    }

    [Table("test_entities")]
    private class NoPrimaryKeyEntity
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}