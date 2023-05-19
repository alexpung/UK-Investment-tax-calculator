using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private string WriteTaxYearSummary(int year, TradeCalculationResult calculationResult)
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
        StringBuilder output = new StringBuilder();
        int DisposalCount = 1;
        foreach (var calculations in tradeTaxCalculations)
        {
            output.Append($"Disposal {DisposalCount}: Sold {calculations.TotalQty} units of {calculations.AssetName} on {calculations.Date.Date.ToString("dd-MMM-yyyy")} for {calculations.TotalNetAmount:C2}.\t");
            output.AppendLine($"Total gain (loss): {calculations.Gain:C2}");
            output.AppendLine(UnmatchedDescription(calculations));
            output.AppendLine($"Trade details:");
            foreach (var trade in calculations.TradeList)
            {
                output.AppendLine($"\t{trade}");
            }
            output.AppendLine($"Trade matching:");
            foreach (var matching in calculations.MatchHistory)
            {
                output.AppendLine(TradeMatchDispatcher(matching, calculations));
            }
            output.AppendLine();
            DisposalCount++;
        }
        return output.ToString();
    }

    private string UnmatchedDescription(ITradeTaxCalculation tradeTaxCalculation) => tradeTaxCalculation.UnmatchedQty switch
    {
        0 => "All units of the disposals are matched with acquitions",
        > 0 => $"{tradeTaxCalculation.UnmatchedQty} units of disposals are not matched (short sale).",
        _ => throw new NotImplementedException()
    };

    private string TradeMatchDispatcher(TradeMatch tradeMatch, ITradeTaxCalculation calculation) => tradeMatch.TradeMatchType switch
    {
        UkMatchType.SECTION_104 => PrintSection104Match(tradeMatch, calculation),
        _ => PrintTradeMatch(tradeMatch)
    };

    private string PrintSection104Match(TradeMatch tradeMatch, ITradeTaxCalculation calculation)
    {
        StringBuilder output = new StringBuilder();
        List<Section104History> section104Histories = _section104Pools.GetHistory(calculation);
        output.AppendLine($"At time of disposal, section 104 contains {section104Histories.Last().OldQuantity} units with value {section104Histories.Last().OldValue:C4}");
        output.AppendLine($"Section 104: Matched {tradeMatch.MatchQuantity} units of the disposal. Acquition cost is {tradeMatch.BaseCurrencyMatchAcquitionValue:C4}");
        output.AppendLine($"Gain for this match is {tradeMatch.BaseCurrencyMatchDisposalValue:C2} - {tradeMatch.BaseCurrencyMatchAcquitionValue:C2} " +
                            $"= {tradeMatch.BaseCurrencyMatchDisposalValue - tradeMatch.BaseCurrencyMatchAcquitionValue:C2}");
        output.AppendLine();
        return output.ToString();
    }

    private string PrintTradeMatch(TradeMatch tradeMatch)
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine($"{PrettyPrintTradeType(tradeMatch)}: Matched {tradeMatch.MatchQuantity} units of the disposal. Acquition cost is {tradeMatch.BaseCurrencyMatchAcquitionValue:C4}");
        output.AppendLine($"Matched trade: {string.Join("\n", tradeMatch.MatchedGroup!.TradeList)}");
        output.AppendLine($"Gain for this match is {tradeMatch.BaseCurrencyMatchDisposalValue:C2} - {tradeMatch.BaseCurrencyMatchAcquitionValue:C2} " +
                            $"= {tradeMatch.BaseCurrencyMatchDisposalValue - tradeMatch.BaseCurrencyMatchAcquitionValue:C2}");
        output.AppendLine();
        return output.ToString();
    }

    private string PrettyPrintTradeType(TradeMatch tradeMatch) => tradeMatch.TradeMatchType switch
    {
        UkMatchType.SAME_DAY => "Same day",
        UkMatchType.BED_AND_BREAKFAST => "Bed and breakfast",
        UkMatchType.SHORTCOVER => "Cover unmatched disposal",
        UkMatchType.SECTION_104 => "Section 104",
        _ => throw new NotImplementedException()
    };
}
