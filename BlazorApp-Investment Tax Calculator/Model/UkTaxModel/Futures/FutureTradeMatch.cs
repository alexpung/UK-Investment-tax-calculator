using Enum;

using Model.Interfaces;
using Model.UkTaxModel.Stocks;

using System.Text;

namespace Model.UkTaxModel.Futures;

public record FutureTradeMatch : TradeMatch
{
    private WrappedMoney MatchDisposalContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    private WrappedMoney MatchAcquisitionContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    private WrappedMoney BaseCurrencyTotalDealingExpense { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public decimal ClosingFxRate { get; set; } = 1m;
    public virtual WrappedMoney BaseCurrencyContractValueGain => (MatchDisposalContractValue - MatchAcquisitionContractValue) * ClosingFxRate;

    private FutureTradeMatch() { }

    public static FutureTradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney acqisitionValue, WrappedMoney disposalValue,
        WrappedMoney matchAcquisitionContractValue, WrappedMoney matchDisposalContractValue,
        ITradeTaxCalculation? matchedSellTrade = null, ITradeTaxCalculation? matchedBuyTrade = null, string additionalInfo = "")
    {
        return new()
        {
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = qty,
            MatchDisposalQty = qty,
            BaseCurrencyMatchAllowableCost = acqisitionValue,
            BaseCurrencyMatchDisposalProceed = disposalValue,
            MatchedBuyTrade = matchedBuyTrade,
            MatchedSellTrade = matchedSellTrade,
            AdditionalInformation = additionalInfo,
            MatchAcquisitionContractValue = matchAcquisitionContractValue,
            MatchDisposalContractValue = matchDisposalContractValue,
        };
    }

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        string gainCalculationFormula = $"({MatchDisposalContractValue} - {MatchAcquisitionContractValue}) * {ClosingFxRate} - {BaseCurrencyTotalDealingExpense} ";
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with contract value {Section104HistorySnapshot.OldValue}");
            output.AppendLine($"Section 104: Matched {MatchDisposalQty} units of the disposal. Acquisition contract value is {MatchAcquisitionContractValue} " +
                $"and disposal contract value {MatchDisposalContractValue}");
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Gain for this match is ({gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: Matched {MatchDisposalQty} units of the disposal. " +
                $"Acquisition contract value is {MatchAcquisitionContractValue} and disposal contract value is {MatchDisposalContractValue}");
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedBuyTrade!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is ({gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        return output.ToString();
    }
}
