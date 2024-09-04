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
                ArgumentException matchError = new("Incorrect grouping of trade. Exactly one acqusition and one disposal for ITaxMatchable is expected" +
                    "Each ITaxMatchable should contain all trades on the same side on the same day.");
                if (dayTrades.Count() != 2) throw matchError;
                if (dayTrades.ElementAt(0).AcquisitionDisposal == dayTrades.ElementAt(1).AcquisitionDisposal) throw matchError;
                yield return Tuple.Create(dayTrades.ElementAt(0), dayTrades.ElementAt(1));
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
                    for (int j = i + 1; j < sortedTradeList.Count; j++) // Look forward 30 days to check for acquistions after a disposal
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

    public static IEnumerable<Tuple<T, T>> ProcessTradeInChronologicalOrder<T>(UkSection104Pools section104Pools, GroupedTradeContainer<T> tradesToBeMatched) where T : ITradeTaxCalculation
    {
        foreach (ImmutableList<IAssetDatedEvent> group in tradesToBeMatched.GetAllTaxEventsGroupedAndSorted())
        {
            string assetName = group[0].AssetName;
            UkSection104 section104 = section104Pools.GetExistingOrInitialise(assetName);
            Queue<T> unmatchedDisposal = new();
            foreach (IAssetDatedEvent taxEvent in group)
            {
                switch (taxEvent)
                {
                    case T tradeTaxCalculation:
                        if (tradeTaxCalculation.CalculationCompleted) continue;
                        if (unmatchedDisposal.Count != 0 && tradeTaxCalculation.AcquisitionDisposal == TradeType.ACQUISITION)
                        {
                            while (unmatchedDisposal.Count != 0 && !tradeTaxCalculation.CalculationCompleted)
                            {
                                var nextTradeToMatch = unmatchedDisposal.Peek();
                                yield return Tuple.Create(nextTradeToMatch, tradeTaxCalculation);
                                if (nextTradeToMatch.CalculationCompleted) unmatchedDisposal.Dequeue();
                            }
                        }
                        tradeTaxCalculation.MatchWithSection104(section104);
                        if (!tradeTaxCalculation.CalculationCompleted && tradeTaxCalculation.AcquisitionDisposal == TradeType.DISPOSAL)
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
}
