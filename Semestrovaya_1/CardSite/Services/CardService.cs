using CardSite.DTOs;
using CardSite.Models;
using CardSite.Repositories.Interfaces;
using CardSite.Services.Interfaces;

namespace CardSite.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;

    public CardService(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public Task<List<Card>> GetCardsBySetAsync(string setCode)
    {
        return _cardRepository.GetCardsBySetAsync(setCode);
    }

    public Task<Card?> GetCardByIdAsync(int id)
    {
        return _cardRepository.GetByIdAsync(id);
    }

    public Task<List<Card>> SearchCardsAsync(
        string setCode,
        string? keyword,
        int? typeId,
        int? colorId,
        int? rarityId,
        int? costMin,
        int? costMax,
        int? powerMin,
        int? powerMax)
    {
        return SearchCardsAsync(
            setCode,
            keyword,
            typeId,
            colorId,
            rarityId,
            costMin,
            costMax,
            powerMin,
            powerMax,
            null,
            null,
            null);
    }

    public Task<List<Card>> SearchCardsAsync(
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
        IReadOnlyCollection<int>? specialTraitIds)
    {
        return SearchCardsWithFiltersAsync(
            setCode,
            keyword,
            typeId.HasValue ? new[] { typeId.Value } : null,
            colorId.HasValue ? new[] { colorId.Value } : null,
            rarityId.HasValue ? new[] { rarityId.Value } : null,
            costMin,
            costMax,
            powerMin,
            powerMax,
            comboPowerMin,
            comboPowerMax,
            specialTraitIds);
    }

    public Task<List<Card>> SearchCardsWithFiltersAsync(
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
        return _cardRepository.SearchCardsWithFiltersAsync(
            setCode,
            keyword,
            typeIds,
            colorIds,
            rarityIds,
            costMin,
            costMax,
            powerMin,
            powerMax,
            comboPowerMin,
            comboPowerMax,
            specialTraitIds);
    }

    public Task<List<CardSet>> GetAllSetsAsync()
    {
        return _cardRepository.GetAllSetsAsync();
    }

    public Task<List<SpecialTrait>> GetAllSpecialTraitsAsync()
    {
        return _cardRepository.GetAllSpecialTraitsAsync();
    }

    public async Task CreateCardAsync(CreateCardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var card = new Card
        {
            Name = request.Name.Trim(),
            CardNumber = request.CardNumber.Trim(),

            ColorId = request.ColorId,
            TypeId = request.TypeId,
            RarityId = request.RarityId,
            SetId = request.SetId,

            Cost = request.Cost,
            SpecifiedCost = request.SpecifiedCost,
            Power = request.Power,
            ComboPower = request.ComboPower,

            ImageUrl = request.ImageUrl.Trim(),
            Skills = request.Skills
        };

        var traits = await _cardRepository.GetSpecialTraitsByIdsAsync(
            request.SpecialTraitIds.Distinct().ToArray());

        if (traits.Count != request.SpecialTraitIds.Distinct().Count())
        {
            throw new InvalidOperationException(
                "Одна или несколько выбранных особенностей не существуют.");
        }

        card.SpecialTraits = traits;

        await _cardRepository.AddAsync(card);
    }

    public async Task<Dictionary<string, string>> ValidateReferencesAsync(
        CreateCardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string>();

        var colors = await _cardRepository.GetAllColorsAsync();

        if (!colors.Any(color => color.Id == request.ColorId))
            errors["colorId"] = "Выбранный цвет не существует.";

        var types = await _cardRepository.GetAllTypesAsync();

        if (!types.Any(type => type.Id == request.TypeId))
            errors["typeId"] = "Выбранный тип карты не существует.";

        var rarities = await _cardRepository.GetAllRaritiesAsync();

        if (!rarities.Any(rarity => rarity.Id == request.RarityId))
            errors["rarityId"] = "Выбранная редкость не существует.";

        var sets = await _cardRepository.GetAllSetsAsync();

        if (!sets.Any(set => set.Id == request.SetId))
            errors["setId"] = "Выбранный набор не существует.";

        var traitIds = request.SpecialTraitIds.Distinct().ToArray();

        if (traitIds.Any(id => id <= 0))
        {
            errors["specialTraitIds"] =
                "Выбрана некорректная характеристика.";
        }
        else
        {
            var traits = await _cardRepository.GetSpecialTraitsByIdsAsync(
                traitIds);

            if (traits.Count != traitIds.Length)
            {
                errors["specialTraitIds"] =
                    "Одна из выбранных характеристик не существует.";
            }
        }

        return errors;
    }

    public async Task<Dictionary<string, string>> ValidateCreateCardAsync(
        CreateCardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = CardFormValidator.Validate(request);

        var referenceErrors = await ValidateReferencesAsync(request);

        foreach (var error in referenceErrors)
            errors.TryAdd(error.Key, error.Value);

        return errors;
    }

    public Task<List<CardColor>> GetAllColorsAsync()
    {
        return _cardRepository.GetAllColorsAsync();
    }

    public Task<List<CardType>> GetAllTypesAsync()
    {
        return _cardRepository.GetAllTypesAsync();
    }

    public Task<List<CardRarity>> GetAllRaritiesAsync()
    {
        return _cardRepository.GetAllRaritiesAsync();
    }

    public Task<List<Card>> GetAllCardsAsync()
    {
        return _cardRepository.GetAllCardsAsync();
    }

    public async Task UpdateCardAsync(int id, CreateCardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var card = await _cardRepository.GetByIdAsync(id);

        if (card == null)
        {
            throw new InvalidOperationException(
                $"Card with ID {id} does not exist.");
        }

        card.Name = request.Name.Trim();
        card.CardNumber = request.CardNumber.Trim();

        card.ColorId = request.ColorId;
        card.TypeId = request.TypeId;
        card.RarityId = request.RarityId;
        card.SetId = request.SetId;

        card.Cost = request.Cost;
        card.SpecifiedCost = request.SpecifiedCost;
        card.Power = request.Power;
        card.ComboPower = request.ComboPower;

        card.ImageUrl = request.ImageUrl.Trim();
        card.Skills = request.Skills;

        var traits = await _cardRepository.GetSpecialTraitsByIdsAsync(
            request.SpecialTraitIds.Distinct().ToArray());

        if (traits.Count != request.SpecialTraitIds.Distinct().Count())
        {
            throw new InvalidOperationException(
                "Одна или несколько выбранных особенностей не существуют.");
        }

        card.SpecialTraits = traits;

        await _cardRepository.UpdateAsync(card);
    }

    public Task DeleteCardAsync(int id)
    {
        return _cardRepository.DeleteAsync(id);
    }
}