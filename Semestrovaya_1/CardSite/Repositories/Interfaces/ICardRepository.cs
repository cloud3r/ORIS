using CardSite.DTOs;
using CardSite.Models;

namespace CardSite.Repositories.Interfaces;

public interface ICardRepository
{
    Task<List<Card>> GetAllAsync();
    Task<List<Card>> GetAllCardsAsync();
    Task<List<Card>> GetCardsBySetAsync(string setCode);
    Task<Card?> GetByIdAsync(int id);
    Task<CardDetailsDto?> GetDetailsByIdAsync(int id);

    Task<List<Card>> SearchCardsWithFiltersAsync(
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
        IReadOnlyCollection<int>? specialTraitIds);

    Task<int> AddAsync(Card card);
    Task UpdateAsync(Card card);
    Task DeleteAsync(int id);

    Task<List<CardSet>> GetAllSetsAsync();
    Task<List<SpecialTrait>> GetAllSpecialTraitsAsync();
    Task<List<CardColor>> GetAllColorsAsync();
    Task<List<CardType>> GetAllTypesAsync();
    Task<List<CardRarity>> GetAllRaritiesAsync();

    Task<List<SpecialTrait>> GetSpecialTraitsByIdsAsync(
        IReadOnlyCollection<int> ids);
}