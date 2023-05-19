using Enum;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
            output.AppendLine($"\tTotal dividends: {dividendSummary.TotalTaxableDividend:C2}");
            output.AppendLine($"\tTotal withholding tax: {dividendSummary.TotalForeignTaxPaid:C2}\n");
            output.AppendLine("\t\tTransactions:");
            foreach (var dividend in dividendSummary.RelatedDividendsAndTaxes.OrderBy(i => i.Date))
            {
                output.AppendLine($"\t\t{PrettyPrintDividend(dividend)}");
            }
            output.AppendLine();
        }
        return output.ToString();
    }

    private static string DividendTypeConverter(DividendType dividendType) => dividendType switch
    {
        DividendType.WITHHOLDING => "Withholding Tax",
        DividendType.DIVIDEND_IN_LIEU => "Payment In Lieu of a Dividend",
        DividendType.DIVIDEND => "Dividend",
        _ => throw new NotImplementedException() //SHould not get a dividend object with any other type.
    };

    private string PrettyPrintDividend(Dividend dividend)
    {
        return $"Asset Name: {dividend.AssetName}, " +
                $"Date: {dividend.Date.ToShortDateString()}, " +
                $"Type: {DividendTypeConverter(dividend.DividendType)}, " +
                $"Amount: {dividend.Proceed.Amount}, " +
                $"FxRate: {dividend.Proceed.FxRate}, " +
                $"Sterling Amount: £{dividend.Proceed.BaseCurrencyAmount:0.00}, " +
                $"Description: {dividend.Proceed.Description}";
    }
}
