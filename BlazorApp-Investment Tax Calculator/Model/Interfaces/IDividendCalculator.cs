namespace InvestmentTaxCalculator.Model.Interfaces;

public interface IDividendCalculator
{
    public List<DividendSummary> CalculateTax();
}
