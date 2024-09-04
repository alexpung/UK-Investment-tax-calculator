namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax();
}