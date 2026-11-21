namespace CardSite.DTOs;

public class CreateCardRequest
{
    public string Name { get; set; } = string.Empty;

    public string CardNumber { get; set; } = string.Empty;

    public int ColorId { get; set; }

    public int TypeId { get; set; }

    public int RarityId { get; set; }

    public int SetId { get; set; }

    public int? Cost { get; set; }

    public string? SpecifiedCost { get; set; }

    public int? Power { get; set; }

    public int?  ComboPower { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? Skills { get; set; }
    
    public List<int> SpecialTraitIds { get; set; } = new();
}