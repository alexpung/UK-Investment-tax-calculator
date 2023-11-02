using Enum;
using Model.TaxEvents;
using Model.UkTaxModel.Stocks;

namespace Model.Interfaces;
public interface ITradeTaxCalculation : ITextFilePrintable, IAssetDatedEvent
{
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    WrappedMoney TotalNetMoneyPaidOrReceived { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    WrappedMoney UnmatchedNetMoneyPaidOrReceived { get; }
    decimal UnmatchedQty { get; }
    WrappedMoney TotalProceeds { get; }
    WrappedMoney TotalAllowableCost { get; }
    WrappedMoney Gain { get; }
    WrappedMoney GetNetAmount(decimal qty) => TotalNetMoneyPaidOrReceived / TotalQty * qty;

    (decimal matchedQty, WrappedMoney matchedValue) MatchAll();
    (decimal matchedQty, WrappedMoney matchedValue) MatchQty(decimal demandedQty);
}