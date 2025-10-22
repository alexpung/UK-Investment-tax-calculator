using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.Interfaces;

/// <summary>
/// Dividend and interest income lists for tax calculations.
/// </summary>
public interface IDividendLists
{
    List<Dividend> Dividends { get; set; }
    List<InterestIncome> InterestIncomes { get; set; }
}