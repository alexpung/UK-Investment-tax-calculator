using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Text;

namespace InvestmentTaxCalculator.Services;

public class UkCalculationResultExportService(ITaxYear taxYear, TradeCalculationResult tradeCalculationResult,
    TaxYearCgtByTypeReportService taxYearCgtByTypeReportService, TaxYearReportService taxYearReportService) : ITextFilePrintable
{
    private IEnumerable<TaxYearCgtReport>? _taxYearReports;
    public string PrintToTextFile(IEnumerable<int> yearsToExport)
    {
        _taxYearReports = taxYearReportService.GetTaxYearReports();
        StringBuilder output = new();
        foreach (int year in yearsToExport.OrderByDescending(i => i))
        {
            output.Append(WriteTaxYearSummary(year));
            IEnumerable<ITradeTaxCalculation> yearFilteredTradeCalculations = tradeCalculationResult
                .CalculatedTrade.Where(i => taxYear.ToTaxYear(i.Date) == year && i.AcquisitionDisposal == TradeType.DISPOSAL)
                .OrderBy(i => i.Date);
            output.AppendLine();
            output.Append(WriteDisposalDetails(yearFilteredTradeCalculations));
            output.AppendLine();
        }
        output.AppendLine();
        return output.ToString();
    }

    public string PrintToTextFile()
    {
        IEnumerable<int> taxYears = tradeCalculationResult.CalculatedTrade.Select(calculation => taxYear.ToTaxYear(calculation.Date)).Distinct().OrderByDescending(i => i);
        return PrintToTextFile(taxYears);
    }

    private string WriteTaxYearSummary(int year)
    {
        TaxYearCgtReport? taxYearCgtReport = _taxYearReports?.FirstOrDefault(report => report.TaxYear == year);
        TaxYearCgtByTypeReport taxYearCgtByTypeReport = taxYearCgtByTypeReportService.GetTaxYearCgtByTypeReport(year);
        StringBuilder output = new();
        output.AppendLine($"Summary for tax year {year}:");
        if (taxYearCgtReport != null) WriteTaxYearOverallSummary(output, taxYearCgtReport);
        WriteTaxYearByTypeSummary(output, taxYearCgtByTypeReport);
        return output.ToString();
    }

    private static void WriteTaxYearOverallSummary(StringBuilder output, TaxYearCgtReport taxYearCgtReport)
    {
        output.AppendLine($"Total gain in year {taxYearCgtReport.TotalGainInYear}");
        output.AppendLine($"Total loss in year {taxYearCgtReport.TotalLossInYear}");
        output.AppendLine($"Net gain in year {taxYearCgtReport.NetCapitalGain}");
        output.AppendLine($"Capital allowance available {taxYearCgtReport.CapitalGainAllowance}");
        output.AppendLine($"Capital loss brought forward and used {taxYearCgtReport.CgtAllowanceBroughtForwardAndUsed}");
        output.AppendLine($"Taxable gain after allowance and offset {taxYearCgtReport.TaxableGainAfterAllowanceAndLossOffset}");
        output.AppendLine($"Loss available to bring forward {taxYearCgtReport.LossesAvailableToBroughtForward}");
    }

    private static void WriteTaxYearByTypeSummary(StringBuilder output, TaxYearCgtByTypeReport taxYearCgtByTypeReport)
    {
        output.AppendLine();
        output.AppendLine("Listed Shares and Securities:");
        output.AppendLine($"Number of Disposals {taxYearCgtByTypeReport.ListedSecurityNumberOfDisposals}");
        output.AppendLine($"Disposal Proceeds {taxYearCgtByTypeReport.ListedSecurityDisposalProceeds}");
        output.AppendLine($"Allowable costs {taxYearCgtByTypeReport.ListedSecurityAllowableCosts}");
        output.AppendLine($"Gain excluding loss {taxYearCgtByTypeReport.ListedSecurityGainExcludeLoss}");
        output.AppendLine($"Loss {taxYearCgtByTypeReport.ListedSecurityLoss}");
        output.AppendLine();
        output.AppendLine("Other assets:");
        output.AppendLine($"Number of Disposals {taxYearCgtByTypeReport.OtherAssetsNumberOfDisposals}");
        output.AppendLine($"Disposal Proceeds {taxYearCgtByTypeReport.OtherAssetsDisposalProceeds}");
        output.AppendLine($"Allowable costs {taxYearCgtByTypeReport.OtherAssetsAllowableCosts}");
        output.AppendLine($"Gain excluding loss {taxYearCgtByTypeReport.OtherAssetsGainExcludeLoss}");
        output.AppendLine($"Loss {taxYearCgtByTypeReport.OtherAssetsLoss}");
    }

    private static string WriteDisposalDetails(IEnumerable<ITradeTaxCalculation> tradeTaxCalculations)
    {
        StringBuilder output = new();
        var GroupedTradeTaxCalculations = tradeTaxCalculations.GroupBy(tradeTaxCalculations => tradeTaxCalculations.AssetCategoryType);
        foreach (var Grouping in GroupedTradeTaxCalculations)
        {
            output.AppendLine("*******************************************************************************");
            output.AppendLine($"Disposal for {Grouping.Key.GetDescription()}");
            int DisposalCount = 1;
            foreach (var calculations in Grouping)
            {
                output.AppendLine("*******************************************************************************");
                output.Append($"Disposal {DisposalCount}: {calculations.PrintToTextFile()}");
                DisposalCount++;
            }
        }
        return output.ToString();
    }
}
