using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CapitalGainCalculator.Services;

public class CalculationResultExportService
{
    private ITaxYear _taxYear { get; set; }

    public CalculationResultExportService(ITaxYear taxYear)
    {
        _taxYear = taxYear;
    }

    public string Export(TradeCalculationResult calculationResult)
    {
        List<TradeTaxCalculation> tradeTaxCalculations = calculationResult.CalculatedTrade;
        StringBuilder output = new();
        List<int> availableYears = tradeTaxCalculations.Select(i => _taxYear.ToTaxYear(i.Date)).Distinct().OrderByDescending(i => i).ToList();
        foreach (int year in availableYears)
        {
            output.Append(WriteTaxYearSummary(year, calculationResult));
            output.AppendLine();
        }
        output.AppendLine();
        return output.ToString();
    }

    private string WriteTaxYearSummary(int year, TradeCalculationResult calculationResult)
    {
        StringBuilder output = new();
        Func<TradeTaxCalculation, bool> filter = trade => _taxYear.ToTaxYear(trade.Date) == year;
        output.AppendLine($"Summary for tax year {year}:");
        output.AppendLine($"Number of disposals: {calculationResult.NumberOfDisposals(filter)}");
        output.AppendLine($"Total disposal proceeds: {calculationResult.DisposalProceeds(filter):C0}");
        output.AppendLine($"Total allowable costs: {calculationResult.AllowableCosts(filter):C0}");
        output.AppendLine($"Total gains (excluding loss): {calculationResult.TotalGain(filter):C0}");
        output.AppendLine($"Total loss: {calculationResult.TotalLoss(filter):C0}");
        return output.ToString();
    }

}
