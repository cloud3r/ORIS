using CardSite.DTOs;
using CardSite.Models;

namespace CardSite.Services.Interfaces;

public interface ICardService
{
    Task<List<Card>> GetCardsBySetAsync(string setCode);
    Task<Card?> GetCardByIdAsync(int id);
    Task<List<Card>> GetAllCardsAsync();

    Task<List<Card>> SearchCardsAsync(
        string setCode,
        string? keyword,
        int? typeId,
        int? colorId,
        int? rarityId,
        int? costMin,
        int? costMax,
        int? powerMin,
        int? powerMax);

    Task<List<Card>> SearchCardsAsync(
        string? setCode,
        string? keyword,
        int? typeId,
        int? colorId,
        int? rarityId,
        int? costMin,
        int? costMax,
        int? powerMin,
        int? powerMax,
        int? comboPowerMin,
        int? comboPowerMax,
        IReadOnlyCollection<int>? specialTraitIds);

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

    Task<List<CardSet>> GetAllSetsAsync();
    Task<List<SpecialTrait>> GetAllSpecialTraitsAsync();
    Task<List<CardColor>> GetAllColorsAsync();
    Task<List<CardType>> GetAllTypesAsync();
    Task<List<CardRarity>> GetAllRaritiesAsync();

    Task CreateCardAsync(CreateCardRequest request);
    Task UpdateCardAsync(int id, CreateCardRequest request);
    Task DeleteCardAsync(int id);

    Task<Dictionary<string, string>> ValidateReferencesAsync(
        CreateCardRequest request);

    Task<Dictionary<string, string>> ValidateCreateCardAsync(
        CreateCardRequest request);
}