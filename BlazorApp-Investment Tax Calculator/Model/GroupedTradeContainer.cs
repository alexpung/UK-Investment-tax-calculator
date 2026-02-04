using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Collections.Immutable;

namespace InvestmentTaxCalculator.Model;

public class GroupedTradeContainer<T>(IEnumerable<T> tradeList, IEnumerable<CorporateAction> corporateActionList) where T : ITradeTaxCalculation
{
    private readonly Dictionary<string, ImmutableList<T>> _tradeListDict = tradeList.GroupBy(trade => trade.AssetName)
                                                                                                       .ToDictionary(
                                                                                                       group => group.Key,
                                                                                                       group => group.OrderBy(trade => trade.Date.Date).ToImmutableList()
                                                                                                       );

    private readonly Dictionary<string, ImmutableList<IAssetDatedEvent>> _tradeAndCorporateActionListDict = BuildTaxEventsDictionary(tradeList, corporateActionList);

    // Dependency tree: new company ticker -> set of old company tickers that must be processed first
    private readonly Dictionary<string, HashSet<string>> _takeoverDependencies = corporateActionList.OfType<TakeoverCorporateAction>()
                                                                                                   .GroupBy(ca => ca.AcquiringCompanyTicker)
                                                                                                   .ToDictionary(
                                                                                                       group => group.Key,
                                                                                                       group => group.Select(ca => ca.AssetName).ToHashSet()
                                                                                                   );

    /// <summary>
    /// Builds the dictionary of tax events grouped by asset name.
    /// TakeoverCorporateActions are added to BOTH old company and acquiring company groups.
    /// </summary>
    private static Dictionary<string, ImmutableList<IAssetDatedEvent>> BuildTaxEventsDictionary(
        IEnumerable<T> tradeList,
        IEnumerable<CorporateAction> corporateActionList)
    {
        // Group trades and corporate actions by asset name
        var dict = tradeList.Cast<IAssetDatedEvent>()
                           .Concat(corporateActionList.Cast<IAssetDatedEvent>())
                           .GroupBy(e => e.AssetName)
                           .ToDictionary(
                               group => group.Key,
                               group => group.OrderBy(e => e.Date.Date).ToImmutableList()
                           );

        // Manually add TakeoverCorporateActions to the acquiring company's list
        foreach (var action in corporateActionList.OfType<TakeoverCorporateAction>())
        {
            if (dict.TryGetValue(action.AcquiringCompanyTicker, out var existingList))
            {
                // Add the takeover to the existing list and re-sort
                var updatedList = existingList.Add(action).OrderBy(e => e.Date.Date).ToImmutableList();
                dict[action.AcquiringCompanyTicker] = updatedList;
            }
            else
            {
                // Create a new list with just the takeover
                dict[action.AcquiringCompanyTicker] = [action];
            }
        }

        return dict;
    }

    /// <summary>
    /// return an ImmutableList of trades sorted by date with the given asset name
    /// </summary>
    /// <param name="AssetName">Ticket name of the trade list you want to access</param>
    /// <returns></returns>
    public ImmutableList<T> this[string AssetName]
    {
        get
        {
            if (_tradeListDict.TryGetValue(AssetName, out ImmutableList<T>? value))
            {
                return value;
            }
            else return [];
        }
    }

    /// <summary>
    /// return all ImmutableLists of trades sorted by date
    /// </summary>
    public IEnumerable<ImmutableList<T>> GetAllTradesGroupedAndSorted()
    {
        foreach (var key in _tradeListDict.Keys)
        {
            yield return _tradeListDict[key];
        }
    }

    /// <summary>
    /// return all ImmutableLists of trades plus corporate actions sorted by date
    /// Processes assets in topological order respecting takeover dependencies
    /// Returns a tuple of (AssetName, Events) because the events themselves might belong to a different asset (e.g. Takeover)
    /// </summary>
    public IEnumerable<(string AssetName, ImmutableList<IAssetDatedEvent> Events)> GetAllTaxEventsGroupedAndSorted()
    {
        // Use a sorted list for deterministic iteration
        var allAssets = _tradeAndCorporateActionListDict.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var processed = new HashSet<string>();
        var result = new List<string>();

        void ProcessAsset(string asset, HashSet<string> visiting)
        {
            if (processed.Contains(asset)) return;

            if (visiting.Contains(asset))
                throw new InvalidOperationException($"Circular takeover dependency detected for {asset}");

            visiting.Add(asset);

            // Process dependencies first (old companies before new company)
            if (_takeoverDependencies.TryGetValue(asset, out var deps))
            {
                // Sort dependencies for deterministic order
                foreach (var dep in deps.Where(allAssets.Contains).OrderBy(k => k, StringComparer.Ordinal))
                {
                    ProcessAsset(dep, visiting);
                }
            }

            visiting.Remove(asset);
            processed.Add(asset);
            result.Add(asset);
        }

        // Process all assets in topological order
        foreach (var asset in allAssets)
        {
            ProcessAsset(asset, new HashSet<string>());
        }

        // Yield results in dependency-respecting order
        foreach (var asset in result)
        {
            yield return (asset, _tradeAndCorporateActionListDict[asset]);
        }
    }
}
