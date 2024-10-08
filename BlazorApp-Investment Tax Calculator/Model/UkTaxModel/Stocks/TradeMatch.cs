﻿using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch : ITextFilePrintable
{
    public required TaxMatchType TradeMatchType { get; init; }
    public ITradeTaxCalculation? MatchedBuyTrade { get; init; }
    public ITradeTaxCalculation? MatchedSellTrade { get; init; }
    public decimal MatchAcquisitionQty { get; set; } = 0m;
    public decimal MatchDisposalQty { get; set; } = 0m;
    public virtual WrappedMoney BaseCurrencyMatchDisposalProceed { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public virtual WrappedMoney BaseCurrencyMatchAllowableCost { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public virtual WrappedMoney MatchGain => BaseCurrencyMatchDisposalProceed - BaseCurrencyMatchAllowableCost;
    public string AdditionalInformation { get; set; } = string.Empty;
    public Section104History? Section104HistorySnapshot { get; init; }
    public required DateOnly Date { get; init; }
    public required string AssetName { get; init; }

    public virtual string PrintToTextFile()
    {
        StringBuilder output = new();
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with value {Section104HistorySnapshot.OldValue}");
            output.AppendLine($"Section 104: Matched {MatchDisposalQty} units of the disposal trade against the section 104 pool. " +
                $"Acquisition cost is {BaseCurrencyMatchAllowableCost}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalProceed} - {BaseCurrencyMatchAllowableCost} " +
                                $"= {BaseCurrencyMatchDisposalProceed - BaseCurrencyMatchAllowableCost}");
            if (!string.IsNullOrEmpty(AdditionalInformation)) output.AppendLine(AdditionalInformation);
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: {MatchAcquisitionQty} units of the acquisition trade against {MatchDisposalQty} units of the disposal trade. " +
                $"Acquisition cost is {BaseCurrencyMatchAllowableCost}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedBuyTrade!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalProceed} - {BaseCurrencyMatchAllowableCost} " +
                                $"= {BaseCurrencyMatchDisposalProceed - BaseCurrencyMatchAllowableCost}");
            if (!string.IsNullOrEmpty(AdditionalInformation)) output.AppendLine(AdditionalInformation);
        }
        return output.ToString();
    }

    protected static string ToPrintedString(TaxMatchType TaxMatchType) => TaxMatchType switch
    {
        TaxMatchType.SAME_DAY => "Same day",
        TaxMatchType.BED_AND_BREAKFAST => "Bed and breakfast",
        TaxMatchType.SHORTCOVER => "Cover unmatched disposal",
        TaxMatchType.SECTION_104 => "Section 104",
        _ => throw new NotImplementedException()
    };
}
