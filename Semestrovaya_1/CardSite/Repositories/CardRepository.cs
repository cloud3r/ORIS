using CardSite.DTOs;
using CardSite.Models;
using CardSite.ORM;
using CardSite.Repositories.Interfaces;

namespace CardSite.Repositories;

public class CardRepository : ICardRepository
{
    private readonly CardSiteContext _context;

    public CardRepository(CardSiteContext context)
    {
        _context = context;
    }

    public async Task<List<Card>> GetAllAsync()
    {
        return await GetAllCardsAsync();
    }

    public async Task<List<Card>> GetAllCardsAsync()
    {
        return await _context.Cards
            .Where(card => card.Id > 0)
            .Include(card => card.Set)
            .Include(card => card.Color)
            .Include(card => card.Type)
            .Include(card => card.Rarity)
            .Include(card => card.SpecialTraits)
            .OrderBy(card => card.Id)
            .ToListAsync();
    }

    public async Task<List<Card>> GetCardsBySetAsync(string setCode)
    {
        return await _context.Cards
            .Where(card => card.Set!.Code == setCode)
            .Include(card => card.Set)
            .Include(card => card.Color)
            .Include(card => card.Type)
            .Include(card => card.Rarity)
            .Include(card => card.SpecialTraits)
            .OrderBy(card => card.CardNumber)
            .ToListAsync();
    }

    public async Task<Card?> GetByIdAsync(int id)
    {
        return await _context.Cards
            .Where(card => card.Id == id)
            .Include(card => card.Set)
            .Include(card => card.Color)
            .Include(card => card.Type)
            .Include(card => card.Rarity)
            .Include(card => card.SpecialTraits)
            .Include(card => card.Products)
            .FirstOrDefaultAsync();
    }

    public async Task<CardDetailsDto?> GetDetailsByIdAsync(int id)
    {
        var card = await GetByIdAsync(id);

        if (card == null)
            return null;

        return new CardDetailsDto
        {
            Card = card,
            Set = card.Set,
            Type = card.Type,
            Rarity = card.Rarity
        };
    }

    public async Task<List<Card>> SearchCardsWithFiltersAsync(
        string? setCode,
        string? keyword,
        IReadOnlyCollection<int>? typeIds,
        IReadOnlyCollection<int>? colorIds,
        IReadOnlyCollection<int>? rarityIds,
        int? costMin,
        int? costMax,
        int? powerMin,
        int? powerMax,
        int? comboPowerMin,
        int? comboPowerMax,
        IReadOnlyCollection<int>? specialTraitIds)
    {
        var cards = string.IsNullOrWhiteSpace(setCode)
            ? await GetAllCardsAsync()
            : await GetCardsBySetAsync(setCode);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            cards = cards
                .Where(card =>
                    card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    card.CardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (typeIds is { Count: > 0 })
        {
            cards = cards
                .Where(card => typeIds.Contains(card.TypeId))
                .ToList();
        }

        if (colorIds is { Count: > 0 })
        {
            cards = cards
                .Where(card => colorIds.Contains(card.ColorId))
                .ToList();
        }

        if (rarityIds is { Count: > 0 })
        {
            cards = cards
                .Where(card => rarityIds.Contains(card.RarityId))
                .ToList();
        }

        if (costMin.HasValue)
        {
            cards = cards
                .Where(card => card.Cost.HasValue && card.Cost.Value >= costMin.Value)
                .ToList();
        }

        if (costMax.HasValue)
        {
            cards = cards
                .Where(card => card.Cost.HasValue && card.Cost.Value <= costMax.Value)
                .ToList();
        }

        if (powerMin.HasValue)
        {
            cards = cards
                .Where(card => card.Power.HasValue && card.Power.Value >= powerMin.Value)
                .ToList();
        }

        if (powerMax.HasValue)
        {
            cards = cards
                .Where(card => card.Power.HasValue && card.Power.Value <= powerMax.Value)
                .ToList();
        }

        if (comboPowerMin.HasValue)
        {
            cards = cards
                .Where(card => card.ComboPower.HasValue &&
                               card.ComboPower.Value >= comboPowerMin.Value)
                .ToList();
        }

        if (comboPowerMax.HasValue)
        {
            cards = cards
                .Where(card => card.ComboPower.HasValue &&
                               card.ComboPower.Value <= comboPowerMax.Value)
                .ToList();
        }

        if (specialTraitIds is { Count: > 0 })
        {
            cards = cards
                .Where(card => card.SpecialTraits.Any(
                    trait => specialTraitIds.Contains(trait.Id)))
                .ToList();
        }

        return cards
            .OrderBy(card => card.CardNumber)
            .ToList();
    }

    public async Task<int> AddAsync(Card card)
    {
        return await _context.Cards.AddAsync(card);
    }

    public async Task UpdateAsync(Card card)
    {
        await _context.Cards.UpdateWithRelationsAsync(card);
    }

    public async Task DeleteAsync(int id)
    {
        var card = await GetByIdAsync(id);

        if (card == null)
            throw new InvalidOperationException(
                $"Карточка с ID {id} не найдена.");

        await _context.Cards.RemoveWithRelationsAsync(card);
    }

    public async Task<List<CardSet>> GetAllSetsAsync()
    {
        return await _context.CardSets
            .OrderBy(set => set.Code)
            .ToListAsync();
    }

    public async Task<List<SpecialTrait>> GetAllSpecialTraitsAsync()
    {
        return await _context.SpecialTraits
            .OrderBy(trait => trait.Name)
            .ToListAsync();
    }

    public async Task<List<CardColor>> GetAllColorsAsync()
    {
        return await _context.CardColors.ToListAsync();
    }

    public async Task<List<CardType>> GetAllTypesAsync()
    {
        return await _context.CardTypes.ToListAsync();
    }

    public async Task<List<CardRarity>> GetAllRaritiesAsync()
    {
        return await _context.CardRarities.ToListAsync();
    }

    public async Task<List<SpecialTrait>> GetSpecialTraitsByIdsAsync(
        IReadOnlyCollection<int> ids)
    {
        var traits = new List<SpecialTrait>();

        foreach (var id in ids.Distinct())
        {
            var trait = await _context.SpecialTraits.FindAsync(id);

            if (trait != null)
                traits.Add(trait);
        }

        return traits;
    }
}