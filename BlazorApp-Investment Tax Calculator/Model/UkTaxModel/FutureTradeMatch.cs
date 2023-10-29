using Enum;
using Model.Interfaces;
using System.Text;

namespace Model.UkTaxModel;

public record FutureTradeMatch : TradeMatch
{
    private WrappedMoney MatchDisposalContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    private WrappedMoney MatchAcquisitionContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    private WrappedMoney BaseCurrencyTotalDealingExpense { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public decimal ClosingFxRate { get; set; } = 1m;
    public virtual WrappedMoney BaseCurrencyContractValueGain => (MatchDisposalContractValue - MatchAcquisitionContractValue) * ClosingFxRate;

    private FutureTradeMatch() { }

    public static FutureTradeMatch CreateSection104Match(decimal qty, WrappedMoney matchDisposalContractValue, WrappedMoney matchAcquisitionContractValue, WrappedMoney baseCurrencyTotalDealingExpense, decimal fxRate,
        Section104History section104History)
    {
        FutureTradeMatch tradeMatch = CreateTradeMatch(TaxMatchType.SECTION_104, qty, matchDisposalContractValue, matchAcquisitionContractValue, fxRate, baseCurrencyTotalDealingExpense);
        tradeMatch.Section104HistorySnapshot = section104History;
        return tradeMatch;
    }

    /// <summary>
    /// For the purposes of this Act, where, in the course of dealing in commodity or financial futures, a person who has entered into a futures contract closes out that contract by entering into another futures contract with obligations which are reciprocal to those of the first-mentioned contract, that transaction shall constitute the disposal of an asset (namely, his outstanding obligations under the first-mentioned contract) and, accordingly—
    /// (a) any money or money’s worth received by him on that transaction shall constitute consideration for the disposal; and
    /// (b) any money or money’s worth paid or given by him on that transaction shall be treated as incidental costs to him of making the disposal.
    /// </summary>
    /// <returns></returns>
    public static FutureTradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney matchDisposalContractValue, WrappedMoney matchAcquisitionContractValue, decimal fxRate,
        WrappedMoney baseCurrencyTotalDealingExpense, ITradeTaxCalculation? matchedGroup = null)
    {
        WrappedMoney baseCurrencyMatchDisposalValue = WrappedMoney.GetBaseCurrencyZero();
        WrappedMoney baseCurrencyMatchAcquisitionValue = WrappedMoney.GetBaseCurrencyZero();
        WrappedMoney gain = matchDisposalContractValue - matchAcquisitionContractValue;
        // The handling of the gain is treated in accordance to TCGA92 S143 (5)
        if (gain.Amount >= 0)
        {
            baseCurrencyMatchDisposalValue += new WrappedMoney(gain.Amount); //
        }
        else
        {
            baseCurrencyMatchAcquisitionValue += new WrappedMoney(gain.Amount) * -1;
        }
        return new()
        {
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = qty,
            MatchDisposalQty = qty,
            ClosingFxRate = fxRate,
            MatchDisposalContractValue = matchDisposalContractValue,
            MatchAcquisitionContractValue = matchAcquisitionContractValue,
            BaseCurrencyMatchAcquisitionValue = baseCurrencyTotalDealingExpense + baseCurrencyMatchAcquisitionValue,
            BaseCurrencyMatchDisposalValue = baseCurrencyMatchDisposalValue,
            MatchedGroup = matchedGroup
        };
    }

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        string gainCalculationFormula = $"({MatchDisposalContractValue} - {MatchAcquisitionContractValue}) * {ClosingFxRate} - {BaseCurrencyTotalDealingExpense} ";
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with contract value {Section104HistorySnapshot.OldValue}");
            output.AppendLine($"Section 104: Matched {MatchDisposalQty} units of the disposal. Acquisition contract value is {MatchAcquisitionContractValue} and disposal contract value {MatchDisposalContractValue}");
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Gain for this match is ({gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: Matched {MatchDisposalQty} units of the disposal. Acquisition contract value is {MatchAcquisitionContractValue} and disposal contract value {MatchDisposalContractValue}");
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedGroup!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is ({gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        return output.ToString();
    }
}
