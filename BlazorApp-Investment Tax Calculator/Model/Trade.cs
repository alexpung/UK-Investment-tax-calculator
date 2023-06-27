using Enum;

namespace Model;

public record Trade : TaxEvent
{
    public required TradeType BuySell { get; set; }
    public required decimal Quantity { get; set; }
    public required DescribedMoney GrossProceed { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<DescribedMoney> Expenses { get; set; } = new List<DescribedMoney>();
    public decimal NetProceed
    {
        get
        {
            if (!Expenses.Any()) return GrossProceed.BaseCurrencyAmount;
            if (BuySell == TradeType.BUY) return GrossProceed.BaseCurrencyAmount + Expenses.Sum(expense => expense.BaseCurrencyAmount);
            else return GrossProceed.BaseCurrencyAmount - Expenses.Sum(expense => expense.BaseCurrencyAmount);
        }
    }
}

