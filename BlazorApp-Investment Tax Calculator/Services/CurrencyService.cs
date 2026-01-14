using NMoneys;

namespace InvestmentTaxCalculator.Services;

public class CurrencyService
{
    private static readonly string[] _commonCurrencies = { "GBP", "USD", "EUR", "JPY", "HKD", "CHF", "CAD" };

    public static List<CurrencyViewModel> GetCurrencies()
    {
        // Get all currencies from NMoneys
        var allCurrencies = Currency.FindAll()
            .Select(c => new CurrencyViewModel
            {
                Code = c.IsoCode.ToString(),
                Symbol = c.Symbol,
                Name = c.EnglishName
            })
            .ToList();

        // Sort: Common ones first, then others alphabetically by code
        return [.. allCurrencies
            .OrderByDescending(c => Array.IndexOf(_commonCurrencies, c.Code) != -1)
            .ThenBy(c => Array.IndexOf(_commonCurrencies, c.Code) == -1 ? 0 : Array.IndexOf(_commonCurrencies, c.Code))
            .ThenBy(c => c.Code)];
    }
}

public class CurrencyViewModel
{
    public required string Code { get; init; }
    public string? Symbol { get; init; }
    public required string Name { get; init; }
    public string Display => $"{Code} - {Name} ({Symbol})";
}
