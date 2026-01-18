using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record CashSettlement : TaxEvent
{
    public required string Description { get; init; }
    public required DescribedMoney Amount { get; init; }
    public required TradeReason TradeReason { get; init; }
    public override string GetDuplicateSignature()
    {
        return $"CASH|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}|{Amount.FxRate}|{Description}|{TradeReason}";
    }

    public override string ToSummaryString() => $"Cash Settlement: {AssetName} ({Date.ToShortDateString()}) - {Amount.Amount}";
}
