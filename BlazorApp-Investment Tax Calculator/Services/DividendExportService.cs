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
            output.AppendLine($"\tTotal withholding tax: {dividendSummary.TotalForeignTaxPaid}\n");
            output.AppendLine("\t\tTransactions:");
            foreach (var dividend in dividendSummary.RelatedDividendsAndTaxes.OrderBy(i => i.Date))
            {
                output.AppendLine($"\t\t{dividend.PrintToTextFile()}");
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
