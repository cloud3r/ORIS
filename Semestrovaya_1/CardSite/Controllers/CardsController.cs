using System.Net;
using System.Text;
using System.Text.Json;
using CardSite.Services.Interfaces;
using MiniTemplateEngine.Interfaces;

namespace CardSite.Controllers;

public class CardsController
{
    private readonly ICardService _cardService;
    private readonly IHtmlTemplateRenderer _templateRenderer;
    private readonly string _viewsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CardsController(
        ICardService cardService,
        IHtmlTemplateRenderer templateRenderer,
        string viewsPath)
    {
        _cardService = cardService;
        _templateRenderer = templateRenderer;
        _viewsPath = viewsPath;
    }
public async Task GetCardsAsync(HttpListenerContext context)
{
    var query = context.Request.QueryString;

    string? keyword = query["q"];

    List<int> typeIds = GetIntParameters(context, "type");
    List<int> colorIds = GetIntParameters(context, "color");
    List<int> rarityIds = GetIntParameters(context, "rarity");

    int? costMin = GetRangeParameter(context, "costMin", 0);
    int? costMax = GetRangeParameter(context, "costMax", CostLimit);

    int? powerMin = GetRangeParameter(context, "powerMin", 0);
    int? powerMax = GetRangeParameter(context, "powerMax", PowerLimit);

    int? comboPowerMin = GetRangeParameter(context, "comboPowerMin", 0);
    int? comboPowerMax = GetRangeParameter(
        context, "comboPowerMax", ComboPowerLimit);

    var traitIds = (query.GetValues("specialTraits") ?? Array.Empty<string>())
        .Select(value => int.TryParse(value, out int parsed)
            ? parsed
            : (int?)null)
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .Distinct()
        .ToList();

    var products = await _cardService.GetAllSetsAsync();

    string? productParameter = query["product"];

    bool allSeries = productParameter == "all";

    var selectedProduct = allSeries
        ? null
        : products.FirstOrDefault(
            product => product.Id.ToString() == productParameter)
          ?? products.FirstOrDefault(product => product.Code == "FB11");

    string? setCode = allSeries
        ? null
        : selectedProduct?.Code ?? "FB11";

    var cards = await _cardService.SearchCardsWithFiltersAsync(
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
        traitIds);

    const int pageSize = 20;

    int totalCards = cards.Count;
    int totalPages = (int)Math.Ceiling(totalCards / (double)pageSize);

    int currentPage = GetIntParameter(context, "page") ?? 1;

    currentPage = Math.Max(1, currentPage);

    if (totalPages > 0)
        currentPage = Math.Min(currentPage, totalPages);

    var pagedCards = cards
        .Skip((currentPage - 1) * pageSize)
        .Take(pageSize)
        .Select(card => new
        {
            card.Id,
            card.Name,
            card.CardNumber,
            card.ImageUrl
        })
        .ToList();

    await SendJsonAsync(context, 200, new
    {
        cards = pagedCards,
        totalCards,
        currentPage,
        totalPages,
        hasPrevious = currentPage > 1,
        hasNext = currentPage < totalPages
    });
}
    public async Task GetCardByIdAsync(HttpListenerContext context)
    {
        string path = context.Request.Url?.AbsolutePath ?? "";

        string idString = path.Split('/').Last();

        if (!int.TryParse(idString, out int id))
        {
            await SendJsonAsync(context, 400, new
            {
                error = "Invalid card ID"
            });

            return;
        }

        var card = await _cardService.GetCardByIdAsync(id);

        if (card is null)
        {
            await SendJsonAsync(context, 404, new
            {
                error = "Card not found"
            });

            return;
        }

        var result = new
        {
            id = card.Id,
            name = card.Name,
            cardNumber = card.CardNumber,
            imageUrl = card.ImageUrl,
            cardType = card.Type?.Name,
            color = card.Color?.Name,
            cost = card.Cost,
            specifiedCost = card.SpecifiedCost,
            power = card.Power,
            comboPower = card.ComboPower,
            rarity = card.Rarity?.Code,
            set = card.Set?.Name,
            setCode = card.Set?.Code,
            specialTraits = card.SpecialTraits
                .Select(trait => trait.Name)
                .ToList(),

            skills = card.Skills,

            products = card.Products
                .Select(product => new
                {
                    id = product.Id,
                    name = product.Name,
                    imageUrl = product.ImageUrl,
                    productUrl = product.ProductUrl
                })
                .ToList()
        };
        
        await SendJsonAsync(context, 200, result);
        
    }
    
    private const int CostLimit = 9;
    private const int PowerLimit = 55000;
    private const int ComboPowerLimit = 10000;

    private static int? GetRangeParameter(
        HttpListenerContext context,
        string parameterName,
        int defaultValue)
    {
        int? value = GetIntParameter(context, parameterName);

        return value == defaultValue ? null : value;
    }

    public async Task GetCardsPageAsync(HttpListenerContext context)
    {
        var query = context.Request.QueryString;

        string? keyword = query["q"];

        List<int> typeIds = GetIntParameters(context, "type");
        List<int> colorIds = GetIntParameters(context, "color");
        List<int> rarityIds = GetIntParameters(context, "rarity");

        int? costMin = GetRangeParameter(context, "costMin", 0);
        int? costMax = GetRangeParameter(context, "costMax", CostLimit);
        int? powerMin = GetRangeParameter(context, "powerMin", 0);
        int? powerMax = GetRangeParameter(context, "powerMax", PowerLimit);
        int? comboPowerMin = GetRangeParameter(context, "comboPowerMin", 0);
        int? comboPowerMax = GetRangeParameter(context, "comboPowerMax", ComboPowerLimit);

        var traitIds = (query.GetValues("specialTraits") ?? Array.Empty<string>())
            .Select(value => int.TryParse(value, out int parsed) ? parsed : (int?)null)
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value)
            .Distinct()
            .ToList();

        var products = await _cardService.GetAllSetsAsync();
        var allTraits = await _cardService.GetAllSpecialTraitsAsync();

        string? productParameter = query["product"];
        bool allSeries = productParameter == "all";

        var selectedProduct = allSeries
            ? null
            : products.FirstOrDefault(product => product.Id.ToString() == productParameter)
              ?? products.FirstOrDefault(product => product.Code == "FB11");

        string? setCode = allSeries ? null : selectedProduct?.Code ?? "FB11";

        var cards = await _cardService.SearchCardsWithFiltersAsync(
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
            traitIds);
        
        const int pageSize = 20;

        int totalCards = cards.Count;

        int totalPages = (int)Math.Ceiling(totalCards / (double)pageSize);

        int currentPage = GetIntParameter(context, "page") ?? 1;

        if (currentPage < 1)
            currentPage = 1;

        if (totalPages > 0 && currentPage > totalPages)
            currentPage = totalPages;

        if (totalPages == 0)
            currentPage = 1;

        var pagedCards = cards
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pageNumbers = Enumerable.Range(1, totalPages)
            .Select(page => new
            {
                Number = page,
                Url = BuildPageUrl(context, page),
                CurrentClass = page == currentPage ? "active" : ""
            })
            .ToList();

        string previousUrl = BuildPageUrl(
            context,
            Math.Max(1, currentPage - 1));

        string nextUrl = BuildPageUrl(
            context,
            Math.Min(totalPages, currentPage + 1));

        static string Checked(bool condition) => condition ? "checked" : "";

var data = new
{
    Cards = pagedCards.Select(card => new
    {
        card.Id,
        card.Name,
        card.CardNumber,
        card.ImageUrl
    }).ToList(),

    TotalCards = totalCards,
    CurrentPage = currentPage,
    TotalPages = totalPages,

    HasPrevious = currentPage > 1,
    HasNext = currentPage < totalPages,
    

    PreviousClass = currentPage > 1 ? "active" : "disabled",
    NextClass = currentPage < totalPages ? "active" : "disabled",

    PageNumbers = pageNumbers,

    Products = products.Select(product => new
    {
        product.Id,
        product.Name,
        product.Code,
        Selected = product.Id == selectedProduct?.Id ? "selected" : ""
    }).ToList(),

    AllSeriesSelected = allSeries ? "selected" : "",

    SpecialTraits = allTraits.Select(trait => new
    {
        trait.Id,
        trait.Name,
        Selected = Checked(traitIds.Contains(trait.Id))
    }).ToList(),

    AllTraitsChecked = Checked(traitIds.Count == 0),

    SelectedSetName = selectedProduct?.Name ?? string.Empty,
    SelectedSetCode = selectedProduct?.Code ?? string.Empty,

    Keyword = keyword ?? string.Empty,

    CostMin = costMin?.ToString() ?? string.Empty,
    CostMax = costMax?.ToString() ?? string.Empty,
    PowerMin = powerMin?.ToString() ?? string.Empty,
    PowerMax = powerMax?.ToString() ?? string.Empty,
    ComboPowerMin = comboPowerMin?.ToString() ?? string.Empty,
    ComboPowerMax = comboPowerMax?.ToString() ?? string.Empty,

    Type1Checked = Checked(typeIds.Contains(1)),
    Type2Checked = Checked(typeIds.Contains(2)),
    Type3Checked = Checked(typeIds.Contains(3)),
    Type4Checked = Checked(typeIds.Contains(4)),

    Color1Checked = Checked(colorIds.Contains(1)),
    Color2Checked = Checked(colorIds.Contains(2)),
    Color3Checked = Checked(colorIds.Contains(3)),
    Color4Checked = Checked(colorIds.Contains(4)),
    Color5Checked = Checked(colorIds.Contains(5)),

    Rarity1Checked = Checked(rarityIds.Contains(1)),
    Rarity2Checked = Checked(rarityIds.Contains(2)),
    Rarity3Checked = Checked(rarityIds.Contains(3)),
    Rarity4Checked = Checked(rarityIds.Contains(4)),
    Rarity5Checked = Checked(rarityIds.Contains(5)),
    Rarity6Checked = Checked(rarityIds.Contains(6)),
    Rarity7Checked = Checked(rarityIds.Contains(7))
    };

    string templatePath = Path.Combine(_viewsPath, "Index.html");

    string html = _templateRenderer.RenderFromFile(templatePath, data);

    byte[] bytes = Encoding.UTF8.GetBytes(html);

    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength64 = bytes.Length;

    await context.Response.OutputStream.WriteAsync(bytes);
    context.Response.Close();
    }
    
    public async Task GetCardDetailsPageAsync(HttpListenerContext context)
    {
        string path = context.Request.Url?.AbsolutePath ?? "";

        string idString = path.Split('/').Last();

        if (!int.TryParse(idString, out int id) || id <= 0)
        {
            RedirectToHome(context);
            return;
        }

        var card = await _cardService.GetCardByIdAsync(id);

        if (card is null)
        {
            RedirectToHome(context);
            return;
        }

        var data = new
        {
            card.Id,
            card.Name,
            card.CardNumber,
            card.ImageUrl,

            CardType = card.Type?.Name ?? "",
            Color = card.Color?.Name ?? "",
            Cost = card.Cost?.ToString() ?? "-",
            SpecifiedCost = card.SpecifiedCost ?? "-",
            Power = card.Power?.ToString() ?? "-",
            ComboPower = card.ComboPower?.ToString() ?? "-",

            Rarity = card.Rarity?.Code ?? "",
            SetName = card.Set?.Name ?? "",
            SetCode = card.Set?.Code ?? "",

            Skills = card.Skills ?? "",

            SpecialTraits = card.SpecialTraits
                .Select(trait => new
                {
                    trait.Name
                })
                .ToList(),

            Products = card.Products
                .Select(product => new
                {
                    product.Name,
                    product.ImageUrl,
                    product.ProductUrl
                })
                .ToList()
        };

        string templatePath = Path.Combine(_viewsPath, "Details.html");

        string html = _templateRenderer.RenderFromFile(templatePath, data);

        byte[] bytes = Encoding.UTF8.GetBytes(html);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;

        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }
    
    private static void RedirectToHome(HttpListenerContext context)
    {
        try
        {
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = "/";
            context.Response.Close();
        }
        catch
        {
            try
            {
                context.Response.Abort();
            }
            catch
            {
                
            }
        }
    }
    
    private static async Task SendJsonAsync(
        HttpListenerContext context,
        int statusCode,
        object data)
    {
        string json = JsonSerializer.Serialize(data, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;

        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private static int? GetIntParameter(
        HttpListenerContext context,
        string parameterName)
    {
        string? value = context.Request.QueryString[parameterName];

        if (int.TryParse(value, out int result))
            return result;

        return null;
    }
    
    private static List<int> GetIntParameters(
        HttpListenerContext context,
        string parameterName)
    {
        string[] values =
            context.Request.QueryString.GetValues(parameterName)
            ?? Array.Empty<string>();
        
        if (values.Any(value =>
                string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)))
        {
            return new List<int>();
        }

        return values
            .Select(value => int.TryParse(value, out int parsed)
                ? parsed
                : (int?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();
    }
    
    private static string BuildPageUrl(
        HttpListenerContext context,
        int page)
    {
        var parameters = new List<string>();

        foreach (string? key in context.Request.QueryString.AllKeys)
        {
            if (string.IsNullOrEmpty(key) ||
                key.Equals("page", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] values =
                context.Request.QueryString.GetValues(key)
                ?? Array.Empty<string>();

            foreach (string value in values)
            {
                parameters.Add(
                    $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        parameters.Add($"page={page}");

        return "/cards?" + string.Join("&", parameters);
    }
}