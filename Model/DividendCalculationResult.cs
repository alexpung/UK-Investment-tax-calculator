using System.Collections.Generic;
using System.Linq;

namespace CapitalGainCalculator.Model;

public class DividendCalculationResult
{
    public List<DividendSummary> DividendSummary { get; set; } = new();

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }

    public decimal GetTotalDividend(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).Sum(i => i.TotalTaxableDividend);
    }

    public decimal GetForeignTaxPaid(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).Sum(i => i.TotalForeignTaxPaid);
    }
}
