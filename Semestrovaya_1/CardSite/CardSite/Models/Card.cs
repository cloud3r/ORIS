using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("cards")]
public class Card
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("cardnumber")]
    public string CardNumber { get; set; } = string.Empty;
    
    [Column("colorid")]
    public int ColorId { get; set; }

    [ForeignKey(nameof(ColorId))]
    public CardColor? Color { get; set; }
    
    [Column("specifiedcost")]
    public string? SpecifiedCost { get; set; }
    
    [Column("skills")]
    public string? Skills { get; set; }
    
    [Column("combopower")]
    public int? ComboPower { get; set; }

    [Column("imageurl")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("cost")]
    public int? Cost { get; set; }

    [Column("power")]
    public int? Power { get; set; }
    
    [Column("setid")]
    public int SetId { get; set; }
    
    [Column("typeid")]
    public int TypeId { get; set; }
    
    [Column("rarityid")]
    public int RarityId { get; set; }
    
    [ForeignKey(nameof(SetId))]
    public CardSet? Set { get; set; }

    [ForeignKey(nameof(TypeId))]
    public CardType? Type { get; set; }

    [ForeignKey(nameof(RarityId))]
    public CardRarity? Rarity { get; set; }

    [ManyToMany(
        "cardspecialtraits",
        "cardid",
        "specialtraitid")]
    public List<SpecialTrait> SpecialTraits { get; set; } = new();
    
    [ManyToMany(
        "cardproducts",
        "cardid",
        "productid")]
    public List<Product> Products { get; set; } = new();
}