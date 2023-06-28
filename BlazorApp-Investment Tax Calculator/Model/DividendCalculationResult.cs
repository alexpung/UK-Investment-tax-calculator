using NMoneys;

namespace Model;

public class DividendCalculationResult
{
    public List<DividendSummary> DividendSummary { get; set; } = new();

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }

    public Money GetTotalDividend(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).BaseCurrencySum(i => i.TotalTaxableDividend);
    }

    public Money GetForeignTaxPaid(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).BaseCurrencySum(i => i.TotalForeignTaxPaid);
    }
}
