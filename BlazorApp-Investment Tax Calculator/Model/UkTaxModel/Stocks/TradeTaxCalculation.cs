﻿using Enum;

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
    public decimal UnmatchedQty { get; private set; }
    public virtual TradeType BuySell { get; init; }
    public bool CalculationCompleted => UnmatchedQty == 0;
    public DateTime Date => TradeList[0].Date;
    public string AssetName => TradeList[0].AssetName;


    /// <summary>
    /// Bunch a group of trade on the same side so that they can be matched together as a group, 
    /// e.g. UK tax trades on the same side on the same day and same capacity are grouped.
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
    }

    public void MatchQty(decimal demandedQty)
    {
        if (demandedQty > UnmatchedQty)
        {
            throw new ArgumentException($"Unexpected {nameof(demandedQty)} in MatchQty {demandedQty} larger than {nameof(UnmatchedQty)} {UnmatchedQty}");
        }
        else
        {
            UnmatchedQty -= demandedQty;
            UnmatchedCostOrProceed -= TotalCostOrProceed * demandedQty / TotalQty;
        }
    }

    public void MatchWithSection104(UkSection104 ukSection104)
    {
        if (BuySell is TradeType.BUY)
        {
            Section104History newSection104History = Section104History.AddToSection104(this, UnmatchedQty, UnmatchedCostOrProceed, ukSection104.Quantity,
                ukSection104.AcquisitionCostInBaseCurrency);
            MatchHistory.Add(TradeMatch.CreateSection104Match(UnmatchedQty, UnmatchedCostOrProceed, WrappedMoney.GetBaseCurrencyZero(), newSection104History));
            ukSection104.Section104HistoryList.Add(newSection104History);
            ukSection104.AdjustValues(UnmatchedQty, UnmatchedCostOrProceed);
            UnmatchedQty = 0;
            UnmatchedCostOrProceed = WrappedMoney.GetBaseCurrencyZero();
        }
        else if (BuySell is TradeType.SELL)
        {
            WrappedMoney acqisitionValue;
            if (ukSection104.Quantity == 0m) return;
            decimal matchQty = Math.Min(UnmatchedQty, ukSection104.Quantity);
            acqisitionValue = ukSection104.AcquisitionCostInBaseCurrency * matchQty / ukSection104.Quantity;
            Section104History newSection104History = Section104History.RemoveFromSection104(this, matchQty * -1, acqisitionValue * -1,
                                                     ukSection104.Quantity, ukSection104.AcquisitionCostInBaseCurrency);
            MatchHistory.Add(TradeMatch.CreateSection104Match(matchQty, acqisitionValue, GetProportionedCostOrProceed(matchQty), newSection104History));
            ukSection104.Section104HistoryList.Add(newSection104History);
            ukSection104.AdjustValues(matchQty * -1, ukSection104.AcquisitionCostInBaseCurrency * matchQty * -1 / ukSection104.Quantity);
            UnmatchedQty -= matchQty;
            UnmatchedCostOrProceed -= GetProportionedCostOrProceed(matchQty);
        }
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
