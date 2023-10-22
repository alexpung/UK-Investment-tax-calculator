using Enum;
using Model.TaxEvents;

namespace Model.Interfaces;
public interface ITradeTaxCalculation : ITextFilePrintable, IAssetDatedEvent
{
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    WrappedMoney TotalNetAmount { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    WrappedMoney UnmatchedNetAmount { get; }
    decimal UnmatchedQty { get; }
    WrappedMoney TotalProceeds { get; }
    WrappedMoney TotalAllowableCost { get; }
    WrappedMoney Gain { get; }
    WrappedMoney GetNetAmount(decimal qty) => TotalNetAmount / TotalQty * qty;

    (decimal matchedQty, WrappedMoney matchedValue) MatchAll();
    (decimal matchedQty, WrappedMoney matchedValue) MatchQty(decimal demandedQty);
}