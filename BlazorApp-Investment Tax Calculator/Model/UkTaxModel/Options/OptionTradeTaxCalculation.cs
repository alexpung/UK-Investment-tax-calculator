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
    public decimal ExpiredQty { get; set; }
    public decimal AssignedQty { get; set; }
    public decimal OwnerExecisedQty { get; set; }
    /// <summary>
    /// When short an option, the option get "disappear" and the taxable proceed is reduced when the option is assigned. The premium get rolled to the assigned trade instead.
    /// This number indicate the amount to subtract from the full 
    /// </summary>
    private WrappedMoney _refundedDisposalProceed = WrappedMoney.GetBaseCurrencyZero();
    public void RefundDisposalQty(decimal qty)
    {
        _refundedDisposalProceed += GetProportionedCostOrProceed(qty);
    }

    public OptionTradeTaxCalculation(IEnumerable<OptionTrade> trades) : base(trades)
    {
        ExpiredQty = TradeList.Where(trade => ((OptionTrade)trade).TradeReason == TradeReason.Expired).Sum(trade => trade.Quantity);
        AssignedQty = TradeList.Where(trade => ((OptionTrade)trade).TradeReason == TradeReason.OptionAssigned).Sum(trade => trade.Quantity);
        OwnerExecisedQty = TradeList.Where(trade => ((OptionTrade)trade).TradeReason == TradeReason.OwnerExeciseOption).Sum(trade => trade.Quantity);
        PUTCALL = trades.Any() ? trades.First().PUTCALL : throw new ArgumentException("trades is null when providing trade list");
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
            decimal matchDisposalProceedQty = matchQty; // allow owner execise to reduce disposalProceed as execised options are rolled to the underlying trade
            WrappedMoney allowableCost = section104History.ValueChange * -1;
            if (ExpiredQty > 0)
            {
                additionalInformation += $"{ExpiredQty} option expired";
                ExpiredQty = 0;
            }
            if (OwnerExecisedQty > 0)
            {
                Trade exerciseTrade = TradeList.First(trade => trade.TradeReason == TradeReason.OwnerExeciseOption);
                allowableCost = allowableCost * (1 - (OwnerExecisedQty / matchQty));
                additionalInformation += $"{OwnerExecisedQty} option execised.";
                matchDisposalProceedQty -= OwnerExecisedQty;
                OwnerExecisedQty = 0;
                exerciseTrade.AttachOptionTrade(allowableCost, $"Trade is created by option exercise of option on {Date.Date}");
            }
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

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"Option trade of {TotalQty} units of {AssetName} on {Date.Date.ToString("dd-MMM-yyyy")}.\n");
        output.AppendLine($"Total proceeds: {TotalProceeds}.");
        output.AppendLine($"Total gain (loss): {Gain}.");
        output.AppendLine(UnmatchedDescription());

        // Print trade details
        output.AppendLine("Trade details:");
        foreach (var trade in TradeList)
        {
            output.AppendLine($"\t{trade.PrintToTextFile()}");
        }

        // Print trade matching history
        output.AppendLine("Trade matching:");
        foreach (var matching in MatchHistory)
        {
            output.AppendLine(matching.PrintToTextFile());
        }

        // Include any tax repayments if present
        if (TaxRepayList.Any())
        {
            output.AppendLine("Tax repayments:");
            foreach (var taxRepay in TaxRepayList)
            {
                output.AppendLine($"\tTax Year: {taxRepay.TaxYear}, Refund Amount: {taxRepay.RefundAmount}, Reason: {taxRepay.Reason}");
            }
        }

        // Final summary if there are more than 2 match history items
        if (MatchHistory.Count > 2)
        {
            output.AppendLine($"Resulting overall gain for this disposal: {GetSumFormula(MatchHistory.Select(match => match.MatchGain))}");
        }

        output.AppendLine();
        return output.ToString();
    }


}

public record TaxRepay(int TaxYear, WrappedMoney RefundAmount, string Reason);
