using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapitalGainCalculator.Model.UkTaxModel;

public class UkTradeCalculator : ICalculator
{
    private readonly TaxEventLists _taxEventList;
    private readonly UkSection104Pools _setion104Pools;
    private readonly List<TradeTaxCalculation> _unmatchedDisposal = new();

    public UkTradeCalculator(UkSection104Pools section104Pools, TaxEventLists taxEventLists)
    {
        _setion104Pools = section104Pools;
        _taxEventList = taxEventLists;
    }

    public CalculationResult CalculateTax()
    {
        _setion104Pools.Clear();
        _unmatchedDisposal.Clear();
        Dictionary<string, List<TradeTaxCalculation>> tradeTaxCalculations = GroupTrade(_taxEventList.Trades);
        foreach (KeyValuePair<string, List<TradeTaxCalculation>> assetGroup in tradeTaxCalculations)
        {
            ApplySameDayMatchingRule(assetGroup.Value);
            ApplyBedAndBreakfastMathingRule(assetGroup.Value);
            ProcessTradeInChronologicalOrder(assetGroup.Value, assetGroup.Key);
        }
        return new CalculationResult
        {
            CalculatedTrade = tradeTaxCalculations.Values.SelectMany(i => i).ToList(),
            UnmatchedDisposal = _unmatchedDisposal
        };
    }

    private static Dictionary<string, List<TradeTaxCalculation>> GroupTrade(IEnumerable<TaxEvent> taxEvents)
    {
        IEnumerable<Trade> trades = from taxEvent in taxEvents
                                    where taxEvent is Trade
                                    select (Trade)taxEvent;

        var groupedTrade = from trade in trades
                           group trade by new { trade.AssetName, trade.Date, trade.BuySell };
        var groupedTradeCalculations = groupedTrade.Select(group => new TradeTaxCalculation(group)).ToList();
        return groupedTradeCalculations.GroupBy(TradeTaxCalculation => TradeTaxCalculation.TradeList.First().AssetName).ToDictionary(group => group.Key, group => group.ToList());
    }

    private void ApplySameDayMatchingRule(IList<TradeTaxCalculation> tradeTaxCalculations)
    {
        List<TradeTaxCalculation> sortedList = tradeTaxCalculations.OrderBy(trade => trade.Date).ToList();
        for (int i = 0; i < sortedList.Count - 1; i++)
        {
            if (sortedList[i].Date == sortedList[i + 1].Date)
            {
                // No need to check BUY vs SELL. There should only be one of each in the same day after GroupTrade()
                MatchTrade(sortedList[i], sortedList[i + 1], UkMatchType.SAME_DAY);
            }
        }
    }

    private void ApplyBedAndBreakfastMathingRule(IList<TradeTaxCalculation> tradeTaxCalculations)
    {
        List<TradeTaxCalculation> sortedList = tradeTaxCalculations.OrderBy(trade => trade.Date).ToList();
        for (int i = 0; i < sortedList.Count - 1; i++)
        {
            if (sortedList[i].BuySell == TradeType.BUY)
            {
                int k = i - 1;
                // lookback a 30 days window
                while (k >= 0)
                {
                    if (sortedList[k].Date.AddDays(30) < sortedList[i].Date) break;
                    if (sortedList[k].BuySell == TradeType.SELL)
                    {
                        MatchTrade(sortedList[i], sortedList[k], UkMatchType.BREAD_AND_BREAKFAST);
                        if (sortedList[i].CalculationCompleted) break;
                    }
                    k--;
                }
            }
        }
    }

    private List<StockSplit> CheckStockSplit(DateTime fromDate, DateTime toDate)
    {
        return _taxEventList.CorporateActions.OfType<StockSplit>().Where(i => i.Date > fromDate && i.Date <= toDate).ToList();
    }

    private void MatchTrade(TradeTaxCalculation trade1, TradeTaxCalculation trade2, UkMatchType ukMatchType)
    {
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        decimal matchQuantity = Math.Min(trade1.UnmatchedQty, trade2.UnmatchedQty);
        decimal matchQuantityAfterActions = matchQuantity;
        TradeTaxCalculation earlierTrade;
        TradeTaxCalculation laterTrade;
        if (trade1.Date <= trade2.Date)
        {
            earlierTrade = trade1;
            laterTrade = trade2;
        }
        else
        {
            earlierTrade = trade2;
            laterTrade = trade1;
        }
        List<StockSplit> stockSplits = CheckStockSplit(earlierTrade.Date, laterTrade.Date);
        if (stockSplits != null)
        {
            foreach (StockSplit split in stockSplits)
            {
                matchQuantityAfterActions = split.GetSharesAfterSplit(matchQuantityAfterActions);
            }
        }
        (_, decimal trade1Value) = earlierTrade.MatchQty(matchQuantity);
        (_, decimal trade2Value) = laterTrade.MatchQty(matchQuantityAfterActions);
        decimal acquitionValue = trade1.BuySell == TradeType.BUY ? trade1Value : trade2Value;
        decimal disposalValue = trade1.BuySell == TradeType.BUY ? trade2Value : trade1Value;
        trade1.MatchHistory.Add(new TradeMatch()
        {
            TradeMatchType = ukMatchType,
            MatchQuantity = matchQuantity,
            BaseCurrencyMatchAcquitionValue = acquitionValue,
            BaseCurrencyMatchDisposalValue = disposalValue,
            MatchedGroup = trade2
        });
        trade2.MatchHistory.Add(new TradeMatch()
        {
            TradeMatchType = ukMatchType,
            MatchQuantity = matchQuantity,
            BaseCurrencyMatchAcquitionValue = acquitionValue,
            BaseCurrencyMatchDisposalValue = disposalValue,
            MatchedGroup = trade1
        });
    }

    private void ProcessTradeInChronologicalOrder(List<TradeTaxCalculation> tradeTaxCalculations, string assetName)
    {
        List<TradeTaxCalculation> sortedList = tradeTaxCalculations.OrderBy(trade => trade.Date).ToList();
        List<TradeTaxCalculation> unmatchedDisposal = new();
        UkSection104 section104 = _setion104Pools.GetExistingOrInitialise(assetName);
        List<CorporateAction> corporateActions = _taxEventList.CorporateActions.Where(i => i.AssetName == assetName).OrderBy(i => i.Date).ToList();
        foreach (TradeTaxCalculation trade in sortedList)
        {
            if (trade.CalculationCompleted) continue;
            if (corporateActions.Any())
            {
                List<CorporateAction> actionToBePerformed = corporateActions.Where(action => action.Date < trade.Date).ToList();
                foreach (CorporateAction action in actionToBePerformed)
                {
                    section104.PerformCorporateAction(action);
                    corporateActions.Remove(action);
                }
            }
            if (trade.BuySell == TradeType.SELL)
            {
                section104.MatchTradeWithSection104(trade);
                if (!trade.CalculationCompleted)
                {
                    unmatchedDisposal.Add(trade);
                }
            }
            else if (trade.BuySell == TradeType.BUY)
            {
                if (unmatchedDisposal.Any())
                {
                    unmatchedDisposal.ForEach(unmatchedTrade => MatchTrade(unmatchedTrade, trade, UkMatchType.SHORTCOVER));
                }
                if (!trade.CalculationCompleted)
                {
                    section104.MatchTradeWithSection104(trade);
                }
            }
        }
        _unmatchedDisposal.AddRange(unmatchedDisposal);
    }
}
