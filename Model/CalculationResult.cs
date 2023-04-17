using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class CalculationResult
{
    public required List<TradeTaxCalculation> CalculatedTrade { get; set; }
    public List<TradeTaxCalculation> UnmatchedDisposal { get; set; } = new();
}
