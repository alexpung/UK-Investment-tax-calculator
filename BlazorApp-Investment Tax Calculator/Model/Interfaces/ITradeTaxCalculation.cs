using Enum;
using NMoneys;

namespace Model.Interfaces;
public interface ITradeTaxCalculation : ITextFilePrintable
{
    string AssetName { get; }
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    Money TotalNetAmount { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    Money UnmatchedNetAmount { get; }
    decimal UnmatchedQty { get; }
    DateTime Date { get; }
    Money TotalProceeds { get; }
    Money TotalAllowableCost { get; }
    Money Gain { get; }

    (decimal matchedQty, Money matchedValue) MatchAll();
    (decimal matchedQty, Money matchedValue) MatchQty(decimal demandedQty);
}