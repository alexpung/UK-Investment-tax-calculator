using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using System.Text;

namespace Services;

public class UkCalculationResultExportService
{
    private readonly ITaxYear _taxYear;
    private readonly UkSection104Pools _section104Pools;
    private readonly TradeCalculationResult _tradeCalculationResult;

    public UkCalculationResultExportService(ITaxYear taxYear, UkSection104Pools ukSection104Pools, TradeCalculationResult tradeCalculationResult)
    {
        _taxYear = taxYear;
        _section104Pools = ukSection104Pools;
        _tradeCalculationResult = tradeCalculationResult;
    }

    public string Export(IEnumerable<int> yearsToExport)
    {
        IEnumerable<ITradeTaxCalculation> tradeTaxCalculations = _tradeCalculationResult.CalculatedTrade;
        StringBuilder output = new();
        foreach (int year in yearsToExport.OrderByDescending(i => i))
        {
            output.Append(WriteTaxYearSummary(year, _tradeCalculationResult));
            IEnumerable<ITradeTaxCalculation> yearFilteredTradeCalculations = tradeTaxCalculations.Where(i => _taxYear.ToTaxYear(i.Date) == year && i.BuySell == Enum.TradeType.SELL)
                                                                                                 .OrderBy(i => i.Date);
            output.AppendLine();
            output.Append(WriteDisposalDetails(yearFilteredTradeCalculations));
            output.AppendLine();
        }
        output.AppendLine();
        return output.ToString();
    }

    private static string WriteTaxYearSummary(int year, TradeCalculationResult calculationResult)
    {
        StringBuilder output = new();
        IEnumerable<int> filter = new[] { year };
        output.AppendLine($"Summary for tax year {year}:");
        output.AppendLine($"Number of disposals: {calculationResult.NumberOfDisposals(filter)}");
        output.AppendLine($"Total disposal proceeds: {calculationResult.DisposalProceeds(filter):C0}");
        output.AppendLine($"Total allowable costs: {calculationResult.AllowableCosts(filter):C0}");
        output.AppendLine($"Total gains (excluding loss): {calculationResult.TotalGain(filter):C0}");
        output.AppendLine($"Total loss: {calculationResult.TotalLoss(filter):C0}");
        return output.ToString();
    }

    private string WriteDisposalDetails(IEnumerable<ITradeTaxCalculation> tradeTaxCalculations)
    {
        StringBuilder output = new();
        int DisposalCount = 1;
        foreach (var calculations in tradeTaxCalculations)
        {
            output.Append($"Disposal {DisposalCount}: Sold {calculations.TotalQty} units of {calculations.AssetName} on {calculations.Date.Date.ToString("dd-MMM-yyyy")} for {calculations.TotalNetAmount:C2}.\t");
            output.AppendLine($"Total gain (loss): {calculations.Gain:C2}");
            output.AppendLine(calculations.UnmatchedDescription());
            output.AppendLine($"Trade details:");
            foreach (var trade in calculations.TradeList)
            {
                output.AppendLine($"\t{trade.ToPrintedString()}");
            }
            output.AppendLine($"Trade matching:");
            foreach (var matching in calculations.MatchHistory)
            {
                if (matching.TradeMatchType == TaxMatchType.SECTION_104)
                {
                    output.AppendLine(matching.ToPrintedString(calculations, _section104Pools));
                }
                else output.AppendLine(matching.ToPrintedString());
            }
            output.AppendLine();
            DisposalCount++;
        }
        return output.ToString();
    }
}
