using Enum;
using Model.Interfaces;
using System.Text;

namespace Model.UkTaxModel;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch : ITextFilePrintable
{
    public required TaxMatchType TradeMatchType { get; set; }
    public ITradeTaxCalculation? MatchedBuyTrade { get; set; }
    public ITradeTaxCalculation? MatchedSellTrade { get; set; }
    public decimal MatchAcquisitionQty { get; set; } = 0m;
    public decimal MatchDisposalQty { get; set; } = 0m;
    public virtual WrappedMoney BaseCurrencyMatchDisposalValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public virtual WrappedMoney BaseCurrencyMatchAcquisitionValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public virtual WrappedMoney MatchGain => BaseCurrencyMatchDisposalValue - BaseCurrencyMatchAcquisitionValue;
    public string AdditionalInformation { get; set; } = string.Empty;
    public Section104History? Section104HistorySnapshot { get; set; }

    protected TradeMatch() { }

    public static TradeMatch CreateSection104Match(decimal qty, WrappedMoney acqisitionValue, WrappedMoney disposalValue, Section104History section104History)
    {
        TradeMatch tradeMatch = CreateTradeMatch(TaxMatchType.SECTION_104, qty, acqisitionValue, disposalValue);
        tradeMatch.Section104HistorySnapshot = section104History;
        return tradeMatch;
    }

    public static TradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney acqisitionValue, WrappedMoney disposalValue, ITradeTaxCalculation? matchedSellTrade = null,
        ITradeTaxCalculation? matchedBuyTrade = null, string additionalInfo = "")
    {
        return new()
        {
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = qty,
            MatchDisposalQty = qty,
            BaseCurrencyMatchAcquisitionValue = acqisitionValue,
            BaseCurrencyMatchDisposalValue = disposalValue,
            MatchedBuyTrade = matchedBuyTrade,
            MatchedSellTrade = matchedSellTrade,
            AdditionalInformation = additionalInfo
        };
    }

    public virtual string PrintToTextFile()
    {
        StringBuilder output = new();
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with value {Section104HistorySnapshot.OldValue}");
            output.AppendLine($"Section 104: Matched {MatchAcquisitionQty} units of the acquisition trade against {MatchDisposalQty} units of the disposal trade. acquisition cost is {BaseCurrencyMatchAcquisitionValue}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalValue} - {BaseCurrencyMatchAcquisitionValue} " +
                                $"= {BaseCurrencyMatchDisposalValue - BaseCurrencyMatchAcquisitionValue}");
            if (!string.IsNullOrEmpty(AdditionalInformation)) output.AppendLine(AdditionalInformation);
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: {MatchAcquisitionQty} units of the acquisition trade against {MatchDisposalQty} units of the disposal trade. acquisition cost is {BaseCurrencyMatchAcquisitionValue}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedBuyTrade!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalValue} - {BaseCurrencyMatchAcquisitionValue} " +
                                $"= {BaseCurrencyMatchDisposalValue - BaseCurrencyMatchAcquisitionValue}");
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
