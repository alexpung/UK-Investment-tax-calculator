using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public record TradePairSorter<T> where T : ITradeTaxCalculation
{
    public T EarlierTrade { get; init; }
    public T LatterTrade { get; init; }
    public T DisposalTrade { get; init; }
    public T AcqusitionTrade { get; init; }
    /// <summary>
    /// An acqusition trade is a buy trade with the exception of open short
    /// </summary>
    public T BuyTrade { get; init; }
    /// <summary>
    /// A disposal trade is the same as sell trade with the exception of close short
    /// </summary>
    public T SellTrade { get; init; }
    public decimal MatchQuantityAdjustmentFactor { get; set; } = 1;

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
        AcqusitionTrade = trade1.AcquisitionDisposal == TradeType.ACQUISITION ? trade1 : trade2;
        if (AcqusitionTrade is FutureTradeTaxCalculation { PositionType: PositionType.OPENSHORT })
        {
            BuyTrade = DisposalTrade;
            SellTrade = AcqusitionTrade;
        }
        else
        {
            BuyTrade = AcqusitionTrade;
            SellTrade = DisposalTrade;
        }
    }
}
