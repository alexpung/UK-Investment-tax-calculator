using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record CashSettlement : TaxEvent
{
    public required string Description { get; init; }
    public required WrappedMoney Amount { get; init; }
    public required TradeReason TradeReason { get; init; }
}
