using Enum;
using Model.Interfaces;
using System.Text;

namespace Model;

public record Trade : TaxEvent, ITextFilePrintable
{
    public virtual required TradeType BuySell { get; set; }
    public virtual required decimal Quantity { get; set; }
    public virtual required DescribedMoney GrossProceed { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<DescribedMoney> Expenses { get; set; } = new List<DescribedMoney>();
    public virtual WrappedMoney NetProceed
    {
        get
        {
            if (!Expenses.Any()) return GrossProceed.BaseCurrencyAmount;
            if (BuySell == TradeType.BUY) return GrossProceed.BaseCurrencyAmount + Expenses.Select(i => i.BaseCurrencyAmount).Sum();
            else return GrossProceed.BaseCurrencyAmount - Expenses.Select(i => i.BaseCurrencyAmount).Sum();
        }
    }

    private string GetExpensesExplanation()
    {
        if (!Expenses.Any()) return string.Empty;
        StringBuilder stringBuilder = new();
        stringBuilder.Append("\n\tExpenses: ");
        foreach (var expense in Expenses)
        {
            stringBuilder.Append(expense.PrintToTextFile() + "\t");
        }
        return stringBuilder.ToString();
    }

    public string PrintToTextFile()
    {
        string action = BuySell switch
        {
            TradeType.BUY => "Bought",
            TradeType.SELL => "Sold",
            _ => throw new NotImplementedException()
        };
        string netExplanation = BuySell switch
        {
            TradeType.BUY => $"Total cost: {NetProceed}",
            TradeType.SELL => $"Net proceed: {NetProceed}",
            _ => throw new NotImplementedException()
        };
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy} for {GrossProceed.BaseCurrencyAmount} " +
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}, {netExplanation}"
            + GetExpensesExplanation();
    }
}

