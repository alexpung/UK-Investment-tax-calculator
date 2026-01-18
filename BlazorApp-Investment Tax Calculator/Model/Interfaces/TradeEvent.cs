namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITradeEvent
{
    /// <summary>
    /// Adjustment to the net proceeds of the trade (e.g. premium received/paid for options).
    /// </summary>
    WrappedMoney NetProceedsAdjustment { get; }

    /// <summary>
    /// Adjustment to the allowable cost of the trade (e.g. rolled up premium from options).
    /// </summary>
    WrappedMoney AllowableCostAdjustment { get; }

    string Description { get; }
}
