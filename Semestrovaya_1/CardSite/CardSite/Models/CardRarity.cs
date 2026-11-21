using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("cardrarities")]
public class CardRarity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = string.Empty;
}