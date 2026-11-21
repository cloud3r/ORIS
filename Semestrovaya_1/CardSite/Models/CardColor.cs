using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("cardcolors")]
public class CardColor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}