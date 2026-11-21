using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("cardsets")]
public class CardSet
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    public string Code { get; set; } = string.Empty;
}