using Enum;

using Model;
using Model.TaxEvents;

using System.Collections.Immutable;

namespace TaxEvents;

public record FutureContractTrade : Trade
{
    public required DescribedMoney ContractValue { get; set; }
    public override AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.FUTURE;
    public FuturePositionType FuturePositionType { get; set; }

    /// <summary>
    /// Create a copy of the trade with quantity that is part of the whole trade
    /// e.g. You buy 5 contract of a future. You can split it by using SplitTrade(2m) and SplitTrade(3m)
    /// </summary>
    /// <param name="qty"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public FutureContractTrade SplitTrade(decimal qty)
    {
        decimal splitRatio = qty / Quantity;
        if (qty > Quantity) throw new ArgumentException($"Proportioned trade quantity {qty} must be less than total quantity {Quantity}");
        return this with
        {
            Quantity = qty,
            Description = $"Part of a trade with quantity {Quantity}",
            GrossProceed = GrossProceed with { Amount = GrossProceed.Amount * splitRatio },
            ContractValue = ContractValue with { Amount = ContractValue.Amount * splitRatio },
            Expenses = Expenses.Select(expense => expense with { Amount = expense.Amount * splitRatio }).ToImmutableList()
        };
    }

    public override string PrintToTextFile()
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
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy HH:mm} with contract value {ContractValue.Amount} " +
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}"
            + GetExpensesExplanation();
    }
}
