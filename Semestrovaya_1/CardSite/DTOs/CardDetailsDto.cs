using System.Linq;
using CardSite.Models;

namespace CardSite.DTOs;

public class CardDetailsDto
{
    public Card Card { get; set; } = null!;

    public CardSet? Set { get; set; }

    public CardType? Type { get; set; }

    public CardRarity? Rarity { get; set; }

    public string SpecialTraitsText =>
        string.Join("/", Card.SpecialTraits.Select(t => t.Name));
}