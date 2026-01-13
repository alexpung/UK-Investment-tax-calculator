using InvestmentTaxCalculator.Model;

using System.Text;

namespace InvestmentTaxCalculator.Services;

/// <summary>
/// Export dividend and interest income grouped by year and region to a text format.
/// </summary>
/// <param name="dividendCalculationResult"></param>
public class DividendExportService(DividendCalculationResult dividendCalculationResult)
{
    public string Export(IEnumerable<int> yearsToExport)
    {
        IEnumerable<DividendSummary> dividendSummaries = dividendCalculationResult.DividendSummary.Where(dividend => yearsToExport.Contains(dividend.TaxYear)).OrderByDescending(i => i.TaxYear);
        StringBuilder output = new();
        foreach (DividendSummary dividendSummary in dividendSummaries)
        {
            output.AppendLine($"Tax Year: {dividendSummary.TaxYear}");
            output.AppendLine($"Region: {dividendSummary.CountryOfOrigin.ThreeDigitCode} ({dividendSummary.CountryOfOrigin.CountryName})");
            output.AppendLine($"\tTotal dividends: {dividendSummary.TotalTaxableDividend}");
            output.AppendLine($"\t\t(Ordinary: {dividendSummary.TotalTaxableDividend - dividendSummary.TotalExcessReportableIncomeDividend})");
            output.AppendLine($"\t\t(ERI: {dividendSummary.TotalExcessReportableIncomeDividend})");
            output.AppendLine($"\tTotal withholding tax: {dividendSummary.TotalForeignTaxPaid}\n");
            output.AppendLine($"\tSavings interest: {dividendSummary.TotalTaxableSavingInterest}");
            output.AppendLine($"\tBond interest: {dividendSummary.TotalTaxableBondInterest}");
            output.AppendLine($"\tAccrued income profit: {dividendSummary.TotalAccurredIncomeProfit}");
            output.AppendLine($"\tAccrued income loss: {dividendSummary.TotalAccurredIncomeLoss}");
            output.AppendLine($"\tExcess Reportable Income (Interest): {dividendSummary.TotalExcessReportableIncomeInterest}");
            output.AppendLine($"\tTotal interest income: {dividendSummary.TotalInterestIncome}\n");
            output.AppendLine();
            output.AppendLine("\t\tDividend Transactions:");
            if (dividendSummary.RelatedDividendsAndTaxes.Count == 0)
            {
                output.AppendLine("\t\tNone");
            }
            foreach (var dividend in dividendSummary.RelatedDividendsAndTaxes.OrderBy(i => i.Date))
            {
                output.AppendLine($"\t\t{dividend.PrintToTextFile()}");
            }
            output.AppendLine("\t\tInterest Transactions:");
            if (dividendSummary.RelatedInterestIncome.Count == 0)
            {
                output.AppendLine("\t\tNone");
            }
            foreach (var interestIncome in dividendSummary.RelatedInterestIncome.OrderBy(i => i.Date))
            {
                output.AppendLine($"\t\t{interestIncome.PrintToTextFile()}");
            }
            output.AppendLine();
        }
        return output.ToString();
    }
}
