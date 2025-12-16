using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Collections.Immutable;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public static class UkMatchingRules
{
    /// <summary>
    /// Apply same day matching tax rule.
    /// Time of the stock exchange may be different from local UK time, consideration of marginal case where a two "same day" trade span two trading day is needed
    /// Return: Tuple pair of the matching trade pair. The ordering should be respected, with earlier pairs matched first.
    /// </summary>
    public static IEnumerable<Tuple<T, T>> ApplySameDayMatchingRule<T>(GroupedTradeContainer<T> tradesToBeMatched) where T : ITradeTaxCalculation
    {
        foreach (var assetNameGroup in tradesToBeMatched.GetAllTradesGroupedAndSorted())
        {
            var groupedTradesByDate = assetNameGroup.GroupBy(trade => trade.Date.Date);
            foreach (var dayTrades in groupedTradesByDate)
            {
                if (dayTrades.Count() == 1) continue;
                var acquisitions = dayTrades.Where(t => t.AcquisitionDisposal == TradeType.ACQUISITION).ToList();
                var disposals = dayTrades.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();
                if (acquisitions.Count != 1 && disposals.Count != 1)
                {
                    throw new ArgumentException($"Invalid input for same day matching rule. Expected exactly 1 acquisitions and disposals for asset" +
                        $" on {dayTrades.Key}, but found {acquisitions.Count} acquisitions and {disposals.Count} disposals.");
                }
                yield return Tuple.Create(acquisitions[0], disposals[0]);
            }
        }
    }

    /// <summary>
    /// Applies the "Bed and Breakfast" tax rule for trades, matching sell trades to buy trades
    /// that occur within a 30-day window.
    /// If there are multiple disposals during the time window, earlier disposals have to be matched first TCGA92/S106A(5)(b)
    /// Return: Tuple pair of the matching trade pair. The ordering should be respected, with earlier pairs matched first.
    /// </summary>
    public static IEnumerable<Tuple<T, T>> ApplyBedAndBreakfastMatchingRule<T>(GroupedTradeContainer<T> tradesToBeMatched) where T : ITradeTaxCalculation
    {
        foreach (var sortedTradeList in tradesToBeMatched.GetAllTradesGroupedAndSorted())
        {
            for (int i = 0; i < sortedTradeList.Count; i++)
            {
                if (sortedTradeList[i] is { AcquisitionDisposal: TradeType.DISPOSAL })
                {
                    for (int j = i + 1; j < sortedTradeList.Count; j++) // Look forward 30 days to check for acquisitions after a disposal
                    {
                        if ((sortedTradeList[j].Date.Date - sortedTradeList[i].Date.Date).Days > 30) break;
                        if (sortedTradeList[j] is { AcquisitionDisposal: TradeType.ACQUISITION })
                        {
                            yield return Tuple.Create(sortedTradeList[i], sortedTradeList[j]);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Apply section 104 rules and short sale rules
    /// </summary>
    public static IEnumerable<Tuple<T, T>> ProcessTradeInChronologicalOrder<T>(UkSection104Pools section104Pools, GroupedTradeContainer<T> tradesToBeMatched) where T : ITradeTaxCalculation
    {
        foreach (ImmutableList<IAssetDatedEvent> group in tradesToBeMatched.GetAllTaxEventsGroupedAndSorted())
        {
            string assetName = group[0].AssetName;
            UkSection104 section104 = section104Pools.GetExistingOrInitialise(assetName);
            Dictionary<DateTime, T> unmatchedDisposals = [];

            foreach (IAssetDatedEvent taxEvent in group)
            {
                switch (taxEvent)
                {
                    case T tradeTaxCalculation:
                        foreach (var match in ProcessTradeTaxCalculation(tradeTaxCalculation, section104, unmatchedDisposals))
                        {
                            // This produce short sale matching which is iterated by the caller
                            yield return match;
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

    /// <summary>
    /// Process section 104 matching and shorting sale match rules
    /// </summary>
    private static IEnumerable<Tuple<T, T>> ProcessTradeTaxCalculation<T>(T tradeTaxCalculation, UkSection104 section104, Dictionary<DateTime, T> unmatchedDisposals) where T : ITradeTaxCalculation
    {
        if (tradeTaxCalculation.CalculationCompleted) yield break;

        if (tradeTaxCalculation.AcquisitionDisposal == TradeType.ACQUISITION)
        {
            foreach (var disposal in unmatchedDisposals.Values)
            {
                yield return Tuple.Create(disposal, tradeTaxCalculation);
                if (disposal.CalculationCompleted) unmatchedDisposals.Remove(disposal.Date);
                if (tradeTaxCalculation.CalculationCompleted) break;
            }
        }

        tradeTaxCalculation.MatchWithSection104(section104);

        if (!tradeTaxCalculation.CalculationCompleted && tradeTaxCalculation.AcquisitionDisposal == TradeType.DISPOSAL)
        {
            unmatchedDisposals[tradeTaxCalculation.Date] = tradeTaxCalculation;
        }
    }

    /// <summary>
    /// reclassify shorting trades to an acquisition of short position for asset class that are considered so by HMRC 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public static List<T> TagTradesWithOpenClose<T>(IEnumerable<T> trades) where T : Trade, ISplittableToLongAndShort<T>
    {
        decimal currentPosition = 0m; // tracking current position to determine a trade is opening or closing
        List<T> resultList = [];
        foreach (var trade in trades.OrderBy(trade => trade.Date))
        {
            // run through trades chronologically and catagorise trades to one of the 4 catagories.
            string shortPrefix = "Short ";
            switch (currentPosition, trade.RawQuantity)
            {
                case ( >= 0, >= 0): // increase long position
                    trade.PositionType = PositionType.OPENLONG;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position >= 0 && Math.Abs(rawQuantity) <= position: // closing only part or all of a long position.
                    trade.PositionType = PositionType.CLOSELONG;
                    resultList.Add(trade);
                    break;
                case ( <= 0, <= 0): // increase short position
                    trade.PositionType = PositionType.OPENSHORT;
                    if (!trade.AssetName.StartsWith(shortPrefix))
                        trade.AssetName = shortPrefix + trade.AssetName;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position <= 0 && rawQuantity <= Math.Abs(position): // closing only part or all of a short position.
                    trade.PositionType = PositionType.CLOSESHORT;
                    if (!trade.AssetName.StartsWith(shortPrefix))
                        trade.AssetName = shortPrefix + trade.AssetName;
                    resultList.Add(trade);
                    break;
                case var (position, rawQuantity) when position > 0 && rawQuantity < 0 && Math.Abs(rawQuantity) > position:
                    // closing a long position and reopen as a short
                    T closingTrade = trade.SplitTrade(position);
                    closingTrade.PositionType = PositionType.CLOSELONG;
                    T reopeningTrade = trade.SplitTrade(Math.Abs(rawQuantity) - Math.Abs(currentPosition));
                    reopeningTrade.PositionType = PositionType.OPENSHORT;
                    if (!reopeningTrade.AssetName.StartsWith(shortPrefix))
                        reopeningTrade.AssetName = shortPrefix + trade.AssetName;
                    resultList.Add(reopeningTrade);
                    resultList.Add(closingTrade);
                    break;
                case var (position, rawQuantity) when position <= 0 && rawQuantity > 0 && rawQuantity > Math.Abs(currentPosition):
                    // closing a short position and reopen as a long
                    closingTrade = trade.SplitTrade(Math.Abs(currentPosition));
                    closingTrade.PositionType = PositionType.CLOSESHORT;
                    if (!closingTrade.AssetName.StartsWith(shortPrefix))
                        closingTrade.AssetName = shortPrefix + trade.AssetName;
                    reopeningTrade = trade.SplitTrade(Math.Abs(rawQuantity) - Math.Abs(currentPosition));
                    reopeningTrade.PositionType = PositionType.OPENLONG;
                    resultList.Add(reopeningTrade);
                    resultList.Add(closingTrade);
                    break;
            }
            currentPosition += trade.AcquisitionDisposal switch
            {
                TradeType.ACQUISITION => trade.Quantity,
                TradeType.DISPOSAL => trade.Quantity * -1,
                _ => throw new NotImplementedException($"Unexpected tradeType {trade.AcquisitionDisposal}")
            };
        }
        return resultList;
    }
}
