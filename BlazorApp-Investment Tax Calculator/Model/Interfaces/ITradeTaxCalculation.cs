using Enum;

namespace Model.Interfaces;
public interface ITradeTaxCalculation
{
    string AssetName { get; }
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    decimal TotalNetAmount { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    decimal UnmatchedNetAmount { get; }
    decimal UnmatchedQty { get; }
    DateTime Date { get; }
    decimal TotalProceeds { get; }
    decimal TotalAllowableCost { get; }
    decimal Gain { get; }

    (decimal matchedQty, decimal matchedValue) MatchAll();
    (decimal matchedQty, decimal matchedValue) MatchQty(decimal demandedQty);
}