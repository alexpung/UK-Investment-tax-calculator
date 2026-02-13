using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Collections.Immutable;

namespace InvestmentTaxCalculator.Model;

public class GroupedTradeContainer<T>(IEnumerable<T> tradeList, IEnumerable<CorporateAction> corporateActionList) where T : ITradeTaxCalculation
{
    private readonly Dictionary<string, ImmutableList<T>> _tradeListDict = tradeList
        .GroupBy(trade => trade.AssetName)
        .ToDictionary(
            group => group.Key,
            group => group.OrderBy(trade => trade.Date)
                          .ThenBy(trade => trade.Id)
                          .ToImmutableList()
        );

    private readonly Dictionary<string, ImmutableList<IAssetDatedEvent>> _tradeAndCorporateActionListDict = BuildTaxEventsDictionary(tradeList, corporateActionList);

    // Dependency tree: ticker -> set of tickers that must be processed first
    private readonly Dictionary<string, HashSet<string>> _takeoverDependencies = BuildDependencyTree(corporateActionList);

    /// <summary>
    /// Builds the dictionary of tax events grouped by asset name.
    /// TakeoverCorporateActions are added to BOTH old company and acquiring company groups.
    /// Corporate actions are processed at the start of their EffectiveDate (midnight).
    /// </summary>
    private static Dictionary<string, ImmutableList<IAssetDatedEvent>> BuildTaxEventsDictionary(
        IEnumerable<T> tradeList,
        IEnumerable<CorporateAction> corporateActionList)
    {
        var mutableDict = tradeList.Cast<IAssetDatedEvent>()
            .GroupBy(e => e.AssetName)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        // Add corporate actions to each relevant ticker list
        foreach (var action in corporateActionList)
        {
            foreach (var ticker in action.CompanyTickersInProcessingOrder.Distinct(StringComparer.Ordinal))
            {
                if (mutableDict.TryGetValue(ticker, out var existingList))
                {
                    existingList.Add(action);
                }
                else
                {
                    mutableDict[ticker] = [action];
                }
            }
        }

        return mutableDict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .OrderBy(GetProcessingDateTime)
                .ThenBy(GetStableEventId)
                .ToImmutableList()
        );
    }

    private static DateTime GetProcessingDateTime(IAssetDatedEvent taxEvent) => taxEvent switch
    {
        CorporateAction corporateAction =>
            DateTime.SpecifyKind(corporateAction.EffectiveDate.ToDateTime(TimeOnly.MinValue), corporateAction.Date.Kind),
        _ => taxEvent.Date
    };

    private static int GetStableEventId(IAssetDatedEvent taxEvent) => taxEvent switch
    {
        ITradeTaxCalculation tradeCalculation => tradeCalculation.Id,
        TaxEvent taxEventRecord => taxEventRecord.Id,
        _ => 0
    };

    /// <summary>
    /// Builds dependency tree for corporate actions.
    /// Later tickers depend on earlier tickers in the processing order list.
    /// </summary>
    private static Dictionary<string, HashSet<string>> BuildDependencyTree(IEnumerable<CorporateAction> corporateActionList)
    {

        var deps = new Dictionary<string, HashSet<string>>();

        foreach (var action in corporateActionList)
        {
            var tickers = action.CompanyTickersInProcessingOrder;
            for (int i = 0; i < tickers.Count - 1; i++)
            {
                string dependency = tickers[i];
                for (int j = i + 1; j < tickers.Count; j++)
                {
                    string dependent = tickers[j];
                    if (!deps.TryGetValue(dependent, out var set))
                    {
                        set = new HashSet<string>();
                        deps[dependent] = set;
                    }
                    set.Add(dependency);
                }
            }
        }

        return deps;
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
        foreach (var key in _tradeListDict.Keys.OrderBy(key => key, StringComparer.Ordinal))
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
