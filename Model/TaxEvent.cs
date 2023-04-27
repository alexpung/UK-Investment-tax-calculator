using CapitalGainCalculator.Enum;
using NodaMoney;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CapitalGainCalculator.Model;

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

    private string GetExpensesExplanation()
    {
        if (!Expenses.Any()) return string.Empty;
        StringBuilder stringBuilder = new();
        stringBuilder.Append("\n\tExpenses: ");
        foreach (var expense in Expenses)
        {
            stringBuilder.Append(expense.ToString() + "\t");
        }
        return stringBuilder.ToString();
    }

    public override string ToString()
    {
        string action = BuySell switch
        {
            TradeType.BUY => "Bought",
            TradeType.SELL => "Sold",
            _ => throw new NotImplementedException()
        };
        string netExplanation = BuySell switch
        {
            TradeType.BUY => $"Total cost: {NetProceed:C2}",
            TradeType.SELL => $"Net proceed: {NetProceed:C2}",
            _ => throw new NotImplementedException()
        };
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy} for {GrossProceed:C2} with total expense {Expenses.Sum(i => i.BaseCurrencyAmount):C2}, {netExplanation}"
            + GetExpensesExplanation();
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

    public override string ToString()
    {
        string outputString;
        if (Description == string.Empty) outputString = $"{Amount}";
        else outputString = $"{Description}: {Amount}";
        if (FxRate == 1)
        {
            return outputString;
        }
        else return $"{outputString} = {BaseCurrencyAmount:C2} Fx rate = {FxRate}";
    }
}
