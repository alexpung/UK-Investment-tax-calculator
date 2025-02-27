﻿using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Services;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel;
public class Section104History : ITextFilePrintable
{
    public ITradeTaxCalculation? TradeTaxCalculation { get; set; }
    public DateTime Date { get; set; }
    public decimal OldQuantity { get; set; }
    public WrappedMoney OldValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney OldContractValue { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public decimal QuantityChange { get; set; }
    public WrappedMoney ValueChange { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney ContractValueChange { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public decimal NewQuantity => OldQuantity + QuantityChange;
    public WrappedMoney NewValue => OldValue + ValueChange;
    public WrappedMoney NewContractValue => OldContractValue + ContractValueChange;
    public string Explanation { get; set; } = string.Empty;

    public static Section104History AdjustSection104(ITradeTaxCalculation tradeTaxCalculation, decimal quantityChange, WrappedMoney valueChange, decimal oldQuantity,
                                                    WrappedMoney oldValue, WrappedMoney? oldContractValue = null, WrappedMoney? contractValueChange = null)
    {
        return new Section104History
        {
            Date = tradeTaxCalculation.Date,
            QuantityChange = quantityChange,
            ValueChange = valueChange,
            TradeTaxCalculation = tradeTaxCalculation,
            OldQuantity = oldQuantity,
            OldValue = oldValue,
            OldContractValue = oldContractValue is null ? WrappedMoney.GetBaseCurrencyZero() : oldContractValue,
            ContractValueChange = contractValueChange is null ? WrappedMoney.GetBaseCurrencyZero() : contractValueChange,
        };
    }

    public static Section104History ShareAdjustment(DateTime date, decimal oldQuantity, decimal newQuantity)
    {
        return new Section104History
        {
            OldQuantity = oldQuantity,
            Date = date,
            QuantityChange = newQuantity - oldQuantity,
            Explanation = $"Share adjustment on {date.ToShortDateString()} due to corporate action."
        };
    }

    public string PrintToTextFile()
    {
        StringBuilder output = new();
        string contractValueString;
        contractValueString = (OldContractValue.Amount, ContractValueChange.Amount) switch
        {
            (0, 0) => "",
            (0, not 0) => $"{ContractValueChange:0.##} ({ContractValueChange.ToSignedNumberString():0.##})",
            (not 0, _) => $"{NewContractValue:0.##} ({ContractValueChange.ToSignedNumberString():0.##})",
        };
        output.AppendLine($"{Date.ToShortDateString()}\t{NewQuantity:0.##} ({QuantityChange.ToSignedNumberString():0.##})\t\t\t\t" +
            $"{NewValue:0.##} ({ValueChange.ToSignedNumberString()})\t\t\t" +
            contractValueString);
        if (Explanation != string.Empty)
        {
            output.AppendLine($"{Explanation}");
        }
        if (TradeTaxCalculation?.TradeList is not null)
        {
            output.AppendLine("Involved trades:");
            foreach (var trade in TradeTaxCalculation.TradeList)
            {
                output.AppendLine(trade.PrintToTextFile());
            }
        }
        return output.ToString();
    }
}
