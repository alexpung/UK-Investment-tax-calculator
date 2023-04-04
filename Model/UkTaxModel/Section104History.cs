using CapitalGainCalculator.Model.Interfaces;
using System;

namespace CapitalGainCalculator.Model.UkTaxModel;
public class Section104History
{
    public ITradeTaxCalculation? TradeTaxCalculation { get; set; }
    public decimal OldQuantity { get; set; }
    public decimal OldValue { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal ValueChange { get; set; }
    public string Explanation { get; set; } = string.Empty;

    public static Section104History AddToSection104(ITradeTaxCalculation tradeTaxCalculation, decimal quantityChange, decimal valueChange, decimal oldQuantity, decimal oldValue)
    {
        return new Section104History
        {
            QuantityChange = quantityChange,
            ValueChange = valueChange,
            TradeTaxCalculation = tradeTaxCalculation,
            OldQuantity = oldQuantity,
            OldValue = oldValue,
            Explanation = $"{quantityChange} units worth {valueChange:C2} added to Section 104 from the following trades.\n" +
                            $"{string.Join("\n", tradeTaxCalculation.TradeList)}\n" +
                            $"Section 104 quantity changes from {oldQuantity} to {oldQuantity + quantityChange}\n" +
                            $"Section 104 value changes from {oldValue:C2} to {oldValue + valueChange:C2}\n"
        };
    }

    public static Section104History RemoveFromSection104(ITradeTaxCalculation tradeTaxCalculation, decimal quantityChange, decimal valueChange, decimal oldQuantity, decimal oldValue)
    {
        return new Section104History
        {
            QuantityChange = quantityChange,
            ValueChange = valueChange,
            TradeTaxCalculation = tradeTaxCalculation,
            OldQuantity = oldQuantity,
            OldValue = oldValue,
            Explanation = $"{quantityChange * -1} units with value of {valueChange * -1:C2} removed to Section 104 from the following trades.\n" +
                            $"{string.Join("\n", tradeTaxCalculation.TradeList)}\n" +
                            $"Section 104 quantity changes from {oldQuantity} to {oldQuantity + quantityChange}\n" +
                            $"Section 104 value changes from {oldValue:C2} to {oldValue + valueChange:C2}\n"
        };
    }

    public static Section104History ShareAdjustment(DateTime date, decimal oldQuantity, decimal newQuantity)
    {
        return new Section104History
        {
            OldQuantity = oldQuantity,
            QuantityChange = newQuantity - oldQuantity,
            Explanation = $"Share adjustment on {date.ToShortDateString()} due to corporate action. Quantity of Section104 pool changes from {oldQuantity} to {newQuantity}\n"
        };
    }
}
