using Model.TaxEvents;
using Model.UkTaxModel.Stocks;

using System.Text;

namespace Model.UkTaxModel.Fx;

public class FxTradeTaxCalculation : TradeTaxCalculation
{
    public FxTradeTaxCalculation(IEnumerable<Trade> trades) : base(trades)
    {
    }

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"Dispose {TotalQty:0.##} units of {AssetName} on " +
            $"{Date.Date.ToString("dd-MMM-yyyy")} for {TotalCostOrProceed}.\t");
        output.AppendLine($"Total gain (loss): {Gain}");
        output.AppendLine(UnmatchedDescription());
        output.AppendLine($"Trade details:");
        foreach (var trade in TradeList)
        {
            output.AppendLine($"\t{trade.PrintToTextFile()}");
        }
        output.AppendLine($"Trade matching:");
        foreach (var matching in MatchHistory)
        {
            output.AppendLine(matching.PrintToTextFile());
        }
        if (MatchHistory.Count > 2)
        {
            output.AppendLine($"Resulting overall gain for this disposal: {GetSumFormula(MatchHistory.Select(match => match.MatchGain))}");
        }
        output.AppendLine();
        return output.ToString();
    }
}
