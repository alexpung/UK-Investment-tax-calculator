using Model.Interfaces;
using NMoneys;

namespace Model.UkTaxModel;
public class Section104History
{
    public ITradeTaxCalculation? TradeTaxCalculation { get; set; }
    public DateTime Date { get; set; }
    public decimal OldQuantity { get; set; }
    public Money OldValue { get; set; }
    public decimal QuantityChange { get; set; }
    public Money ValueChange { get; set; }
    public string Explanation { get; set; } = string.Empty;

    public static Section104History AddToSection104(ITradeTaxCalculation tradeTaxCalculation, decimal quantityChange, Money valueChange, decimal oldQuantity, Money oldValue)
    {
        return new Section104History
        {
            Date = tradeTaxCalculation.Date,
            QuantityChange = quantityChange,
            ValueChange = valueChange,
            TradeTaxCalculation = tradeTaxCalculation,
            OldQuantity = oldQuantity,
            OldValue = oldValue,
        };
    }

    public static Section104History RemoveFromSection104(ITradeTaxCalculation tradeTaxCalculation, decimal quantityChange, Money valueChange, decimal oldQuantity, Money oldValue)
    {
        return new Section104History
        {
            Date = tradeTaxCalculation.Date,
            QuantityChange = quantityChange,
            ValueChange = valueChange,
            TradeTaxCalculation = tradeTaxCalculation,
            OldQuantity = oldQuantity,
            OldValue = oldValue,
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
}
