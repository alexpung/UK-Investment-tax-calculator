namespace InvestmentTaxCalculator.Model;

public class DividendCalculationResult
{
    public List<DividendSummary> DividendSummary { get; set; } = [];

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }

    public WrappedMoney GetTotalDividend(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).Sum(i => i.TotalTaxableDividend);
    }

    public WrappedMoney GetForeignTaxPaid(IEnumerable<int> yearFilter)
    {
        return DividendSummary.Where(i => yearFilter.Contains(i.TaxYear)).Sum(i => i.TotalForeignTaxPaid);
    }
}
