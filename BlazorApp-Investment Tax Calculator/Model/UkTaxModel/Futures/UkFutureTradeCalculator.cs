using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Futures;

public class UkFutureTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, TradeTaxCalculationFactory tradeTaxCalculationFactory) : ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<FutureTradeTaxCalculation> tradeTaxCalculations = [.. tradeTaxCalculationFactory.GroupFutureTrade(tradeList.FutureContractTrades)];
        GroupedTradeContainer<FutureTradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, tradeList.CorporateActions);
        UkMatchingRules.ApplyUkTaxRuleSequence(MatchTrade, _tradeContainer, section104Pools);
        return [.. tradeTaxCalculations.Cast<ITradeTaxCalculation>()];
    }

    private void MatchTrade(FutureTradeTaxCalculation trade1, FutureTradeTaxCalculation trade2, TaxMatchType taxMatchType, TaxableStatus taxableStatus)
    {
        TradePairSorter<FutureTradeTaxCalculation> tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        MatchAdjustment matchAdjustment = tradeList.CorporateActions
            .Aggregate(new MatchAdjustment(), (matchAdjustment, corporateAction) => corporateAction.TradeMatching(trade1, trade2, matchAdjustment));
        tradePairSorter.SetQuantityAdjustmentFactor(matchAdjustment.MatchAdjustmentFactor);
        WrappedMoney buyContractValue = tradePairSorter.BuyTrade.GetProportionedContractValue(tradePairSorter.BuyMatchQuantity);
        WrappedMoney sellContractValue = tradePairSorter.SellTrade.GetProportionedContractValue(tradePairSorter.SellMatchQuantity);
        WrappedMoney contractGain = sellContractValue - buyContractValue;
        WrappedMoney contractGainInBaseCurrency = new((contractGain * tradePairSorter.DisposalTrade.ContractFxRate).Amount);
        WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceed(tradePairSorter.AcquisitionMatchQuantity)
            + tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(tradePairSorter.DisposalMatchQuantity);
        WrappedMoney disposalProceed = WrappedMoney.GetBaseCurrencyZero();
        if (contractGainInBaseCurrency.Amount < 0)
        {
            allowableCost += contractGainInBaseCurrency * -1;
        }
        else
        {
            disposalProceed += contractGainInBaseCurrency;
        }
        FutureTradeMatch disposalTradeMatch = new()
        {
            Date = DateOnly.FromDateTime(tradePairSorter.DisposalTrade.Date),
            AssetName = tradePairSorter.DisposalTrade.AssetName,
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = tradePairSorter.AcquisitionMatchQuantity,
            MatchDisposalQty = tradePairSorter.DisposalMatchQuantity,
            BaseCurrencyMatchAllowableCost = allowableCost,
            BaseCurrencyMatchDisposalProceed = disposalProceed,
            MatchedBuyTrade = tradePairSorter.AcquisitionTrade,
            MatchedSellTrade = tradePairSorter.DisposalTrade,
            AdditionalInformation = "",
            MatchBuyContractValue = buyContractValue,
            MatchSellContractValue = sellContractValue,
            BaseCurrencyAcquisitionDealingCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceed(tradePairSorter.AcquisitionMatchQuantity),
            BaseCurrencyDisposalDealingCost = tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(tradePairSorter.DisposalMatchQuantity),
            ClosingFxRate = tradePairSorter.DisposalTrade.ContractFxRate,
            IsTaxable = taxableStatus
        };
        tradePairSorter.DisposalTrade.MatchHistory.Add(disposalTradeMatch);
        tradePairSorter.AcquisitionTrade.MatchQty(tradePairSorter.AcquisitionMatchQuantity);
        tradePairSorter.DisposalTrade.MatchQty(tradePairSorter.DisposalMatchQuantity);
    }
}

