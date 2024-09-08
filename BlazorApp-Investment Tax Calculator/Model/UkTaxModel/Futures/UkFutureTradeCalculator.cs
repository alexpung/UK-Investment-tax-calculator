using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using Syncfusion.Blazor.Data;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Futures;

public class UkFutureTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList) : ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<FutureTradeTaxCalculation> tradeTaxCalculations = [.. GroupTrade(tradeList.Trades)];
        GroupedTradeContainer<FutureTradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, tradeList.CorporateActions);
        foreach (var match in UkMatchingRules.ApplySameDayMatchingRule(_tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.SAME_DAY);
        }
        foreach (var match in UkMatchingRules.ApplyBedAndBreakfastMatchingRule(_tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.BED_AND_BREAKFAST);
        }
        foreach (var match in UkMatchingRules.ProcessTradeInChronologicalOrder(section104Pools, _tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.SHORTCOVER);
        }
        return tradeTaxCalculations.Cast<ITradeTaxCalculation>().ToList();
    }

    /// <summary>
    /// A trade can be a opening a position, closing a position or closing and reopen a position in opposite direction.
    /// For each trade calculate how much of the trade is opening and closing a position.
    /// </summary>
    /// <param name="trades"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static List<FutureTradeTaxCalculation> GroupTrade(IEnumerable<Trade> trades)
    {
        List<FutureTradeTaxCalculation> groupedTrade = [];
        foreach (var tradeGroup in GroupFutureContractTradeByAssetName(trades))
        {
            List<FutureContractTrade> taggedTrades = UkMatchingRules.TagTradesWithOpenClose(tradeGroup);
            // trade.AssetName grouping is required as short positions is treated as a separate asset
            groupedTrade.AddRange(taggedTrades.GroupBy(trade => (trade.Date.Date, trade.AcquisitionDisposal, trade.AssetName)).Select(trades => new FutureTradeTaxCalculation(trades)));
        }
        return groupedTrade;
    }

    private static IEnumerable<IGrouping<string, FutureContractTrade>> GroupFutureContractTradeByAssetName(IEnumerable<Trade> trades)
    {
        return from trade in trades
               where trade is FutureContractTrade
               group trade as FutureContractTrade by trade.AssetName;
    }

    private void MatchTrade(FutureTradeTaxCalculation trade1, FutureTradeTaxCalculation trade2, TaxMatchType taxMatchType)
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
            ClosingFxRate = tradePairSorter.DisposalTrade.ContractFxRate
        };
        tradePairSorter.DisposalTrade.MatchHistory.Add(disposalTradeMatch);
        tradePairSorter.AcquisitionTrade.MatchQty(tradePairSorter.AcquisitionMatchQuantity);
        tradePairSorter.DisposalTrade.MatchQty(tradePairSorter.DisposalMatchQuantity);
    }
}

