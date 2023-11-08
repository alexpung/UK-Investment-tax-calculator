using Enum;

using Model.Interfaces;
using Model.TaxEvents;

using Syncfusion.Blazor.Data;

namespace Model.UkTaxModel.Stocks;

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
        Dictionary<string, List<ITradeTaxCalculation>> tradeTaxCalculations = GroupTrade(_tradeList.Trades);
        // This is a Dict grouped by asset name. For each asset name process all trades.
        foreach (KeyValuePair<string, List<ITradeTaxCalculation>> assetGroup in tradeTaxCalculations)
        {
            IEnumerable<CorporateAction> corporateActions = _tradeList.CorporateActions.Where(i => i.AssetName == assetGroup.Key);
            List<IAssetDatedEvent> taxEventsInChronologicalOrder = assetGroup.Value.Select(a => new { Item = (IAssetDatedEvent)a, a.Date })
                              .Concat(corporateActions.Select(b => new { Item = (IAssetDatedEvent)b, b.Date }))
                              .OrderBy(item => item.Date).Select(item => item.Item).ToList();
            ApplySameDayMatchingRule(taxEventsInChronologicalOrder);
            ApplyBedAndBreakfastMatchingRule(taxEventsInChronologicalOrder);
            ProcessTradeInChronologicalOrder(assetGroup.Key, taxEventsInChronologicalOrder);
        }
        return tradeTaxCalculations.Values.SelectMany(i => i).ToList();
    }

    private static Dictionary<string, List<ITradeTaxCalculation>> GroupTrade(IEnumerable<Trade> trades)
    {
        var groupedTrade = from trade in trades
                           where trade.AssetType == AssetCatagoryType.STOCK
                           group trade by new { trade.AssetName, trade.Date.Date, trade.BuySell };
        IEnumerable<ITradeTaxCalculation> groupedTradeCalculations = groupedTrade.Select(group => new TradeTaxCalculation(group)).ToList();
        return groupedTradeCalculations.GroupBy(TradeTaxCalculation => TradeTaxCalculation.TradeList.First().AssetName).ToDictionary(group => group.Key, group => group.ToList());
    }

    /// <summary>
    /// Apply same day matching tax rule.
    /// Time of the stock exchange may be different from local UK time, consideration of marginal case where a two "same day" trade span two trading day is needed
    /// </summary>
    /// <param name="taxEventsInChronologicalOrder"></param>
    private static void ApplySameDayMatchingRule(List<IAssetDatedEvent> taxEventsInChronologicalOrder)
    {
        List<CorporateAction> corporateActionsInBetween = new();
        ITradeTaxCalculation? sameDayTrade = null;
        foreach (var taxEvent in taxEventsInChronologicalOrder)
        {
            switch (taxEvent)
            {
                case ITradeTaxCalculation trade:
                    if (sameDayTrade is not null && sameDayTrade.Date.Date == trade.Date.Date)
                    {
                        // guard against unexpected matching
                        if (!(
                          (sameDayTrade.BuySell == TradeType.BUY && trade.BuySell == TradeType.SELL) ||
                          (sameDayTrade.BuySell == TradeType.SELL && trade.BuySell == TradeType.BUY)
                         ))
                        {
                            throw new ArgumentException("It is not one buy and one sell trade");
                        }
                        MatchTrade(sameDayTrade, trade, TaxMatchType.SAME_DAY, corporateActionsInBetween);
                        corporateActionsInBetween.Clear();
                        sameDayTrade = null;  // Reset for the next day's trade.
                    }
                    else
                    {
                        sameDayTrade = trade;
                    }
                    break;
                case CorporateAction corporateAction:
                    corporateActionsInBetween.Add(corporateAction);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected tax event: {taxEvent.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Applies the "Bed and Breakfast" tax rule for trades, matching sell trades to buy trades
    /// that occur within a 30-day window.
    /// If there are multiple disposals during the time window, earlier disposals have to be matched first TCGA92/S106A(5)(b)
    /// </summary>
    private static void ApplyBedAndBreakfastMatchingRule(List<IAssetDatedEvent> taxEventsInChronologicalOrder)
    {
        List<CorporateAction> corporateActionsInBetween = new();
        Queue<ITradeTaxCalculation> sellTradeQueue = new();
        foreach (var taxEvent in taxEventsInChronologicalOrder)
        {
            switch (taxEvent)
            {
                case ITradeTaxCalculation sellTrade when sellTrade.BuySell == TradeType.SELL:
                    sellTradeQueue.Enqueue(sellTrade);
                    break;
                case ITradeTaxCalculation buyTrade when buyTrade.BuySell == TradeType.BUY:
                    while (sellTradeQueue.Count > 0 && !buyTrade.CalculationCompleted)
                    {
                        var sellTradeToMatch = sellTradeQueue.Peek();
                        if ((buyTrade.Date.Date - sellTradeToMatch.Date.Date).Days > 30)
                        {
                            sellTradeQueue.Dequeue();
                            continue;
                        }
                        MatchTrade(sellTradeToMatch, buyTrade, TaxMatchType.BED_AND_BREAKFAST, corporateActionsInBetween);
                        if (sellTradeToMatch.CalculationCompleted)
                        {
                            sellTradeQueue.Dequeue();
                        }
                    }
                    if (sellTradeQueue.Count == 0)
                    {
                        corporateActionsInBetween.Clear();
                    }
                    break;
                case CorporateAction corporateAction:
                    corporateActionsInBetween.Add(corporateAction);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected tax event: {taxEvent.GetType().Name}");
            }
        }
    }


    private static void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType, IEnumerable<CorporateAction>? corporateActionInBetween = null)
    {
        if (!(
            (trade1.BuySell == TradeType.BUY && trade2.BuySell == TradeType.SELL) ||
            (trade1.BuySell == TradeType.SELL && trade2.BuySell == TradeType.BUY)
            ))
        {
            throw new ArgumentException("The provided trades should consist of one buy and one sell trade.");
        }
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        ITradeTaxCalculation buyTrade = trade1.BuySell == TradeType.BUY ? trade1 : trade2;
        ITradeTaxCalculation sellTrade = trade1.BuySell == TradeType.SELL ? trade1 : trade2;
        decimal proposedMatchQuantity = Math.Min(trade1.UnmatchedQty, trade2.UnmatchedQty);
        TradeMatch proposedMatch = TradeMatch.CreateTradeMatch(taxMatchType, proposedMatchQuantity, buyTrade.GetProportionedCostOrProceed(proposedMatchQuantity), sellTrade.GetProportionedCostOrProceed(proposedMatchQuantity),
            matchedBuyTrade: buyTrade, matchedSellTrade: sellTrade);
        // trades and the proposed match are handed to each CorporateAction to modify.
        if (corporateActionInBetween is not null)
        {
            foreach (CorporateAction action in corporateActionInBetween)
            {
                if (action is IChangeTradeMatchingInBetween tradeMatchChanger)
                {
                    tradeMatchChanger.ChangeTradeMatching(trade1, trade2, proposedMatch);
                }
            }
        }
        // normalise match numbers if match quantity exceed number of unmatched shares
        if (proposedMatch.MatchAcquisitionQty > buyTrade.UnmatchedQty)
        {
            decimal adjustRatio = buyTrade.UnmatchedQty / proposedMatch.MatchAcquisitionQty;
            proposedMatch.MatchDisposalQty *= adjustRatio;
            proposedMatch.MatchAcquisitionQty = buyTrade.UnmatchedQty;
        }
        if (proposedMatch.MatchDisposalQty > sellTrade.UnmatchedQty)
        {
            decimal adjustRatio = sellTrade.UnmatchedQty / proposedMatch.MatchDisposalQty;
            proposedMatch.MatchAcquisitionQty *= adjustRatio;
            proposedMatch.MatchDisposalQty = sellTrade.UnmatchedQty;
        }
        proposedMatch.BaseCurrencyMatchAcquisitionValue = buyTrade.GetProportionedCostOrProceed(proposedMatch.MatchAcquisitionQty);
        proposedMatch.BaseCurrencyMatchDisposalValue = sellTrade.GetProportionedCostOrProceed(proposedMatch.MatchDisposalQty);
        buyTrade.MatchQty(proposedMatch.MatchAcquisitionQty);
        sellTrade.MatchQty(proposedMatch.MatchDisposalQty);
        buyTrade.MatchHistory.Add(proposedMatch);
        sellTrade.MatchHistory.Add(proposedMatch);
    }

    /// <summary>
    /// This apply tax rules for section104 and short sale trades which is matched with buy trades in the future.
    /// If there are multiple short sales, earlier short sales must be matched first according to TCGA92/S105(2)
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="taxEventsInChronologicalOrder"></param>
    /// <exception cref="ArgumentException"></exception>
    private void ProcessTradeInChronologicalOrder(string assetName, IEnumerable<IAssetDatedEvent> taxEventsInChronologicalOrder)
    {
        Queue<ITradeTaxCalculation> unmatchedDisposal = new();
        UkSection104 section104 = _setion104Pools.GetExistingOrInitialise(assetName);
        foreach (IAssetDatedEvent taxEvent in taxEventsInChronologicalOrder)
        {
            switch (taxEvent)
            {
                case ITradeTaxCalculation tradeTaxCalculation:
                    if (tradeTaxCalculation.CalculationCompleted) continue;
                    if (unmatchedDisposal.Any() && tradeTaxCalculation.BuySell == TradeType.BUY)
                    {
                        while (unmatchedDisposal.Any() && !tradeTaxCalculation.CalculationCompleted)
                        {
                            var nextTradeToMatch = unmatchedDisposal.Peek();
                            MatchTrade(nextTradeToMatch, tradeTaxCalculation, TaxMatchType.SHORTCOVER);
                            if (nextTradeToMatch.CalculationCompleted) unmatchedDisposal.Dequeue();
                        }
                    }
                    section104.MatchTradeWithSection104(tradeTaxCalculation);
                    if (!tradeTaxCalculation.CalculationCompleted && tradeTaxCalculation.BuySell == TradeType.SELL)
                    {
                        unmatchedDisposal.Enqueue(tradeTaxCalculation);
                    }
                    break;
                case IChangeSection104 action:
                    action.ChangeSection104(section104);
                    break;
                case CorporateAction:
                    // Intentionally ignore as CorporateActions without IChangeSection104 don't need processing
                    break;
                default:
                    throw new ArgumentException($"Failed to process tax event for asset '{assetName}'. Expected events of type 'ITradeTaxCalculation' or 'IChangeSection104', but received an event of type '{taxEvent.GetType().Name}'.");

            }
        }
    }
}
