using Enum;
using Model.Interfaces;
using Model.UkTaxModel;
using System.Text;

namespace Model;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch : ITextFilePrintable
{
    public required TaxMatchType TradeMatchType { get; set; }
    public ITradeTaxCalculation? MatchedGroup { get; set; }
    public decimal MatchAcquitionQty { get; set; } = 0m;
    public decimal MatchDisposalQty { get; set; } = 0m;
    public virtual WrappedMoney BaseCurrencyMatchDisposalValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public virtual WrappedMoney BaseCurrencyMatchAcquitionValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public string AdditionalInformation { get; set; } = string.Empty;
    public Section104History? Section104HistorySnapshot { get; set; }

    private TradeMatch() { }

    public static TradeMatch CreateSection104Match(decimal qty, WrappedMoney acqisitionValue, WrappedMoney disposalValue, Section104History section104History)
    {
        TradeMatch tradeMatch = CreateTradeMatch(TaxMatchType.SECTION_104, qty, acqisitionValue, disposalValue);
        tradeMatch.Section104HistorySnapshot = section104History;
        return tradeMatch;
    }

    public static TradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney acqisitionValue, WrappedMoney disposalValue, ITradeTaxCalculation? matchedGroup = null,
        string additionalInfo = "")
    {
        return new()
        {
            TradeMatchType = taxMatchType,
            MatchAcquitionQty = qty,
            MatchDisposalQty = qty,
            BaseCurrencyMatchAcquitionValue = acqisitionValue,
            BaseCurrencyMatchDisposalValue = disposalValue,
            MatchedGroup = matchedGroup,
            AdditionalInformation = additionalInfo
        };
    }

    public virtual string PrintToTextFile()
    {
        StringBuilder output = new();
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with value {Section104HistorySnapshot.OldValue}");
            output.AppendLine($"Section 104: Matched {MatchAcquitionQty} units of the acquition trade against {BaseCurrencyMatchDisposalValue} units of the disposal trade. Acquition cost is {BaseCurrencyMatchAcquitionValue}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalValue} - {BaseCurrencyMatchAcquitionValue} " +
                                $"= {BaseCurrencyMatchDisposalValue - BaseCurrencyMatchAcquitionValue}");
            output.AppendLine(AdditionalInformation);
            output.AppendLine();
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: {MatchAcquitionQty} units of the acquition trade against {BaseCurrencyMatchDisposalValue} units of the disposal trade. Acquition cost is {BaseCurrencyMatchAcquitionValue}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedGroup!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is {BaseCurrencyMatchDisposalValue} - {BaseCurrencyMatchAcquitionValue} " +
                                $"= {BaseCurrencyMatchDisposalValue - BaseCurrencyMatchAcquitionValue}");
            output.AppendLine(AdditionalInformation);
            output.AppendLine();
        }
        return output.ToString();
    }

    private static string ToPrintedString(TaxMatchType TaxMatchType) => TaxMatchType switch
    {
        TaxMatchType.SAME_DAY => "Same day",
        TaxMatchType.BED_AND_BREAKFAST => "Bed and breakfast",
        TaxMatchType.SHORTCOVER => "Cover unmatched disposal",
        TaxMatchType.SECTION_104 => "Section 104",
        _ => throw new NotImplementedException()
    };
}
