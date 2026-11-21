using System.Net;
using System.Text;
using CardSite.Services;
using MiniTemplateEngine.Interfaces;
using System.Globalization;
using CardSite.Services.Interfaces;
using CardSite.DTOs;
using CardSite.Models;
using CardSite.Server;

namespace CardSite.Controllers;

public class AdminController
{
    private readonly ICardService _cardService;
    private readonly IHtmlTemplateRenderer _templateRenderer;
    private readonly string _viewsPath;

    public AdminController(
        ICardService cardService,
        IHtmlTemplateRenderer templateRenderer,
        string viewsPath)
    {
        _cardService = cardService;
        _templateRenderer = templateRenderer;
        _viewsPath = viewsPath;
    }

    public async Task ShowCreateCardPageAsync(HttpListenerContext context)
    {
        try
        {
            var model = new
            {
                Colors = await _cardService.GetAllColorsAsync(),
                Types = await _cardService.GetAllTypesAsync(),
                Rarities = await _cardService.GetAllRaritiesAsync(),
                Sets = await _cardService.GetAllSetsAsync(),
                SpecialTraits = await _cardService.GetAllSpecialTraitsAsync()
            };

            string templatePath = Path.Combine(
                _viewsPath,
                "Admin",
                "CreateCard.html");

            string html = _templateRenderer.RenderFromFile(
                templatePath,
                model);

            byte[] buffer = Encoding.UTF8.GetBytes(html);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"AdminController error: {exception.Message}");

            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;

            byte[] buffer = Encoding.UTF8.GetBytes(
                "Не удалось загрузить страницу добавления карты.");

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        finally
        {
            context.Response.Close();
        }
    }
    
    public async Task ShowCardsAsync(HttpListenerContext context)
    {
        try
        {
            var model = new
            {
                Cards = await _cardService.GetAllCardsAsync()
            };

            string templatePath = Path.Combine(
                _viewsPath,
                "Admin",
                "Cards.html");

            string html = _templateRenderer.RenderFromFile(
                templatePath,
                model);

            byte[] buffer = Encoding.UTF8.GetBytes(html);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Show admin cards error: {exception}");

            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;

            byte[] buffer = Encoding.UTF8.GetBytes(
                "Не удалось загрузить список карт.");

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        finally
        {
            context.Response.Close();
        }
    }
    
    public async Task ShowEditCardPageAsync(HttpListenerContext context)
    {
    try
    {
        string? idValue = context.Request.QueryString["id"];

        if (!int.TryParse(idValue, out int cardId) || cardId <= 0)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.RedirectLocation = "/admin/cards";
            return;
        }

        Card? card = await _cardService.GetCardByIdAsync(cardId);

        if (card == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.RedirectLocation = "/admin/cards";
            return;
        }

        var model = new
        {
            Card = card,

            Colors = (await _cardService.GetAllColorsAsync())
                .Select(color => new
                {
                    color.Id,
                    color.Name,
                    SelectedAttribute = color.Id == card.ColorId ? " selected" : ""
                }).ToList(),

            Types = (await _cardService.GetAllTypesAsync())
                .Select(type => new
                {
                    type.Id,
                    type.Name,
                    SelectedAttribute = type.Id == card.TypeId ? " selected" : ""
                }).ToList(),

            Rarities = (await _cardService.GetAllRaritiesAsync())
                .Select(rarity => new
                {
                    rarity.Id,
                    rarity.Code,
                    SelectedAttribute = rarity.Id == card.RarityId ? " selected" : ""
                }).ToList(),

            Sets = (await _cardService.GetAllSetsAsync())
                .Select(set => new
                {
                    set.Id,
                    set.Name,
                    SelectedAttribute = set.Id == card.SetId ? " selected" : ""
                }).ToList(),

            SpecialTraits = (await _cardService.GetAllSpecialTraitsAsync())
                .Select(trait => new
                {
                    trait.Id,
                    trait.Name,
                    SelectedAttribute = card.SpecialTraits.Any(
                        selected => selected.Id == trait.Id) ? " checked" : ""
                }).ToList()
        };

        string templatePath = Path.Combine(
            _viewsPath,
            "Admin",
            "EditCard.html");

        string html = _templateRenderer.RenderFromFile(
            templatePath,
            model);

        byte[] buffer = Encoding.UTF8.GetBytes(html);

        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Show edit card error: {exception}");

        if (context.Response.OutputStream.CanWrite)
        {
            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;

            byte[] buffer = Encoding.UTF8.GetBytes(
                "Не удалось загрузить страницу редактирования.");

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
    }
    finally
    {
        context.Response.Close();
    }
}
    
    public async Task CreateCardAsync(HttpListenerContext context)
    {
        try
        {
        var formValues = await FormDataReader.ReadFormValuesAsync(context.Request);

        var form = formValues.ToDictionary(
            item => item.Key,
            item => item.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var errors = new Dictionary<string, string>();

        var request = new CreateCardRequest
        {
            Name = GetValue(form, "name"),
            CardNumber = GetValue(form, "cardNumber"),
            ImageUrl = GetValue(form, "imageUrl"),
            SpecifiedCost = GetNullableString(form, "specifiedCost"),
            Skills = GetNullableString(form, "skills")
        };
        
        if (formValues.TryGetValue("specialTraitIds", out var traitValues))
        {
            foreach (string value in traitValues)
            {
                if (!int.TryParse(value, out int traitId) || traitId <= 0)
                {
                    errors["specialTraitIds"] = "Invalid Special Trait selection.";
                    break;
                }

                if (!request.SpecialTraitIds.Contains(traitId))
                {
                    request.SpecialTraitIds.Add(traitId);
                }
            }
        }

        if (!TryGetInt(form, "colorId", out int colorId))
            errors["colorId"] = "Select a valid color.";

        if (!TryGetInt(form, "typeId", out int typeId))
            errors["typeId"] = "Select a valid card type.";

        if (!TryGetInt(form, "rarityId", out int rarityId))
            errors["rarityId"] = "Select a valid rarity.";

        if (!TryGetInt(form, "setId", out int setId))
            errors["setId"] = "Select a valid card set.";

        request.ColorId = colorId;
        request.TypeId = typeId;
        request.RarityId = rarityId;
        request.SetId = setId;

        if (!TryGetNullableInt(form, "cost", out int? cost))
            errors["cost"] = "Invalid cost.";

        if (!TryGetNullableInt(form, "power", out int? power))
            errors["power"] = "Invalid power.";

        if (!TryGetNullableInt(form, "comboPower", out int? comboPower))
            errors["comboPower"] = "Invalid combo power.";

        request.Cost = cost;
        request.Power = power;
        request.ComboPower = comboPower;

        if (errors.Count == 0)
        {
            errors = await _cardService.ValidateCreateCardAsync(request);
        }

        if (errors.Count > 0)
        {
            string errorList = string.Join(
                "",
                errors.Select(error =>
                    $"<li>{WebUtility.HtmlEncode(error.Value)}</li>"));

            await SendHtmlAsync(
                context,
                $"""
                <h1>Form Validation Errors</h1>
                <ul>{errorList}</ul>
                <a href="/admin">Back to Create Card</a>
                """,
                HttpStatusCode.BadRequest);

            return;
        }

        await _cardService.CreateCardAsync(request);

        context.Response.StatusCode = 302;
        context.Response.RedirectLocation = "/admin?success=1";
        context.Response.Close();
        return;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Create card error: {exception}");

        await SendHtmlAsync(
            context,
            """
            <h1>Failed to Create Card</h1>
            <a href="/admin">Back to Create Card</a>
            """,
            HttpStatusCode.InternalServerError);
    }
    finally
    {
        try
        {
            context.Response.Close();
        }
        catch
        {
        }
    }
}

private static string GetValue(
    Dictionary<string, string> form,
    string key)
{
    return form.TryGetValue(key, out string? value)
        ? value.Trim()
        : string.Empty;
}

private static bool TryGetInt(
    Dictionary<string, string> form,
    string key,
    out int value)
{
    value = 0;

    return form.TryGetValue(key, out string? rawValue)
        && int.TryParse(
            rawValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
}

private static async Task SendHtmlAsync(
    HttpListenerContext context,
    string html,
    HttpStatusCode statusCode)
{
    byte[] buffer = Encoding.UTF8.GetBytes(
        $"""
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <title>CardSite Admin</title>
        </head>
        <body>
            {html}
        </body>
        </html>
        """);

    context.Response.StatusCode = (int)statusCode;
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength64 = buffer.Length;

    await context.Response.OutputStream.WriteAsync(buffer);
}
private static string? GetNullableString(
    Dictionary<string, string> form,
    string key)
{
    string value = GetValue(form, key);

    return string.IsNullOrWhiteSpace(value)
        ? null
        : value;
}

private static bool TryGetNullableInt(
    Dictionary<string, string> form,
    string key,
    out int? value)
{
    value = null;

    if (!form.TryGetValue(key, out string? rawValue))
        return true;

    if (string.IsNullOrWhiteSpace(rawValue))
        return true;

    if (!int.TryParse(
            rawValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed))
    {
        return false;
    }

    value = parsed;
    return true;
}

public async Task UpdateCardAsync(HttpListenerContext context)
{
    try
    {
        var formValues = await FormDataReader.ReadFormValuesAsync(
            context.Request);

        var form = formValues.ToDictionary(
            item => item.Key,
            item => item.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        if (!TryGetInt(form, "id", out int cardId) || cardId <= 0)
        {
            await SendHtmlAsync(
                context,
                "<h2>Некорректный ID карты.</h2><a href='/admin/cards'>Вернуться</a>",
                HttpStatusCode.BadRequest);

            return;
        }

        var request = new CreateCardRequest
        {
            Name = GetValue(form, "name"),
            CardNumber = GetValue(form, "cardNumber"),
            ImageUrl = GetValue(form, "imageUrl"),

            SpecifiedCost = GetNullableString(form, "specifiedCost"),
            Skills = GetNullableString(form, "skills")
        };

        if (formValues.TryGetValue("specialTraitIds", out var traitValues))
        {
            foreach (string value in traitValues)
            {
                if (int.TryParse(value, out int traitId))
                {
                    request.SpecialTraitIds.Add(traitId);
                }
            }
        }

        if (!TryGetInt(form, "colorId", out int colorId))
            request.ColorId = 0;
        else
            request.ColorId = colorId;

        if (!TryGetInt(form, "typeId", out int typeId))
            request.TypeId = 0;
        else
            request.TypeId = typeId;

        if (!TryGetInt(form, "rarityId", out int rarityId))
            request.RarityId = 0;
        else
            request.RarityId = rarityId;

        if (!TryGetInt(form, "setId", out int setId))
            request.SetId = 0;
        else
            request.SetId = setId;

        if (!TryGetNullableInt(form, "cost", out int? cost))
            cost = null;

        if (!TryGetNullableInt(form, "power", out int? power))
            power = null;

        if (!TryGetNullableInt(form, "comboPower", out int? comboPower))
            comboPower = null;

        request.Cost = cost;
        request.Power = power;
        request.ComboPower = comboPower;

        var errors = await _cardService.ValidateCreateCardAsync(request);

        if (errors.Count > 0)
        {
            string errorHtml = string.Join(
                "",
                errors.Select(error => $"<li>{error.Value}</li>"));

            await SendHtmlAsync(
                context,
                $"""
                 <h2>Не удалось обновить карту</h2>
                 <ul>{errorHtml}</ul>
                 <a href="/admin/cards/edit?id={cardId}">Вернуться к редактированию</a>
                 """,
                HttpStatusCode.BadRequest);

            return;
        }

        await _cardService.UpdateCardAsync(cardId, request);

        context.Response.StatusCode = (int)HttpStatusCode.Redirect;
        context.Response.RedirectLocation = "/admin/cards?success=1";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating card: {ex}");

        await SendHtmlAsync(
            context,
            """
            <h2>Произошла ошибка при обновлении карты.</h2>
            <a href="/admin/cards">Вернуться к списку карт</a>
            """,
            HttpStatusCode.InternalServerError);
    }
    finally
    {
        try
        {
            context.Response.Close();
        }
        catch
        {
        }
    }
}

public async Task DeleteCardAsync(HttpListenerContext context)
{
    try
    {
        var formValues = await FormDataReader.ReadFormValuesAsync(
            context.Request);

        var form = formValues.ToDictionary(
            item => item.Key,
            item => item.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        if (!TryGetInt(form, "id", out int cardId) || cardId <= 0)
        {
            await SendHtmlAsync(
                context,
                "<h2>Некорректный ID карты.</h2><a href='/admin/cards'>Вернуться</a>",
                HttpStatusCode.BadRequest);

            return;
        }

        await _cardService.DeleteCardAsync(cardId);

        context.Response.StatusCode = (int)HttpStatusCode.Redirect;
        context.Response.RedirectLocation = "/admin/cards?deleted=1";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting card: {ex}");

        await SendHtmlAsync(
            context,
            """
            <h2>Не удалось удалить карту.</h2>
            <a href="/admin/cards">Вернуться к списку карт</a>
            """,
            HttpStatusCode.InternalServerError);
    }
    finally
    {
        try
        {
            context.Response.Close();
        }
        catch
        {
        }
    }
}
}