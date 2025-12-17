using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text;

namespace InvestmentTaxCalculator.Services;

public class UkSection104ExportService(UkSection104Pools ukSection104Pools) : ITextFilePrintable
{
    public string PrintToTextFile(IEnumerable<int> yearsToExport)
    {
        StringBuilder output = new();
        if (!yearsToExport.Any())
        {
            return string.Empty;
        }
        int endReportYear = yearsToExport.Max();
        output.AppendLine("Section 104 detail history:");
        foreach (string assetName in ukSection104Pools.GetActiveSection104s(yearsToExport).Select(pool => pool.AssetName))
        {
            output.AppendLine($"Asset Name {assetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\t\tNew Value (change)\t\tContract value (for futures)");
            foreach (var history in ukSection104Pools.GetSection104HistoriesUnitlTaxYear(endReportYear, assetName))
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
