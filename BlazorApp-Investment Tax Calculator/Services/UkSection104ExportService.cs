using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text;

namespace InvestmentTaxCalculator.Services;

public class UkSection104ExportService(ITaxYear taxYear, UkSection104Pools ukSection104Pools) : ITextFilePrintable
{
    public string PrintToTextFile(IEnumerable<int> yearsToExport)
    {
        StringBuilder output = new();
        output.AppendLine("Section 104 detail history:");
        foreach (var pool in ukSection104Pools.GetSection104s())
        {
            IEnumerable<int> activeYears = pool.Section104HistoryList.Select(i => taxYear.ToTaxYear(i.Date)).Distinct();
            if (!activeYears.Intersect(yearsToExport).Any()) continue; // skip if no activities in selected yearsToExport
            output.AppendLine($"Asset Name {pool.AssetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\t\tNew Value (change)\t\tContract value (for futures)");
            foreach (var history in pool.Section104HistoryList)
            {
                output.AppendLine(history.PrintToTextFile());
            }
        }
        return output.ToString();
    }

    public string PrintToTextFile()
    {
        StringBuilder output = new();
        output.AppendLine("Section 104 detail history:");
        foreach (var pool in ukSection104Pools.GetSection104s())
        {
            output.AppendLine($"Asset Name {pool.AssetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\t\tNew Value (change)\t\tContract value (for futures)");
            foreach (var history in pool.Section104HistoryList)
            {
                output.AppendLine(history.PrintToTextFile());
            }
        }
        return output.ToString();
    }
}
