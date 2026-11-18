namespace InvestmentTaxCalculator.Model;

public class DividendCalculationResult
{
    public List<DividendSummary> DividendSummary { get; set; } = [];

    public void SetResult(List<DividendSummary> dividendSummaries)
    {
        DividendSummary = dividendSummaries;
    }

    public WrappedMoney GetTotalDividend(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalTaxableDividend);

    public WrappedMoney GetForeignTaxPaid(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalForeignTaxPaid);

    public WrappedMoney GetExcessReportableIncomeDividend(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalExcessReportableIncomeDividend);

    public WrappedMoney GetTotalInterestIncome(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalInterestIncome);

    public WrappedMoney GetSavingInterest(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalTaxableSavingInterest);

    public WrappedMoney GetBondInterest(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalTaxableBondInterest);

    public WrappedMoney GetAccruedIncomeProfit(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalAccurredIncomeProfit);

    /// <summary>Accrued income losses, as a negative number.</summary>
    public WrappedMoney GetAccruedIncomeLoss(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalAccurredIncomeLoss);

    public WrappedMoney GetEtfDividendIncome(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalEtfDividendIncome);

    public WrappedMoney GetExcessReportableIncomeInterest(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalExcessReportableIncomeInterest);

    /// <summary>Interest paid, as a negative number. For reference only: not deductible and not in any total.</summary>
    public WrappedMoney GetInterestExpense(IEnumerable<int> yearFilter) => Sum(yearFilter, i => i.TotalInterestExpense);

    private WrappedMoney Sum(IEnumerable<int> yearFilter, Func<DividendSummary, WrappedMoney> selector)
    {
        HashSet<int> years = [.. yearFilter];
        return DividendSummary.Where(i => years.Contains(i.TaxYear)).Sum(selector);
    }
}
