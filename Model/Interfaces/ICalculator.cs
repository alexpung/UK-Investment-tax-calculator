using CapitalGainCalculator.Model.UkTaxModel;

namespace CapitalGainCalculator.Model.Interfaces;

public interface ICalculator
{
    public CalculationResult CalculateTax(TaxEventLists taxEventLists);
}