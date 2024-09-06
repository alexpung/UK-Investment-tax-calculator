using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.Interfaces;
public interface IDividendLists
{
    List<Dividend> Dividends { get; set; }
}