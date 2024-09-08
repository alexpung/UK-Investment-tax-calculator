using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

public record TradePairSorter
{
    public ITradeTaxCalculation EarlierTrade { get; init; }
    public ITradeTaxCalculation LatterTrade { get; init; }
    public ITradeTaxCalculation DisposalTrade { get; init; }
    public ITradeTaxCalculation AcqusitionTrade { get; init; }

    public TradePairSorter(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2)
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
    }
}
