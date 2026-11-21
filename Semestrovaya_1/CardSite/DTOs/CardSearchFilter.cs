public class CardSearchFilter
{
    public string? Keyword { get; set; }
    public string? SetCode { get; set; }

    public List<string> Types { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public List<string> Rarities { get; set; } = new();

    public int? CostMin { get; set; }
    public int? CostMax { get; set; }

    public int? PowerMin { get; set; }
    public int? PowerMax { get; set; }

    public int? ComboPowerMin { get; set; }
    public int? ComboPowerMax { get; set; }

    public List<string> SpecialTraits { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}