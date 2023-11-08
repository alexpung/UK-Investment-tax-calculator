using Enum;

using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel.Stocks;

using Syncfusion.Blazor.Data;

using TaxEvents;

namespace Model.UkTaxModel.Futures;

public class UkFutureTradeCalculator : ITradeCalculator
{
    private readonly ITradeAndCorporateActionList _tradeList;
    private readonly UkSection104Pools _setion104Pools;

    public UkFutureTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList)
    {
        _setion104Pools = section104Pools;
        _tradeList = tradeList;
    }

    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<FutureTradeTaxCalculation> tradeTaxCalculations = GroupTrade(_tradeList.Trades);
        foreach (var trades in tradeTaxCalculations.GroupBy(trade => trade.AssetName))
        {
            List<FutureTradeTaxCalculation> taxEventsInChronologicalOrder = trades.OrderBy(item => item.Date).ToList();
            ApplySameDayMatchingRule(taxEventsInChronologicalOrder);
            ApplyBedAndBreakfastMatchingRule(taxEventsInChronologicalOrder);
            ProcessTradeInChronologicalOrder(trades.Key, taxEventsInChronologicalOrder);
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
        List<FutureTradeTaxCalculation> groupedTrade = new();
        foreach (var tradeGroup in GroupFutureContractTradeByAssetName(trades))
        {
            List<FutureContractTrade> taggedTrades = TagTradesWithOpenClose(tradeGroup);
            groupedTrade.AddRange(taggedTrades.GroupBy(trade => (trade.Date, trade.FuturePositionType)).Select(trades => new FutureTradeTaxCalculation(trades)));
        }
        return groupedTrade;
    }

    private static IEnumerable<IGrouping<string, FutureContractTrade>> GroupFutureContractTradeByAssetName(IEnumerable<Trade> trades)
    {
        return from trade in trades
               where trade is FutureContractTrade
               group trade as FutureContractTrade by trade.AssetName;
    }

    private static List<FutureContractTrade> TagTradesWithOpenClose(IEnumerable<FutureContractTrade> trades)
    {
        decimal currentPosition = 0m; // tracking current position to determine a trade is opening or closing
        List<FutureContractTrade> resultList = new();
        foreach (var trade in trades)
        {
            // run through trades chronologically and catagorise trades to one of the 4 catagories.
            switch (currentPosition, trade.RawQuantity)
            {
                case ( >= 0, >= 0): // increase long position
                    trade.FuturePositionType = FuturePositionType.OPENLONG;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position >= 0 && Math.Abs(rawQuantity) <= position: // closing only part or all of a long position.
                    trade.FuturePositionType = FuturePositionType.CLOSELONG;
                    resultList.Add(trade);
                    break;
                case ( <= 0, <= 0): // increase short position
                    trade.FuturePositionType = FuturePositionType.OPENSHORT;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position <= 0 && rawQuantity <= Math.Abs(position): // closing only part or all of a short position.
                    trade.FuturePositionType = FuturePositionType.CLOSESHORT;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position > 0 && rawQuantity < 0 && Math.Abs(rawQuantity) > position:
                    // closing a long position and reopen as a short
                    FutureContractTrade closingTrade = trade.SplitTrade(position);
                    closingTrade.FuturePositionType = FuturePositionType.CLOSELONG;
                    FutureContractTrade reopeningTrade = trade.SplitTrade(Math.Abs(rawQuantity) - Math.Abs(currentPosition));
                    reopeningTrade.FuturePositionType = FuturePositionType.OPENSHORT;
                    resultList.Add(reopeningTrade);
                    resultList.Add(closingTrade);
                    break;
                case var (position, rawQuantity) when position <= 0 && rawQuantity > 0 && rawQuantity > Math.Abs(currentPosition):
                    // closing a short position and reopen as a long
                    closingTrade = trade.SplitTrade(Math.Abs(currentPosition));
                    closingTrade.FuturePositionType = FuturePositionType.CLOSESHORT;
                    reopeningTrade = trade.SplitTrade(Math.Abs(rawQuantity) - Math.Abs(currentPosition));
                    reopeningTrade.FuturePositionType = FuturePositionType.OPENLONG;
                    resultList.Add(reopeningTrade);
                    resultList.Add(closingTrade);
                    break;
            }
            currentPosition += trade.BuySell switch
            {
                TradeType.BUY => trade.Quantity,
                TradeType.SELL => trade.Quantity * -1,
                _ => throw new NotImplementedException($"Unexpected tradeType {trade.BuySell}")
            };
        }
        return resultList;
    }

    /// <summary>
    /// Apply same-day matching tax rule.
    /// </summary>
    /// <param name="tradesInChronologicalOrder">List of future trade tax calculations in chronological order.</param>
    private static void ApplySameDayMatchingRule(List<FutureTradeTaxCalculation> tradesInChronologicalOrder)
    {
        var tradesByDateAndType = tradesInChronologicalOrder
            .GroupBy(trade => new { trade.Date.Date, trade.PositionType })
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var trade in tradesInChronologicalOrder)
        {
            var oppositePositionType = GetOppositePositionType(trade.PositionType);
            var key = new { trade.Date.Date, PositionType = oppositePositionType };

            if (tradesByDateAndType.TryGetValue(key, out var potentialMatches))
            {
                var tradeToMatch = potentialMatches.FirstOrDefault();
                if (tradeToMatch != null)
                {
                    MatchTrade(trade, tradeToMatch, TaxMatchType.SAME_DAY);
                }
            }
        }
    }

    private static FuturePositionType GetOppositePositionType(FuturePositionType positionType)
    {
        return positionType switch
        {
            FuturePositionType.OPENLONG => FuturePositionType.CLOSELONG,
            FuturePositionType.OPENSHORT => FuturePositionType.CLOSESHORT,
            FuturePositionType.CLOSELONG => FuturePositionType.OPENLONG,
            FuturePositionType.CLOSESHORT => FuturePositionType.OPENSHORT,
            _ => throw new ArgumentOutOfRangeException(nameof(positionType), "Unknown future position type"),
        };
    }


    /// <summary>
    /// Applies the "Bed and Breakfast" tax rule for trades, matching sell trades to buy trades
    /// that occur within a 30-day window.
    /// If there are multiple disposals during the time window, earlier disposals have to be matched first TCGA92/S106A(5)(b)
    /// </summary>
    private static void ApplyBedAndBreakfastMatchingRule(List<FutureTradeTaxCalculation> tradesInChronologicalOrder)
    {
        foreach (var trade in tradesInChronologicalOrder.Where(trade => trade is { PositionType: FuturePositionType.CLOSESHORT or FuturePositionType.CLOSELONG }))
        {
            var oppositePositionType = GetOppositePositionType(trade.PositionType);
            var matchCandidates = tradesInChronologicalOrder.Where(nextTrade => nextTrade.PositionType == oppositePositionType
                                                                                    && trade.Date.Date.AddDays(30) > nextTrade.Date.Date);
            if (matchCandidates.Any())
            {
                matchCandidates = matchCandidates.OrderBy(trade => trade.Date);
                foreach (var matchCandidate in matchCandidates)
                {
                    MatchTrade(trade, matchCandidate, TaxMatchType.BED_AND_BREAKFAST);
                }
            }
        }
    }


    private static void MatchTrade(FutureTradeTaxCalculation openTrade, FutureTradeTaxCalculation closeTrade, TaxMatchType taxMatchType)
    {
        decimal matchQty = Math.Min(openTrade.UnmatchedQty, closeTrade.UnmatchedQty);
        WrappedMoney contractGain = closeTrade.GetProportionedContractValue(matchQty) - openTrade.GetProportionedContractValue(matchQty);
        WrappedMoney allowableCost = openTrade.GetProportionedCostOrProceed(matchQty) + closeTrade.GetProportionedCostOrProceed(matchQty);
        WrappedMoney disposalProceed = WrappedMoney.GetBaseCurrencyZero();
        if (contractGain.Amount < 0)
        {
            allowableCost -= contractGain;
        }
        else
        {
            disposalProceed += contractGain;
        }
        TradeMatch match = TradeMatch.CreateTradeMatch(taxMatchType, matchQty, allowableCost, disposalProceed, closeTrade, openTrade);
        openTrade.MatchHistory.Add(match);
        closeTrade.MatchHistory.Add(match);
    }

    /// <summary>
    /// This apply tax rules for section104 (no short sale possible as shorting a future contract is consider acqusition of a security)
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="tradesInChronologicalOrder"></param>
    /// <exception cref="ArgumentException"></exception>
    private void ProcessTradeInChronologicalOrder(string assetName, IEnumerable<FutureTradeTaxCalculation> tradesInChronologicalOrder)
    {
        UkSection104 longSection104Pool = _setion104Pools.GetExistingOrInitialise(assetName);
        UkSection104 shortSection104Pool = _setion104Pools.GetExistingOrInitialise($"Short contract {assetName}");

        foreach (var trade in tradesInChronologicalOrder)
        {
            if (trade.PositionType is FuturePositionType.OPENLONG or FuturePositionType.CLOSELONG)
            {
                longSection104Pool.MatchTradeWithSection104(trade);
            }
            else if (trade.PositionType is FuturePositionType.OPENSHORT or FuturePositionType.CLOSESHORT)
            {
                shortSection104Pool.MatchTradeWithSection104(trade);
            }
            else throw new ArgumentException($"Unknown future position type {trade.PositionType}");
        }
    }
}

