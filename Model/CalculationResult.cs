using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class CalculationResult
{
    public List<TradeTaxCalculation> CalculatedTrade { get; set; } = new();
    public List<DividendSummary> DividendSummary { get; set; } = new();

    public void SetResult(List<TradeTaxCalculation> tradeTaxCalculations)
    {
        CalculatedTrade = tradeTaxCalculations;
    }

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }
}
