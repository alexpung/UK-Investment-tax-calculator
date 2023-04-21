﻿using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CapitalGainCalculator.Services;
public class DividendExportService
{
    public string Export(IEnumerable<DividendSummary> dividendSummaries)
    {
        dividendSummaries = dividendSummaries.OrderByDescending(i => i.TaxYear);
        StringBuilder output = new();
        foreach (DividendSummary dividendSummary in dividendSummaries)
        {
            output.AppendLine($"Tax Year: {dividendSummary.TaxYear}");
            output.AppendLine($"Region: {dividendSummary.CountryOfOrigin}");
            output.AppendLine($"\tTotal dividends: {dividendSummary.TotalTaxableDividend:C2}");
            output.AppendLine($"\tTotal withholding tax: £{dividendSummary.TotalForeignTaxPaid:C2}\n");
            output.AppendLine("\t\tTransactions:");
            foreach (var dividend in dividendSummary.RelatedDividendsAndTaxes)
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
