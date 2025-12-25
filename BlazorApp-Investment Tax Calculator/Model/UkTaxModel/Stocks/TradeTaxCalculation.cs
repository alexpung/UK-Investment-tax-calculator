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
    public WrappedMoney TotalAllowableCost => MatchHistory.Where(tradeMatch => tradeMatch.IsTaxable != TaxableStatus.NON_TAXABLE).Sum(tradeMatch => tradeMatch.BaseCurrencyMatchAllowableCost);
    /// <summary>
    /// Total proceeds that are matched with acquisitions
    /// </summary>
    public virtual WrappedMoney TotalProceeds => MatchHistory.Where(tradeMatch => tradeMatch.IsTaxable != TaxableStatus.NON_TAXABLE).Sum(tradeMatch => tradeMatch.BaseCurrencyMatchDisposalProceed);
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
    public DateTime TaxableDate { get; set; }

    /// <summary>
    /// Reset trade IDs when start/restart a calculation
    /// </summary>
    public static void ResetID()
    {
        _nextId = 0;
    }

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
        TaxableDate = Date;

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

    public virtual void MatchWithSection104(UkSection104 ukSection104, TaxableStatus taxableStatus)
    {
        Section104History section104History;
        if (UnmatchedQty == 0m) return;
        if (AcquisitionDisposal is TradeType.ACQUISITION)
        {
            if (ResidencyStatusAtTrade is ResidencyStatus.NonResident or ResidencyStatus.TemporaryNonResident)
            {
                ukSection104.NonResidentExemptQuantity += UnmatchedQty;
            }
            section104History = Section104AddAssets(ukSection104, UnmatchedQty);
            MatchHistory.Add(BuildSection104AcquisitionMatch(section104History));
            MatchQty(UnmatchedQty);
        }
        else if (AcquisitionDisposal is TradeType.DISPOSAL)
        {
            if (ukSection104.Quantity == 0m) return;

            // Assets acquired while non-resident are exempt from UK Capital Gains Tax even when disposed while as a temporarily non-resident.
            if (ResidencyStatusAtTrade is ResidencyStatus.TemporaryNonResident)
            {
                // if all unmatched shares are acquired in non-resident/temp non-resident period, the matching is not taxable
                decimal nonResidentExemptQty = Math.Min(UnmatchedQty, ukSection104.NonResidentExemptQuantity);
                section104History = Section104RemoveAssets(ukSection104, nonResidentExemptQty);
                TradeMatch taxExemptMatch = BuildSection104DisposalMatch(section104History, nonResidentExemptQty, TaxableStatus.NON_TAXABLE);
                taxExemptMatch.AdditionalInformation = $"{nonResidentExemptQty} of this disposal is exempt from UK Capital Gains Tax as the assets were acquired while non-resident.";
                MatchHistory.Add(taxExemptMatch);
                MatchQty(nonResidentExemptQty);
            }
            if (ukSection104.Quantity == 0m || UnmatchedQty == 0) return;
            decimal matchQty = Math.Min(UnmatchedQty, ukSection104.Quantity);
            section104History = Section104RemoveAssets(ukSection104, matchQty);
            MatchHistory.Add(BuildSection104DisposalMatch(section104History, matchQty, taxableStatus));
            MatchQty(matchQty);
        }
    }

    protected virtual Section104History Section104AddAssets(UkSection104 ukSection104, decimal qty)
    {
        return ukSection104.AddAssets(this, qty, UnmatchedCostOrProceed);
    }

    protected virtual Section104History Section104RemoveAssets(UkSection104 ukSection104, decimal qty)
    {
        return ukSection104.RemoveAssets(this, qty);
    }

    protected virtual TradeMatch BuildSection104AcquisitionMatch(Section104History history)
    {
        return new TradeMatch()
        {
            Date = DateOnly.FromDateTime(Date),
            AssetName = AssetName,
            TradeMatchType = TaxMatchType.SECTION_104,
            MatchedBuyTrade = this,
            MatchAcquisitionQty = UnmatchedQty,
            MatchDisposalQty = UnmatchedQty,
            BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
            BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
            Section104HistorySnapshot = history
        };
    }

    protected virtual TradeMatch BuildSection104DisposalMatch(Section104History history, decimal matchQty, TaxableStatus taxableStatus)
    {
        return new TradeMatch()
        {
            Date = DateOnly.FromDateTime(Date),
            AssetName = AssetName,
            TradeMatchType = TaxMatchType.SECTION_104,
            MatchedSellTrade = this,
            MatchAcquisitionQty = matchQty,
            MatchDisposalQty = matchQty,
            BaseCurrencyMatchAllowableCost = history.ValueChange * -1,
            BaseCurrencyMatchDisposalProceed = GetProportionedCostOrProceed(matchQty),
            Section104HistorySnapshot = history,
            IsTaxable = taxableStatus
        };
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
