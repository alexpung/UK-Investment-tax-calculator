﻿using Model;
using System.Text;

namespace Services;

public class DividendExportService
{
    private readonly DividendCalculationResult _result;

    public DividendExportService(DividendCalculationResult dividendCalculationResult)
    {
        _result = dividendCalculationResult;
    }

    public string Export(IEnumerable<int> yearsToExport)
    {
        IEnumerable<DividendSummary> dividendSummaries = _result.DividendSummary.Where(dividend => yearsToExport.Contains(dividend.TaxYear)).OrderByDescending(i => i.TaxYear);
        StringBuilder output = new();
        foreach (DividendSummary dividendSummary in dividendSummaries)
        {
            output.AppendLine($"Tax Year: {dividendSummary.TaxYear}");
            output.AppendLine($"Region: {dividendSummary.CountryOfOrigin.ThreeLetterISORegionName} ({dividendSummary.CountryOfOrigin.EnglishName})");
            output.AppendLine($"\tTotal dividends: {dividendSummary.TotalTaxableDividend}");
            output.AppendLine($"\tTotal withholding tax: {dividendSummary.TotalForeignTaxPaid}\n");
            output.AppendLine("\t\tTransactions:");
            foreach (var dividend in dividendSummary.RelatedDividendsAndTaxes.OrderBy(i => i.Date))
            {
                output.AppendLine($"\t\t{dividend.ToPrintedString()}");
            }
            output.AppendLine();
        }
        return output.ToString();
    }
}
