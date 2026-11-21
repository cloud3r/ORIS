using System.Text.RegularExpressions;
using CardSite.DTOs;

namespace CardSite.Services;

public static class CardFormValidator
{
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{N}\s'’().,:!?+-]{1,100}$",
        RegexOptions.Compiled);

    private static readonly Regex CardNumberRegex = new(
        @"^[A-Z]{2,4}\d{2}-\d{3}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Dictionary<string, string> Validate(
        string? name,
        string? cardNumber)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название карты обязательно.";
        }
        else if (!NameRegex.IsMatch(name.Trim()))
        {
            errors["name"] =
                "Название должно содержать от 1 до 100 допустимых символов.";
        }

        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            errors["cardNumber"] = "Номер карты обязателен.";
        }
        else if (!CardNumberRegex.IsMatch(cardNumber.Trim()))
        {
            errors["cardNumber"] =
                "Неверный формат номера. Например: FB11-001.";
        }

        return errors;
    }
    
    public static Dictionary<string, string> Validate(CreateCardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = Validate(request.Name, request.CardNumber);

        if (request.ColorId <= 0)
            errors["colorId"] = "Необходимо выбрать цвет карты.";

        if (request.TypeId <= 0)
            errors["typeId"] = "Необходимо выбрать тип карты.";

        if (request.RarityId <= 0)
            errors["rarityId"] = "Необходимо выбрать редкость.";

        if (request.SetId <= 0)
            errors["setId"] = "Необходимо выбрать набор.";

        if (request.Cost.HasValue &&
            (request.Cost.Value < 0 || request.Cost.Value > 9))
        {
            errors["cost"] = "Стоимость должна быть от 0 до 9.";
        }

        if (request.Power.HasValue &&
            (request.Power.Value < 0 || request.Power.Value > 55000))
        {
            errors["power"] = "Сила должна быть от 0 до 55000.";
        }

        if (request.ComboPower.HasValue &&
            (request.ComboPower.Value < 0 || request.ComboPower.Value > 10000))
        {
            errors["comboPower"] = "Сила комбо должна быть от 0 до 10000.";
        }

        if (string.IsNullOrWhiteSpace(request.ImageUrl))
            errors["imageUrl"] = "Необходимо указать изображение карты.";

        return errors;
    }
}