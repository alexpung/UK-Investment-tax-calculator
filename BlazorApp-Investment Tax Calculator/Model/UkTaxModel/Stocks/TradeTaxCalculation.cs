using Enum;

using Model;
using Model.Interfaces;
using Model.TaxEvents;

using System.Text;

namespace Model.UkTaxModel.Stocks;

/// <summary>
/// Contain trades that considered the same group in tax matching caluclation.
/// The usage of this implementation is limited to trades in same day and same asset name, and same buy/sell side of the trade.
/// </summary>
public class TradeTaxCalculation : ITradeTaxCalculation
{
    public List<Trade> TradeList { get; init; }
    public List<TradeMatch> MatchHistory { get; init; } = new List<TradeMatch>();
    public WrappedMoney TotalAllowableCost => MatchHistory.Sum(tradeMatch => tradeMatch.BaseCurrencyMatchAllowableCost);
    public WrappedMoney TotalProceeds => MatchHistory.Sum(tradeMatch => tradeMatch.BaseCurrencyMatchDisposalProceed);
    public WrappedMoney Gain => TotalProceeds - TotalAllowableCost;
    /// <summary>
    /// For acquistion: Cost of buying + commission
    /// For disposal: Proceed you get - commission
    /// </summary>
    public WrappedMoney TotalCostOrProceed { get; private set; }
    public WrappedMoney UnmatchedCostOrProceed { get; private set; }
    public WrappedMoney GetProportionedCostOrProceed(decimal qty) => TotalCostOrProceed / TotalQty * qty;
    public decimal TotalQty { get; }
    private decimal _unmatchedQty;
    public decimal UnmatchedQty
    {
        get { return _unmatchedQty; }
        private set
        {
            _unmatchedQty = value;
            if (UnmatchedQty == 0) CalculationCompleted = true;
        }
    }
    public TradeType BuySell { get; init; }
    public bool CalculationCompleted { get; private set; }
    public DateTime Date => TradeList[0].Date;
    public string AssetName => TradeList[0].AssetName;


    /// <summary>
    /// Bunch a group of trade on the same side so that they can be matched together as a group, e.g. UK tax trades on the same side on the same day and same capacity are grouped.
    /// </summary>
    /// <param name="trades">Only accept trade from the same side</param>
    public TradeTaxCalculation(IEnumerable<Trade> trades)
    {
        if (!trades.All(i => i.BuySell.Equals(trades.First().BuySell)))
        {
            throw new ArgumentException("Not all trades that is put in TradeTaxCalculation is on the same BUY/SELL side");
        }
        TradeList = trades.ToList();
        TotalCostOrProceed = trades.Sum(trade => trade.NetProceed);
        UnmatchedCostOrProceed = TotalCostOrProceed;
        TotalQty = trades.Sum(trade => trade.Quantity);
        UnmatchedQty = TotalQty;
        BuySell = trades.First().BuySell;
        CalculationCompleted = false;
    }

    public virtual (decimal matchedQty, WrappedMoney matchedValue) MatchQty(decimal demandedQty)
    {
        decimal matchedQty;
        WrappedMoney matchedValue;
        if (demandedQty >= UnmatchedQty)
        {
            matchedQty = UnmatchedQty;
            matchedValue = UnmatchedCostOrProceed;
            UnmatchedQty = 0;
            UnmatchedCostOrProceed = WrappedMoney.GetBaseCurrencyZero();
        }
        else
        {
            matchedQty = demandedQty;
            matchedValue = TotalCostOrProceed * demandedQty / TotalQty;
            UnmatchedQty -= matchedQty;
            UnmatchedCostOrProceed -= matchedValue;
        }
        return (matchedQty, matchedValue);
    }

    public virtual (decimal matchedQty, WrappedMoney matchedValue) MatchAll()
    {
        decimal matchedQty = UnmatchedQty;
        WrappedMoney matchedValue = UnmatchedCostOrProceed;
        UnmatchedQty = 0;
        UnmatchedCostOrProceed = WrappedMoney.GetBaseCurrencyZero();
        return (matchedQty, matchedValue);
    }

    public string UnmatchedDescription() => UnmatchedQty switch
    {
        0 => "All units of the disposals are matched with acquisitions",
        > 0 => $"{UnmatchedQty} units of disposals are not matched (short sale).",
        _ => throw new NotImplementedException()
    };

    private static string GetSumFormula(IEnumerable<WrappedMoney> moneyNumbers)
    {
        WrappedMoney sum = moneyNumbers.Sum();
        string formula = string.Join(" ", moneyNumbers.Select(n => n.Amount >= 0 ? $"+ {n}" : $"- {-n}")).TrimStart('+', ' ') + " = " + sum;
        return formula;
    }

    public string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"Sold {TotalQty} units of {AssetName} on " +
            $"{Date.Date.ToString("dd-MMM-yyyy")} for {TotalCostOrProceed}.\t");
        output.AppendLine($"Total gain (loss): {Gain}");
        output.AppendLine(UnmatchedDescription());
        output.AppendLine($"Trade details:");
        foreach (var trade in TradeList)
        {
            output.AppendLine($"\t{trade.PrintToTextFile()}");
        }
        output.AppendLine($"Trade matching:");
        foreach (var matching in MatchHistory)
        {
            output.AppendLine(matching.PrintToTextFile());
        }
        if (MatchHistory.Count > 2)
        {
            output.AppendLine($"Resulting overall gain for this disposal: {GetSumFormula(MatchHistory.Select(match => match.MatchGain))}");
        }
        output.AppendLine();
        return output.ToString();
    }
}
