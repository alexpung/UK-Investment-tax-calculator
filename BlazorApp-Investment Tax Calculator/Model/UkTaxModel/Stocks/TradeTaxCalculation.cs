using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

/// <summary>
/// Contain trades that considered the same group in tax matching caluclation.
/// The usage of this implementation is limited to trades in same day and same asset name, and same buy/sell side of the trade.
/// </summary>
public class TradeTaxCalculation : ITradeTaxCalculation
{
    private static int _nextId = 0;

    public int Id { get; init; }
    public List<Trade> TradeList { get; init; }
    public List<TradeMatch> MatchHistory { get; init; } = [];
    /// <summary>
    /// Total allowable cost of the matched acquisitions
    /// </summary>
    public WrappedMoney TotalAllowableCost => MatchHistory.Sum(tradeMatch => tradeMatch.BaseCurrencyMatchAllowableCost);
    /// <summary>
    /// Total proceeds that are matched with acquisitions
    /// </summary>
    public virtual WrappedMoney TotalProceeds => MatchHistory.Sum(tradeMatch => tradeMatch.BaseCurrencyMatchDisposalProceed);
    public WrappedMoney Gain => AcquisitionDisposal == TradeType.DISPOSAL ? TotalProceeds - TotalAllowableCost : WrappedMoney.GetBaseCurrencyZero();
    /// <summary>
    /// For acquisition: Cost of buying + commission
    /// For disposal: Proceed you get - commission
    /// </summary>
    public virtual WrappedMoney TotalCostOrProceed { get; protected set; }
    public WrappedMoney UnmatchedCostOrProceed { get; protected set; }
    public WrappedMoney GetProportionedCostOrProceed(decimal qty) => TotalCostOrProceed / TotalQty * qty;
    /// <summary>
    /// Guaranteed by the constructor to be positive non zero decimal.
    /// </summary>
    public decimal TotalQty { get; }
    /// <summary>
    /// The quantity that is not matched with other trades.
    /// </summary>
    public decimal UnmatchedQty { get; protected set; }
    public virtual TradeType AcquisitionDisposal { get; init; }
    public bool CalculationCompleted => UnmatchedQty == 0;
    public DateTime Date { get; init; }
    public AssetCategoryType AssetCategoryType { get; init; }
    public string AssetName { get; init; }
    public ResidencyStatus ResidencyStatusAtTrade { get; set; } = ResidencyStatus.Resident;


    /// <summary>
    /// Bunch a group of trade on the same side so that they can be matched together as a group, 
    /// e.g. UK tax trades on the same side on the same day and same capacity are grouped.
    /// </summary>
    /// <param name="trades">Only accept trade from the same side</param>
    public TradeTaxCalculation(IEnumerable<Trade> trades)
    {
        if (!trades.All(i => i.AcquisitionDisposal.Equals(trades.First().AcquisitionDisposal)))
        {
            throw new ArgumentException("Not all trades that is put in TradeTaxCalculation is on the same BUY/SELL side");
        }
        TradeList = [.. trades];
        TotalCostOrProceed = trades.Sum(trade => trade.NetProceed);
        UnmatchedCostOrProceed = TotalCostOrProceed;
        TotalQty = trades.Sum(trade => trade.Quantity);
        if (TotalQty <= 0) throw new ArgumentException($"The total quantity must be positive. It is {TotalQty}");
        UnmatchedQty = TotalQty;
        AcquisitionDisposal = trades.First().AcquisitionDisposal;
        Id = Interlocked.Increment(ref _nextId);
        AssetName = TradeList[0].AssetName;
        AssetCategoryType = TradeList[0].AssetType;
        Date = TradeList[0].Date;

    }

    public virtual void MatchQty(decimal demandedQty)
    {
        if (demandedQty - UnmatchedQty > 0.00000000000000000000000001m)
        {
            throw new ArgumentException($"Unexpected {nameof(demandedQty)} in MatchQty {demandedQty} larger than {nameof(UnmatchedQty)} {UnmatchedQty}");
        }
        else if (demandedQty - UnmatchedQty > -0.00000000000000000000000001m && demandedQty - UnmatchedQty < 0)
        {
            UnmatchedQty = 0m;
        }
        else
        {
            UnmatchedQty -= demandedQty;
            UnmatchedCostOrProceed -= TotalCostOrProceed * demandedQty / TotalQty;
        }
    }

    public virtual void MatchWithSection104(UkSection104 ukSection104)
    {
        if (UnmatchedQty == 0m) return;
        if (AcquisitionDisposal is TradeType.ACQUISITION)
        {
            Section104History section104History = ukSection104.AddAssets(this, UnmatchedQty, UnmatchedCostOrProceed);
            MatchHistory.Add(
                new TradeMatch()
                {
                    Date = DateOnly.FromDateTime(Date),
                    AssetName = AssetName,
                    TradeMatchType = TaxMatchType.SECTION_104,
                    MatchedBuyTrade = this,
                    MatchAcquisitionQty = UnmatchedQty,
                    MatchDisposalQty = UnmatchedQty,
                    BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
                    BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
                    Section104HistorySnapshot = section104History
                });
            MatchQty(UnmatchedQty);
        }
        else if (AcquisitionDisposal is TradeType.DISPOSAL)
        {
            if (ukSection104.Quantity == 0m) return;
            decimal matchQty = Math.Min(UnmatchedQty, ukSection104.Quantity);
            Section104History section104History = ukSection104.RemoveAssets(this, matchQty);
            MatchHistory.Add(
                new TradeMatch()
                {
                    Date = DateOnly.FromDateTime(Date),
                    AssetName = AssetName,
                    TradeMatchType = TaxMatchType.SECTION_104,
                    MatchedSellTrade = this,
                    MatchAcquisitionQty = matchQty,
                    MatchDisposalQty = matchQty,
                    BaseCurrencyMatchAllowableCost = section104History.ValueChange * -1,
                    BaseCurrencyMatchDisposalProceed = GetProportionedCostOrProceed(matchQty),
                    Section104HistorySnapshot = section104History
                });
            MatchQty(matchQty);
        }
    }
    public string UnmatchedDescription() => UnmatchedQty switch
    {
        0 => "All units of the disposals are matched with acquisitions",
        > 0 => $"{UnmatchedQty} units of disposals are not matched (short sale).",
        _ => throw new NotImplementedException()
    };
    protected static string GetSumFormula(IEnumerable<WrappedMoney> moneyNumbers)
    {
        WrappedMoney sum = moneyNumbers.Sum();
        string formula = string.Join(" ", moneyNumbers.Select(n => n.Amount >= 0 ? $"+ {n}" : $"- {-n}")).TrimStart('+', ' ') + " = " + sum;
        return formula;
    }

    public virtual string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"Sold {TotalQty} units of {AssetName} on " +
            $"{Date:d} for {TotalCostOrProceed}.\t");
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
