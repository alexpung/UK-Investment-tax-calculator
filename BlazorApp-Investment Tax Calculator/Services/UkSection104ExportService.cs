using Model.Interfaces;
using Model.UkTaxModel;
using System.Text;

namespace Services;

public class UkSection104ExportService : ITextFilePrintable
{
    private readonly ITaxYear _taxYear;
    private readonly UkSection104Pools _section104Pools;

    public UkSection104ExportService(ITaxYear taxYear, UkSection104Pools ukSection104Pools)
    {
        _taxYear = taxYear;
        _section104Pools = ukSection104Pools;
    }

    public string PrintToTextFile(IEnumerable<int> yearsToExport)
    {
        StringBuilder output = new();
        output.AppendLine("Section 104 detail history:");
        foreach (var pool in _section104Pools.GetSection104s())
        {
            IEnumerable<int> activeYears = pool.Section104HistoryList.Select(i => _taxYear.ToTaxYear(i.Date)).Distinct();
            if (!activeYears.Intersect(yearsToExport).Any()) continue; // skip if no activities in selected yearsToExport
            output.AppendLine($"Asset Name {pool.AssetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\t\tNew Value (change)");
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
        foreach (var pool in _section104Pools.GetSection104s())
        {
            output.AppendLine($"Asset Name {pool.AssetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\t\tNew Value (change)");
            foreach (var history in pool.Section104HistoryList)
            {
                output.AppendLine(history.PrintToTextFile());
            }
        }
        return output.ToString();
    }
}
