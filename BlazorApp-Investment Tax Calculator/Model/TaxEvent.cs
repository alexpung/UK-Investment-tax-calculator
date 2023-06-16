using Enum;
using NMoneys;
using System.Globalization;

namespace Model;

public abstract record TaxEvent
{
    public required string AssetName { get; set; }
    public required DateTime Date { get; set; }
}

public record Trade : TaxEvent
{
    public required TradeType BuySell { get; set; }
    public required decimal Quantity { get; set; }
    public required DescribedMoney GrossProceed { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<DescribedMoney> Expenses { get; set; } = new List<DescribedMoney>();
    public decimal NetProceed
    {
        get
        {
            if (!Expenses.Any()) return GrossProceed.BaseCurrencyAmount;
            if (BuySell == TradeType.BUY) return GrossProceed.BaseCurrencyAmount + Expenses.Sum(expense => expense.BaseCurrencyAmount);
            else return GrossProceed.BaseCurrencyAmount - Expenses.Sum(expense => expense.BaseCurrencyAmount);
        }
    }
}

public record Dividend : TaxEvent
{
    public required DividendType DividendType { get; set; }
    public RegionInfo CompanyLocation { get; set; } = RegionInfo.CurrentRegion;
    public required DescribedMoney Proceed { get; set; }
}

public abstract record CorporateAction : TaxEvent
{
}

public record StockSplit : CorporateAction
{
    public required int NumberBeforeSplit { get; set; }
    public required int NumberAfterSplit { get; set; }
    public bool Rounding { get; set; } = true;

    public decimal GetSharesAfterSplit(decimal quantity)
    {
        decimal result = quantity * NumberAfterSplit / NumberBeforeSplit;
        return Rounding ? Math.Round(result, MidpointRounding.ToZero) : result;
    }
}

public record DescribedMoney
{
    public string Description { get; set; } = "";
    public required Money Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public decimal BaseCurrencyAmount => Amount.Amount * FxRate;
}
