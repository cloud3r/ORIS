using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("specialtraits")]
public class SpecialTrait
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [ManyToMany("cardspecialtraits", "specialtraitid", "cardid")]
    public List<Card> Cards { get; set; } = new();
}