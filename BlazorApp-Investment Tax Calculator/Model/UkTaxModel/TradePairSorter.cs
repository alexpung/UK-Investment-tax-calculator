using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public record TradePairSorter<T> where T : ITradeTaxCalculation
{
    public T EarlierTrade { get; init; }
    public T LatterTrade { get; init; }
    public T DisposalTrade { get; init; }
    public T AcquisitionTrade { get; init; }
    /// <summary>
    /// An Acquisition trade is a buy trade with the exception of open short
    /// </summary>
    public T BuyTrade { get; init; }
    /// <summary>
    /// A disposal trade is the same as sell trade with the exception of close short
    /// </summary>
    public T SellTrade { get; init; }
    private decimal _matchQuantityAdjustmentFactor = 1;
    private decimal _matchQuantity => Math.Min(EarlierTrade.UnmatchedQty, LatterTrade.UnmatchedQty) / _matchQuantityAdjustmentFactor;
    public decimal AcquisitionMatchQuantity => EarlierTrade.AcquisitionDisposal == TradeType.ACQUISITION ? _matchQuantity : _matchQuantity * _matchQuantityAdjustmentFactor;
    public decimal DisposalMatchQuantity => EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL ? _matchQuantity : _matchQuantity * _matchQuantityAdjustmentFactor;
    public decimal BuyMatchQuantity => EarlierTrade.AcquisitionDisposal == BuyTrade.AcquisitionDisposal ? _matchQuantity : _matchQuantity * _matchQuantityAdjustmentFactor;
    public decimal SellMatchQuantity => EarlierTrade.AcquisitionDisposal == SellTrade.AcquisitionDisposal ? _matchQuantity : _matchQuantity * _matchQuantityAdjustmentFactor;
    public TradePairSorter(T trade1, T trade2)
    {
        if (!(
            (trade1.AcquisitionDisposal == TradeType.ACQUISITION && trade2.AcquisitionDisposal == TradeType.DISPOSAL) ||
            (trade1.AcquisitionDisposal == TradeType.DISPOSAL && trade2.AcquisitionDisposal == TradeType.ACQUISITION)
            ))
        {
            throw new ArgumentException("The provided trades should consist of one buy and one sell trade.");
        }
        EarlierTrade = trade1.Date > trade2.Date ? trade2 : trade1;
        LatterTrade = trade1.Date > trade2.Date ? trade1 : trade2;
        DisposalTrade = trade1.AcquisitionDisposal == TradeType.DISPOSAL ? trade1 : trade2;
        AcquisitionTrade = trade1.AcquisitionDisposal == TradeType.ACQUISITION ? trade1 : trade2;
        if (AcquisitionTrade is FutureTradeTaxCalculation { PositionType: PositionType.OPENSHORT })
        {
            BuyTrade = DisposalTrade;
            SellTrade = AcquisitionTrade;
        }
        else
        {
            BuyTrade = AcquisitionTrade;
            SellTrade = DisposalTrade;
        }
    }

    public void SetQuantityAdjustmentFactor(decimal factor)
    {
        if (factor == 0) throw new ArgumentException("Adjustment factor cannot be zero");
        _matchQuantityAdjustmentFactor = factor;
    }
}
