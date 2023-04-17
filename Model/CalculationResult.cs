using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class CalculationResult
{
    public required List<TradeTaxCalculation> CalculatedTrade { get; set; }
    public List<TradeTaxCalculation> UnmatchedDisposal { get; set; } = new();

    public void SetResult(CalculationResult result)
    {
        CalculatedTrade = result.CalculatedTrade;
        UnmatchedDisposal = result.UnmatchedDisposal;
    }
}
