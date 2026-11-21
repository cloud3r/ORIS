using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("imageurl")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("producturl")]
    public string? ProductUrl { get; set; }

    [ManyToMany(
        "cardproducts",
        "productid",
        "cardid")]
    public List<Card> Cards { get; set; } = new();
}