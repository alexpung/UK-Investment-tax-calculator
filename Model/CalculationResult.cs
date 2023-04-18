using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class CalculationResult
{
    public required List<TradeTaxCalculation> CalculatedTrade { get; set; }

    public void SetResult(CalculationResult result)
    {
        CalculatedTrade = result.CalculatedTrade;
    }
}
