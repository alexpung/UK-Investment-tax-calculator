using Enum;
using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using System.Text;

namespace Services;

public static class ObjectDetailsToPrintedString
{
    public static string ToPrintedString(this Trade trade)
    {
        static string GetExpensesExplanation(Trade trade)
        {
            if (!trade.Expenses.Any()) return string.Empty;
            StringBuilder stringBuilder = new();
            stringBuilder.Append("\n\tExpenses: ");
            foreach (var expense in trade.Expenses)
            {
                stringBuilder.Append(ToPrintedString(expense) + "\t");
            }
            return stringBuilder.ToString();
        }

        string action = trade.BuySell switch
        {
            TradeType.BUY => "Bought",
            TradeType.SELL => "Sold",
            _ => throw new NotImplementedException()
        };
        string netExplanation = trade.BuySell switch
        {
            TradeType.BUY => $"Total cost: {trade.NetProceed:C2}",
            TradeType.SELL => $"Net proceed: {trade.NetProceed:C2}",
            _ => throw new NotImplementedException()
        };
        return $"{action} {trade.Quantity} unit(s) of {trade.AssetName} on {trade.Date:dd-MMM-yyyy} for {trade.GrossProceed.BaseCurrencyAmount:C2} " +
            $"with total expense {trade.Expenses.Sum(i => i.BaseCurrencyAmount):C2}, {netExplanation}"
            + GetExpensesExplanation(trade);
    }

    public static string ToPrintedString(this DescribedMoney describedMoney)
    {
        string outputString;
        if (describedMoney.Description == string.Empty) outputString = $"{describedMoney.Amount}";
        else outputString = $"{describedMoney.Description}: {describedMoney.Amount}";
        if (describedMoney.FxRate == 1)
        {
            return outputString;
        }
        else return $"{outputString} = {describedMoney.BaseCurrencyAmount:C2} Fx rate = {describedMoney.FxRate}";
    }

    public static string ToPrintedString(this Section104History section104History)
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine($"{section104History.Date.ToShortDateString()}\t{section104History.OldQuantity + section104History.QuantityChange} ({section104History.QuantityChange:+#.##;-#.##;+0})\t\t\t" +
            $"{section104History.OldValue + section104History.ValueChange:C2} ({section104History.ValueChange:+#.##;-#.##;+0})\t\t");
        if (section104History.Explanation != string.Empty)
        {
            output.AppendLine($"{section104History.Explanation}");
        }
        if (section104History?.TradeTaxCalculation?.TradeList is not null)
        {
            output.AppendLine("Involved trades:");
            foreach (var trade in section104History.TradeTaxCalculation.TradeList)
            {
                output.AppendLine(ToPrintedString(trade));
            }
        }
        return output.ToString();
    }

    public static string ToPrintedString(this Dividend dividend)
    {
        return $"Asset Name: {dividend.AssetName}, " +
                $"Date: {dividend.Date.ToShortDateString()}, " +
                $"Type: {dividend.DividendType.ToPrintedString()}, " +
                $"Amount: {dividend.Proceed.Amount}, " +
                $"FxRate: {dividend.Proceed.FxRate}, " +
                $"Sterling Amount: {dividend.Proceed.BaseCurrencyAmount:C2}, " +
                $"Description: {dividend.Proceed.Description}";
    }

    public static string ToPrintedString(this DividendType dividendType) => dividendType switch
    {
        DividendType.WITHHOLDING => "Withholding Tax",
        DividendType.DIVIDEND_IN_LIEU => "Payment In Lieu of a Dividend",
        DividendType.DIVIDEND => "Dividend",
        _ => throw new NotImplementedException() //SHould not get a dividend object with any other type.
    };

    public static string ToPrintedString(this TaxMatchType TaxMatchType) => TaxMatchType switch
    {
        TaxMatchType.SAME_DAY => "Same day",
        TaxMatchType.BED_AND_BREAKFAST => "Bed and breakfast",
        TaxMatchType.SHORTCOVER => "Cover unmatched disposal",
        TaxMatchType.SECTION_104 => "Section 104",
        _ => throw new NotImplementedException()
    };

    public static string ToPrintedString(this TradeMatch tradeMatch)
    {
        StringBuilder output = new();
        output.AppendLine($"{tradeMatch.TradeMatchType.ToPrintedString()}: Matched {tradeMatch.MatchQuantity} units of the disposal. Acquition cost is {tradeMatch.BaseCurrencyMatchAcquitionValue:C4}");
        output.AppendLine($"Matched trade: {string.Join("\n", tradeMatch.MatchedGroup!.TradeList.Select(trade => trade.ToPrintedString()))}");
        output.AppendLine($"Gain for this match is {tradeMatch.BaseCurrencyMatchDisposalValue:C2} - {tradeMatch.BaseCurrencyMatchAcquitionValue:C2} " +
                            $"= {tradeMatch.BaseCurrencyMatchDisposalValue - tradeMatch.BaseCurrencyMatchAcquitionValue:C2}");
        output.AppendLine();
        return output.ToString();
    }

    public static string ToPrintedString(this TradeMatch tradeMatch, ITradeTaxCalculation calculation, UkSection104Pools section104Pools)
    {
        StringBuilder output = new StringBuilder();
        List<Section104History> section104Histories = section104Pools.GetHistory(calculation);
        output.AppendLine($"At time of disposal, section 104 contains {section104Histories.Last().OldQuantity} units with value {section104Histories.Last().OldValue:C4}");
        output.AppendLine($"Section 104: Matched {tradeMatch.MatchQuantity} units of the disposal. Acquition cost is {tradeMatch.BaseCurrencyMatchAcquitionValue:C4}");
        output.AppendLine($"Gain for this match is {tradeMatch.BaseCurrencyMatchDisposalValue:C2} - {tradeMatch.BaseCurrencyMatchAcquitionValue:C2} " +
                            $"= {tradeMatch.BaseCurrencyMatchDisposalValue - tradeMatch.BaseCurrencyMatchAcquitionValue:C2}");
        output.AppendLine();
        return output.ToString();
    }

    public static string UnmatchedDescription(this ITradeTaxCalculation tradeTaxCalculation) => tradeTaxCalculation.UnmatchedQty switch
    {
        0 => "All units of the disposals are matched with acquitions",
        > 0 => $"{tradeTaxCalculation.UnmatchedQty} units of disposals are not matched (short sale).",
        _ => throw new NotImplementedException()
    };
}
