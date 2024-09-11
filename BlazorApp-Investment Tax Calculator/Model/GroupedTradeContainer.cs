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
    private readonly Dictionary<string, ImmutableList<IAssetDatedEvent>> _tradeAndCorporateActionListDict = tradeList.Cast<IAssetDatedEvent>()
                                                                                                             .Concat(corporateActionList.Cast<IAssetDatedEvent>())
                                                                                                             .GroupBy(trade => trade.AssetName)
                                                                                                             .ToDictionary(
                                                                                                             group => group.Key,
                                                                                                             group => group.OrderBy(trade => trade.Date.Date).ToImmutableList()
                                                                                                              );

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
    /// </summary>
    public IEnumerable<ImmutableList<IAssetDatedEvent>> GetAllTaxEventsGroupedAndSorted()
    {
        foreach (var key in _tradeListDict.Keys)
        {
            yield return _tradeAndCorporateActionListDict[key];
        }
    }
}
