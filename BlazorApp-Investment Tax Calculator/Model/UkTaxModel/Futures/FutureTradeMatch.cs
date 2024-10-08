﻿using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Futures;

public record FutureTradeMatch : TradeMatch
{
    public WrappedMoney MatchSellContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public required WrappedMoney MatchBuyContractValue { get; set; }
    public required WrappedMoney BaseCurrencyDisposalDealingCost { get; set; }
    public required WrappedMoney BaseCurrencyAcquisitionDealingCost { get; set; }
    public WrappedMoney BaseCurrencyTotalDealingExpense => BaseCurrencyAcquisitionDealingCost + BaseCurrencyDisposalDealingCost;
    public required decimal ClosingFxRate { get; set; }
    public virtual WrappedMoney BaseCurrencyContractValueGain => new((MatchSellContractValue.Amount - MatchBuyContractValue.Amount) * ClosingFxRate);

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        string paymentForContractGainOrLoss = BaseCurrencyContractValueGain.Amount switch
        {
            < 0 => $"Payment made to close the contract as loss is ({MatchSellContractValue} - {MatchBuyContractValue}) * {ClosingFxRate} = {BaseCurrencyContractValueGain}," +
            $" added to allowable cost",
            >= 0 => $"Payment received to close the contract as gain is ({MatchSellContractValue} - {MatchBuyContractValue}) * {ClosingFxRate} = {BaseCurrencyContractValueGain}," +
            $" added to disposal proceed."
        };
        string gainCalculationFormula = $"{BaseCurrencyContractValueGain} - {BaseCurrencyTotalDealingExpense} ";
        if (TradeMatchType == TaxMatchType.SECTION_104)
        {
            output.AppendLine($"At time of disposal, section 104 contains {Section104HistorySnapshot!.OldQuantity} units with contract value {Section104HistorySnapshot.OldContractValue}");
            output.AppendLine($"Section 104: Matched {MatchDisposalQty} units of the disposal. Acquisition contract value is {MatchBuyContractValue} " +
                $"and disposal contract value {MatchSellContractValue}, proportioned dealing cost is {BaseCurrencyAcquisitionDealingCost}");
            output.AppendLine(paymentForContractGainOrLoss);
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Gain for this match is {gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        else
        {
            output.AppendLine($"{ToPrintedString(TradeMatchType)}: Matched {MatchDisposalQty} units of the disposal. " +
                $"Acquisition contract value is {MatchBuyContractValue} and disposal contract value is {MatchSellContractValue}");
            output.AppendLine(paymentForContractGainOrLoss);
            output.AppendLine($"Total dealing cost is {BaseCurrencyTotalDealingExpense}");
            output.AppendLine($"Matched trade: {string.Join("\n", MatchedBuyTrade!.TradeList.Select(trade => trade.PrintToTextFile()))}");
            output.AppendLine($"Gain for this match is {gainCalculationFormula} = {BaseCurrencyContractValueGain - BaseCurrencyTotalDealingExpense}");
            output.AppendLine();
        }
        return output.ToString();
    }
}
