using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class TradeCalculationResult
{
    public List<TradeTaxCalculation> CalculatedTrade { get; set; } = new();

    public void SetResult(List<TradeTaxCalculation> tradeTaxCalculations)
    {
        CalculatedTrade = tradeTaxCalculations;
    }
}
