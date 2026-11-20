namespace InvestmentTaxCalculator.Services;

using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;

public class TaxYearReportService(TradeCalculationResult tradeCalculationResult)
{
    private readonly UkCapitalGainAllowance _ukCapitalGainAllowance = new();
    /// <summary>
    /// The capital gains report for one tax year. A year without any trade has no report of its own (for example a
    /// year with only dividends): it has no gain or loss, and the losses of earlier years carry through it unchanged.
    /// </summary>
    public TaxYearCgtReport GetTaxYearReport(int taxYear) => GetTaxYearReports([taxYear])[0];

    /// <summary>
    /// The capital gains reports for the given tax years, in the order given, building the yearly reports only once.
    /// </summary>
    public List<TaxYearCgtReport> GetTaxYearReports(IEnumerable<int> taxYears)
    {
        List<TaxYearCgtReport> reports = [.. GetTaxYearReports()];
        return [.. taxYears.Select(taxYear => FindOrCreateReport(reports, taxYear))];
    }

    private TaxYearCgtReport FindOrCreateReport(List<TaxYearCgtReport> reports, int taxYear)
    {
        TaxYearCgtReport? report = reports.Find(r => r.TaxYear == taxYear);
        if (report is not null) return report;

        WrappedMoney zero = WrappedMoney.GetBaseCurrencyZero();
        // Reports are in tax year order, so the last earlier one holds the losses still to carry forward.
        WrappedMoney lossesCarried = reports.FindLast(r => r.TaxYear < taxYear)?.LossesAvailableToBroughtForward ?? zero;
        return new TaxYearCgtReport
        {
            TaxYear = taxYear,
            CapitalGainAllowance = _ukCapitalGainAllowance.GetTaxAllowance(taxYear),
            TotalGainInYear = zero,
            TotalLossInYear = zero,
            NetCapitalGain = zero,
            CgtAllowanceBroughtForwardAndUsed = zero,
            TaxableGainAfterAllowanceAndLossOffset = zero,
            LossesAvailableToBroughtForward = lossesCarried,
            AvailableCapitalLossesFromPreviousYears = lossesCarried,
        };
    }

    public IEnumerable<TaxYearCgtReport> GetTaxYearReports()
    {
        // Group by the year each trade is taxable in, the same years the gain and loss totals use.
        List<int> taxYears = [.. tradeCalculationResult.GetTaxYearsWithTrades()];
        WrappedMoney capitalLossRunningCount = WrappedMoney.GetBaseCurrencyZero();
        foreach (int taxYear in taxYears)
        {
            WrappedMoney totalGainInYear = tradeCalculationResult.GetTotalGain([taxYear]);
            WrappedMoney totalLossInYear = tradeCalculationResult.GetTotalLoss([taxYear]); // Is a negative value
            WrappedMoney netGainInYear = totalGainInYear + totalLossInYear;
            WrappedMoney capitalGainAllowance = _ukCapitalGainAllowance.GetTaxAllowance(taxYear);
            WrappedMoney lossBroughtForward = WrappedMoney.GetBaseCurrencyZero();
            WrappedMoney lossesAvailableToBroughtForward = capitalLossRunningCount;
            WrappedMoney capitalLossInPreviousYears = capitalLossRunningCount;

            if (netGainInYear.Amount < 0)
            {
                lossesAvailableToBroughtForward += netGainInYear * -1;
            }
            else if (netGainInYear >= capitalGainAllowance)
            {
                lossBroughtForward = WrappedMoney.Min(capitalLossInPreviousYears, netGainInYear - capitalGainAllowance);
                lossesAvailableToBroughtForward -= lossBroughtForward;
            }
            capitalLossRunningCount = lossesAvailableToBroughtForward;
            yield return new TaxYearCgtReport()
            {
                TaxYear = taxYear,
                CapitalGainAllowance = capitalGainAllowance,
                TotalGainInYear = totalGainInYear,
                TotalLossInYear = totalLossInYear,
                NetCapitalGain = totalGainInYear + totalLossInYear,
                CgtAllowanceBroughtForwardAndUsed = lossBroughtForward,
                TaxableGainAfterAllowanceAndLossOffset = WrappedMoney.Max(netGainInYear - capitalGainAllowance - lossBroughtForward, WrappedMoney.GetBaseCurrencyZero()),
                LossesAvailableToBroughtForward = lossesAvailableToBroughtForward,
                AvailableCapitalLossesFromPreviousYears = capitalLossInPreviousYears,
            };
        }

    }
}
