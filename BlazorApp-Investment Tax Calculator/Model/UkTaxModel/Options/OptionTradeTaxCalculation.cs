using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Options;

public class OptionTradeTaxCalculation : TradeTaxCalculation
{
    /// <summary>
    /// Disposal is always taxed full premium regardless if the trade is matched
    /// </summary>
    public override WrappedMoney TotalProceeds => AcquisitionDisposal == TradeType.DISPOSAL ? TotalCostOrProceed - _refundedDisposalProceed :
        MatchHistory.Sum(tradeMatch => tradeMatch.BaseCurrencyMatchDisposalProceed);
    /// <summary>
    /// If an option is written and taxed for the full premium, the tax payers can get back tax refund for the allowable cost that is not deducted from the full premium
    /// when the written option is matched with an acquisition 
    /// </summary>
    public List<TaxRepay> TaxRepayList { get; init; } = [];
    public PUTCALL PUTCALL { get; init; }
    public decimal ExpiredQty { get; init; }
    public decimal AssignedQty { get; init; }
    public decimal OwnerExercisedQty { get; init; }
    public decimal OrderedTradeQty { get; init; }
    /// <summary>
    /// When short an option, the option get "disappear" and the taxable proceed is reduced when the option is assigned. The premium get rolled to the assigned trade instead.
    /// This number indicate the amount to subtract from the full 
    /// </summary>

    public bool IsCashSettled { get; init; }
    private WrappedMoney _refundedDisposalProceed = WrappedMoney.GetBaseCurrencyZero();

    public WrappedMoney GetProportionedCostOrProceedForTradeReason(TradeReason tradeReason, decimal qty)
    {
        WrappedMoney totalValue = TradeList.Where(trade => trade.TradeReason == tradeReason).Select(trade => trade.NetProceed).Sum();
        decimal totalQty = tradeReason switch
        {
            TradeReason.OwnerExerciseOption => OwnerExercisedQty,
            TradeReason.OptionAssigned => AssignedQty,
            TradeReason.Expired => ExpiredQty,
            TradeReason.OrderedTrade => OrderedTradeQty,
            _ => throw new ArgumentException($"Unexpected trade reason {tradeReason}")
        };
        if (totalQty == 0) return WrappedMoney.GetBaseCurrencyZero();
        return qty / totalQty * totalValue;
    }
    public void RefundDisposalQty(decimal qty)
    {
        if (AcquisitionDisposal != TradeType.DISPOSAL) throw new InvalidOperationException("Refunding option proceed can only be done in short option trade");
        _refundedDisposalProceed += GetProportionedCostOrProceed(qty);
    }

    public OptionTradeTaxCalculation(IEnumerable<OptionTrade> trades) : base(trades)
    {
        if (!trades.Any())
        {
            throw new ArgumentException("Trades list cannot be empty", nameof(trades));
        }
        ExpiredQty = trades.Where(trade => trade.TradeReason == TradeReason.Expired).Sum(trade => trade.Quantity);
        AssignedQty = trades.Where(trade => trade.TradeReason == TradeReason.OptionAssigned).Sum(trade => trade.Quantity);
        OwnerExercisedQty = trades.Where(trade => trade.TradeReason == TradeReason.OwnerExerciseOption).Sum(trade => trade.Quantity);
        OrderedTradeQty = trades.Where(trade => trade.TradeReason == TradeReason.OrderedTrade).Sum(trade => trade.Quantity);
        PUTCALL = trades.First().PUTCALL;
        IEnumerable<bool> settlementTypes = trades
            .Where(trade => trade.TradeReason is TradeReason.OwnerExerciseOption or TradeReason.OptionAssigned)
            .Select(trade => trade.CashSettled)
            .Distinct();
        if (!settlementTypes.Any()) IsCashSettled = false;
        else if (settlementTypes.Count() > 1)
        {
            throw new ArgumentException("Unexpected option that is both cash and underlying settled");
        }
        else IsCashSettled = settlementTypes.First();
    }


    public override void MatchWithSection104(UkSection104 ukSection104)
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
            string additionalInformation = string.Empty;
            decimal matchDisposalProceedQty = matchQty; // allow owner exercise to reduce disposalProceed as exercised options are rolled to the underlying trade
            WrappedMoney allowableCost = section104History.ValueChange * -1;
            if (ExpiredQty > 0)
            {
                additionalInformation += $"{ExpiredQty} option expired. ";
            }
            if (OwnerExercisedQty > 0 && !IsCashSettled)
            {
                WrappedMoney exerciseAllowableCost = allowableCost * OwnerExercisedQty / matchQty;
                allowableCost -= exerciseAllowableCost;
                additionalInformation += $"{OwnerExercisedQty} option exercised. ";
                matchDisposalProceedQty -= OwnerExercisedQty;
                AttachTradeToUnderlying(exerciseAllowableCost, $"Trade is created by option exercise of option on {Date:d}", TradeReason.OwnerExerciseOption);
            }
            if (OwnerExercisedQty > 0 && IsCashSettled) additionalInformation += $"{OwnerExercisedQty:F2} option cash settled.";
            TradeMatch tradeMatch = new()
            {
                Date = DateOnly.FromDateTime(Date),
                AssetName = AssetName,
                TradeMatchType = TaxMatchType.SECTION_104,
                MatchedSellTrade = this,
                MatchAcquisitionQty = matchQty,
                MatchDisposalQty = matchQty,
                BaseCurrencyMatchAllowableCost = allowableCost,
                BaseCurrencyMatchDisposalProceed = GetProportionedCostOrProceed(matchDisposalProceedQty),
                Section104HistorySnapshot = section104History,
                AdditionalInformation = additionalInformation
            };
            MatchHistory.Add(tradeMatch);
            MatchQty(matchQty);
        }
    }

    public void AttachTradeToUnderlying(WrappedMoney attachedPremium, string comment, TradeReason tradeReason)
    {
        if (PUTCALL == PUTCALL.PUT) attachedPremium = attachedPremium * -1;
        OptionTrade exerciseTrade = (OptionTrade)TradeList.First(trade => ((OptionTrade)trade).ExerciseOrExercisedTrade?.TradeReason == tradeReason);
        exerciseTrade.ExerciseOrExercisedTrade!.AttachOptionTrade(attachedPremium, comment);
    }

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"Sold {TotalQty} units of {AssetName} on " +
            $"{Date:d} for {TotalCostOrProceed}.\t");
        output.AppendLine($"Total gain (loss): {Gain}.");
        output.AppendLine(UnmatchedDescription());
        output.AppendLine("Trade details:");
        foreach (var trade in TradeList)
        {
            output.AppendLine($"\t{trade.PrintToTextFile()}");
        }
        output.AppendLine("Trade matching:");
        foreach (var matching in MatchHistory)
        {
            output.AppendLine(matching.PrintToTextFile());
        }
        if (TaxRepayList.Count != 0)
        {
            output.AppendLine("Overpaid tax refund:");
            foreach (var taxRepay in TaxRepayList)
            {
                output.AppendLine($"\tTax Year: {taxRepay.TaxYear}, Refund Amount: {taxRepay.RefundAmount}, Reason: {taxRepay.Reason}");
            }
        }
        if (MatchHistory.Count > 2)
        {
            output.AppendLine($"Resulting overall gain for this disposal: {GetSumFormula(MatchHistory.Select(match => match.MatchGain))}");
        }
        output.AppendLine();
        return output.ToString();
    }
}

public record TaxRepay(int TaxYear, WrappedMoney RefundAmount, string Reason);
