using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

public class DividendCalculationResult
{
    public List<DividendSummary> DividendSummary { get; set; } = new();

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }
}
