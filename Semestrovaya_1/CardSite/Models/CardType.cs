using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("cardtypes")]
public class CardType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}