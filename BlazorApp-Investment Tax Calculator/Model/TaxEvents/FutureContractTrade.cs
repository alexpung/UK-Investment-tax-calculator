using Enum;
using Model;
using Model.TaxEvents;

namespace TaxEvents;

public record FutureContractTrade : Trade
{
    public required DescribedMoney ContractValue { get; set; }

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
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}, {netExplanation}"
            + GetExpensesExplanation();
    }
}
