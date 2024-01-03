using Enum;

using Model.Interfaces;

using System.Collections.Immutable;
using System.Text;

namespace Model.TaxEvents;

public record Trade : TaxEvent, ITextFilePrintable
{
    public virtual AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.STOCK;
    public virtual required TradeType BuySell { get; set; }
    public virtual required decimal Quantity { get; set; }
    public virtual required DescribedMoney GrossProceed { get; set; }
    public string Description { get; set; } = string.Empty;
    public ImmutableList<DescribedMoney> Expenses { get; init; } = [];
    public virtual WrappedMoney NetProceed
    {
        get
        {
            if (Expenses.IsEmpty) return GrossProceed.BaseCurrencyAmount;
            if (BuySell == TradeType.BUY) return GrossProceed.BaseCurrencyAmount + Expenses.Select(i => i.BaseCurrencyAmount).Sum();
            else return GrossProceed.BaseCurrencyAmount - Expenses.Select(i => i.BaseCurrencyAmount).Sum();
        }
    }

    public decimal RawQuantity => BuySell switch
    {
        TradeType.BUY => Quantity,
        TradeType.SELL => Quantity * -1,
        _ => throw new NotImplementedException($"Unknown trade type {BuySell}"),
    };

    protected string GetExpensesExplanation()
    {
        if (Expenses.IsEmpty) return string.Empty;
        StringBuilder stringBuilder = new();
        stringBuilder.Append("\n\tExpenses: ");
        foreach (var expense in Expenses)
        {
            stringBuilder.Append(expense.PrintToTextFile() + "\t");
        }
        return stringBuilder.ToString();
    }

    public virtual string PrintToTextFile()
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
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy HH:mm} for {GrossProceed.PrintToTextFile()} " +
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}, {netExplanation}"
            + GetExpensesExplanation();
    }
}

