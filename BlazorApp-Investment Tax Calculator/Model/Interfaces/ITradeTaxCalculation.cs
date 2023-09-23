using Enum;

namespace Model.Interfaces;
public interface ITradeTaxCalculation : ITextFilePrintable
{
    string AssetName { get; }
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    WrappedMoney TotalNetAmount { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    WrappedMoney UnmatchedNetAmount { get; }
    decimal UnmatchedQty { get; }
    DateTime Date { get; }
    WrappedMoney TotalProceeds { get; }
    WrappedMoney TotalAllowableCost { get; }
    WrappedMoney Gain { get; }

    (decimal matchedQty, WrappedMoney matchedValue) MatchAll();
    (decimal matchedQty, WrappedMoney matchedValue) MatchQty(decimal demandedQty);
}