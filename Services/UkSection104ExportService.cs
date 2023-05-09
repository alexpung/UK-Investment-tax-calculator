using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CapitalGainCalculator.Services;

public class UkSection104ExportService
{
    private readonly ITaxYear _taxYear;
    private readonly UkSection104Pools _section104Pools;

    public UkSection104ExportService(ITaxYear taxYear, UkSection104Pools ukSection104Pools)
    {
        _taxYear = taxYear;
        _section104Pools = ukSection104Pools;
    }

    public string Export(IEnumerable<int> yearsToExport)
    {
        StringBuilder output = new();
        output.AppendLine("Section 104 detail history:");
        foreach (var pool in _section104Pools.GetSection104s())
        {
            IEnumerable<int> activeYears = pool.Section104HistoryList.Select(i => _taxYear.ToTaxYear(i.Date)).Distinct();
            if (!activeYears.Intersect(yearsToExport).Any()) continue; // skip if no activities in selected yearsToExport
            output.AppendLine($"Asset Name {pool.AssetName}");
            output.AppendLine("Date\t\tNew Quantity (change)\tNew Value (change)");
            foreach (var history in pool.Section104HistoryList)
            {
                output.AppendLine(PrettyPrintSection104History(history));
            }
        }
        return output.ToString();
    }

    private string PrettyPrintSection104History(Section104History section104History)
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine($"{section104History.Date.ToShortDateString()}\t{section104History.OldQuantity + section104History.QuantityChange} ({section104History.QuantityChange:+#.##;-#.##;+0})\t\t\t" +
            $"{section104History.OldValue + section104History.ValueChange:C2} ({section104History.ValueChange:+#.##;-#.##;+0})\t\t");
        output.AppendLine($"{section104History.Explanation}");
        return output.ToString();
    }
}
