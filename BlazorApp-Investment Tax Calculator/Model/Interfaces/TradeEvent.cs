namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITradeEvent
{
    WrappedMoney NetProceedsAdjustment { get; }
    WrappedMoney AllowableCostAdjustment { get; }
    string Description { get; }
}