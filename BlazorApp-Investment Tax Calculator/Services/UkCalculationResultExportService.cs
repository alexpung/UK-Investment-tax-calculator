using Model;
using Model.Interfaces;
using System.Text;

namespace Services;

public class UkCalculationResultExportService(ITaxYear taxYear, TradeCalculationResult tradeCalculationResult) : ITextFilePrintable
{
    public string PrintToTextFile(IEnumerable<int> yearsToExport)
    {
        StringBuilder output = new();
        foreach (int year in yearsToExport.OrderByDescending(i => i))
        {
            output.Append(WriteTaxYearSummary(year, tradeCalculationResult));
            IEnumerable<ITradeTaxCalculation> yearFilteredTradeCalculations = tradeCalculationResult.CalculatedTrade.Where(i => taxYear.ToTaxYear(i.Date) == year && i.BuySell == Enum.TradeType.SELL)
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

    private static string WriteTaxYearSummary(int year, TradeCalculationResult calculationResult)
    {
        StringBuilder output = new();
        IEnumerable<int> filter = new[] { year };
        output.AppendLine($"Summary for tax year {year}:");
        output.AppendLine($"Number of disposals: {calculationResult.NumberOfDisposals(filter)}");
        output.AppendLine($"Total disposal proceeds: {calculationResult.DisposalProceeds(filter)}");
        output.AppendLine($"Total allowable costs: {calculationResult.AllowableCosts(filter)}");
        output.AppendLine($"Total gains (excluding loss): {calculationResult.TotalGain(filter)}");
        output.AppendLine($"Total loss: {calculationResult.TotalLoss(filter)}");
        return output.ToString();
    }

    private static string WriteDisposalDetails(IEnumerable<ITradeTaxCalculation> tradeTaxCalculations)
    {
        StringBuilder output = new();
        int DisposalCount = 1;
        foreach (var calculations in tradeTaxCalculations)
        {
            output.AppendLine("*******************************************************************************");
            output.Append($"Disposal {DisposalCount}: {calculations.PrintToTextFile()}");
            DisposalCount++;
        }
        return output.ToString();
    }
}
