using Enum;

using Model.UkTaxModel.Stocks;

using System.Text;

namespace Model.UkTaxModel.Futures;

public record FutureTradeMatch : TradeMatch
{
    public WrappedMoney MatchDisposalContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public required WrappedMoney MatchAcquisitionContractValue { get; set; }
    public required WrappedMoney BaseCurrencyDisposalDealingCost { get; set; }
    public required WrappedMoney BaseCurrencyAcqusitionDealingCost { get; set; }
    public WrappedMoney BaseCurrencyTotalDealingExpense => BaseCurrencyAcqusitionDealingCost + BaseCurrencyDisposalDealingCost;
    public required decimal ClosingFxRate { get; set; }
    public virtual WrappedMoney BaseCurrencyContractValueGain => new((MatchDisposalContractValue.Amount - MatchAcquisitionContractValue.Amount) * ClosingFxRate);

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        string paymentForContractGainOrLoss = BaseCurrencyContractValueGain.Amount switch
        {
            < 0 => $"Payment made to close the contract as loss is ({MatchDisposalContractValue} - {MatchAcquisitionContractValue}) * {ClosingFxRate} = {BaseCurrencyContractValueGain}," +
            $" added to allowable cost",
            >= 0 => $"Payment received to close the contract as gain is ({MatchDisposalContractValue} - {MatchAcquisitionContractValue}) * {ClosingFxRate} = {BaseCurrencyContractValueGain}," +
            $" added to disposal proceed."
        };
        string gainCalculationFormula = $"{BaseCurrencyContractValueGain} - {BaseCurrencyTotalDealingExpense} ";
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with contract value {Section104HistorySnapshot.OldContractValue}");
            output.AppendLine($"Section 104: Matched {MatchDisposalQty} units of the disposal. Acquisition contract value is {MatchAcquisitionContractValue} " +
                $"and disposal contract value {MatchDisposalContractValue}, proportioned dealing cost is {BaseCurrencyAcqusitionDealingCost}");
            output.AppendLine(paymentForContractGainOrLoss);
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Gain for this match is {gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: Matched {MatchDisposalQty} units of the disposal. " +
                $"Acquisition contract value is {MatchAcquisitionContractValue} and disposal contract value is {MatchDisposalContractValue}");
            output.AppendLine(paymentForContractGainOrLoss);
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedBuyTrade!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is {gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        return output.ToString();
    }
}
