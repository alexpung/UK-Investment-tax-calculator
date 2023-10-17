using Enum;
using Model.Interfaces;
using Model.TaxEvents;

namespace Model.UkTaxModel;

public class UkTradeCalculator : ITradeCalculator
{
    private readonly ITradeAndCorporateActionList _tradeList;
    private readonly UkSection104Pools _setion104Pools;

    public UkTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList)
    {
        _setion104Pools = section104Pools;
        _tradeList = tradeList;
    }

    public List<ITradeTaxCalculation> CalculateTax()
    {
        _setion104Pools.Clear();
        Dictionary<string, List<ITradeTaxCalculation>> tradeTaxCalculations = GroupTrade(_tradeList.Trades);
        foreach (KeyValuePair<string, List<ITradeTaxCalculation>> assetGroup in tradeTaxCalculations)
        {
            ApplySameDayMatchingRule(assetGroup.Value);
            ApplyBedAndBreakfastMatchingRule(assetGroup.Value);
            ProcessTradeInChronologicalOrder(assetGroup.Value, assetGroup.Key);
        }
        return tradeTaxCalculations.Values.SelectMany(i => i).ToList();
    }

    private static Dictionary<string, List<ITradeTaxCalculation>> GroupTrade(IEnumerable<Trade> trades)
    {
        var groupedTrade = from trade in trades
                           group trade by new { trade.AssetName, trade.Date.Date, trade.BuySell };
        IEnumerable<ITradeTaxCalculation> groupedTradeCalculations = groupedTrade.Select(group => new TradeTaxCalculation(group)).ToList();
        return groupedTradeCalculations.GroupBy(TradeTaxCalculation => TradeTaxCalculation.TradeList.First().AssetName).ToDictionary(group => group.Key, group => group.ToList());
    }

    private void ApplySameDayMatchingRule(IList<ITradeTaxCalculation> tradeTaxCalculations)
    {
        List<ITradeTaxCalculation> sortedList = tradeTaxCalculations.OrderBy(trade => trade.Date).ToList();
        for (int i = 0; i < sortedList.Count - 1; i++)
        {
            if (sortedList[i].Date.Date == sortedList[i + 1].Date.Date)
            {
                if (!((sortedList[i].BuySell == TradeType.SELL && sortedList[i + 1].BuySell == TradeType.BUY) || (sortedList[i].BuySell == TradeType.BUY && sortedList[i + 1].BuySell == TradeType.SELL)))
                {
                    throw new ArgumentException($"Unexpected same day matching with {sortedList[i]} and {sortedList[i + 1]}");
                }
                MatchTrade(sortedList[i], sortedList[i + 1], TaxMatchType.SAME_DAY);
            }
        }
    }

    /// <summary>
    /// Applies the "Bed and Breakfast" tax rule for trades, matching sell trades to buy trades
    /// that occur within a 30-day window.
    /// </summary>
    /// <param name="tradeTaxCalculations">List of trades to process.</param>
    private void ApplyBedAndBreakfastMatchingRule(IList<ITradeTaxCalculation> tradeTaxCalculations)
    {
        List<ITradeTaxCalculation> sortedList = tradeTaxCalculations.OrderBy(trade => trade.Date).ToList();

        for (int i = 0; i < sortedList.Count; i++)
        {
            var currentTrade = sortedList[i];

            // Skip trades that aren't BUYs
            if (currentTrade.BuySell != TradeType.BUY) continue;

            DateTime currentDate = currentTrade.Date.Date;

            for (int k = i - 1; k >= 0; k--)
            {
                var lookbackTrade = sortedList[k];
                var lookbackDate = lookbackTrade.Date.Date;

                // Break if the buy trade is more than 30 days after any sell trade
                if ((currentDate - lookbackDate).Days > 30) break;

                // Apply rule for matching SELL trades within the 30-day window
                if (lookbackTrade.BuySell == TradeType.SELL)
                {
                    MatchTrade(currentTrade, lookbackTrade, TaxMatchType.BED_AND_BREAKFAST);

                    if (currentTrade.CalculationCompleted) break;
                }
            }
        }
    }


    private List<StockSplit> CheckStockSplit(DateTime fromDate, DateTime toDate)
    {
        return _tradeList.CorporateActions.OfType<StockSplit>().Where(i => i.Date > fromDate && i.Date <= toDate).ToList();
    }

    private void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType)
    {
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        ITradeTaxCalculation earlierTrade = (trade1.Date <= trade2.Date) ? trade1 : trade2;
        ITradeTaxCalculation laterTrade = (trade1.Date <= trade2.Date) ? trade2 : trade1;
        decimal earlierTradeAdjustedUnmatchedQty = earlierTrade.UnmatchedQty;
        List<StockSplit> stockSplits = CheckStockSplit(earlierTrade.Date, laterTrade.Date);
        if (stockSplits != null)
        {
            foreach (StockSplit split in stockSplits)
            {
                earlierTradeAdjustedUnmatchedQty = split.GetSharesAfterSplit(earlierTradeAdjustedUnmatchedQty);
            }
        }
        decimal matchQuantity = Math.Min(earlierTradeAdjustedUnmatchedQty, laterTrade.UnmatchedQty);
        decimal shareMultiplier = earlierTradeAdjustedUnmatchedQty / earlierTrade.UnmatchedQty;
        decimal earlierTradeAdjustedmatchQuantity = matchQuantity / shareMultiplier;
        (_, WrappedMoney earlierTradeValue) = earlierTrade.MatchQty(earlierTradeAdjustedmatchQuantity);
        (_, WrappedMoney laterTradeValue) = laterTrade.MatchQty(matchQuantity);
        WrappedMoney acquisitionValue = earlierTrade.BuySell == TradeType.BUY ? earlierTradeValue : laterTradeValue;
        WrappedMoney disposalValue = earlierTrade.BuySell == TradeType.BUY ? laterTradeValue : earlierTradeValue;
        string additionalInfo = string.Empty;
        if (shareMultiplier != 1)
        {
            additionalInfo = $"{earlierTradeAdjustedmatchQuantity} units of the earlier trade is matched with {matchQuantity} units of later trade due to share split in between.";
        }
        earlierTrade.MatchHistory.Add(TradeMatch.CreateTradeMatch(taxMatchType, earlierTradeAdjustedmatchQuantity, acquisitionValue, disposalValue, laterTrade, additionalInfo));
        laterTrade.MatchHistory.Add(TradeMatch.CreateTradeMatch(taxMatchType, matchQuantity, acquisitionValue, disposalValue, earlierTrade, additionalInfo));
    }

    private void ProcessTradeInChronologicalOrder(IEnumerable<ITradeTaxCalculation> tradeTaxCalculations, string assetName)
    {
        List<ITradeTaxCalculation> unmatchedDisposal = new();
        UkSection104 section104 = _setion104Pools.GetExistingOrInitialise(assetName);
        IEnumerable<CorporateAction> corporateActions = _tradeList.CorporateActions.Where(i => i.AssetName == assetName);
        IEnumerable<object> taxEventsInChronologicalOrder = tradeTaxCalculations.Select(a => new { Item = (object)a, a.Date })
                          .Concat(corporateActions.Select(b => new { Item = (object)b, b.Date }))
                          .OrderBy(item => item.Date).Select(item => item.Item);
        foreach (object taxEvent in taxEventsInChronologicalOrder)
        {
            switch (taxEvent)
            {
                case ITradeTaxCalculation tradeTaxCalculation:
                    if (tradeTaxCalculation.CalculationCompleted) continue;
                    if (unmatchedDisposal.Any() && tradeTaxCalculation.BuySell == TradeType.BUY)
                    {
                        unmatchedDisposal.ForEach(unmatchedTrade => MatchTrade(unmatchedTrade, tradeTaxCalculation, TaxMatchType.SHORTCOVER));
                    }
                    section104.MatchTradeWithSection104(tradeTaxCalculation);
                    if (!tradeTaxCalculation.CalculationCompleted && tradeTaxCalculation.BuySell == TradeType.SELL)
                    {
                        unmatchedDisposal.Add(tradeTaxCalculation);
                    }
                    break;
                case CorporateAction action:
                    section104.PerformCorporateAction(action);
                    break;
                default:
                    throw new ArgumentException($"Unknown object {taxEvent} encountered when processing section104");
            }
        }
    }
}
