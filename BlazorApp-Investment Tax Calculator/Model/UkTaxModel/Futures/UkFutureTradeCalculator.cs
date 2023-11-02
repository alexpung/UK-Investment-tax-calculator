using Enum;

using Model.Interfaces;
using Model.TaxEvents;

using Syncfusion.Blazor.Data;

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
        _setion104Pools.Clear();
        //Dictionary<string, List<ITradeTaxCalculation>> tradeTaxCalculations = GroupTrade(_tradeList.Trades);
        // This is a Dict grouped by asset name. For each asset name process all trades.
        //foreach (KeyValuePair<string, List<ITradeTaxCalculation>> assetGroup in tradeTaxCalculations)
        //{
        //List<ITradeTaxCalculation> taxEventsInChronologicalOrder = assetGroup.Value.OrderBy(item => item.Date).ToList();
        //ApplySameDayMatchingRule(taxEventsInChronologicalOrder);
        //ApplyBedAndBreakfastMatchingRule(taxEventsInChronologicalOrder);
        //ProcessTradeInChronologicalOrder(assetGroup.Key, taxEventsInChronologicalOrder);
        // }
        //return tradeTaxCalculations.Values.SelectMany(i => i).ToList();
        return new List<ITradeTaxCalculation>();
    }

    private static Dictionary<string, List<ITradeTaxCalculation>> GroupTrade(IEnumerable<Trade> trades)
    {
        var groupedTrade = from trade in trades
                           where trade.AssetType == AssetCatagoryType.FUTURE
                           group trade by trade.AssetName;
        return new();
    }

    /// <summary>
    /// Apply same day matching tax rule.
    /// Time of the stock exchange may be different from local UK time, consideration of marginal case where a two "same day" trade span two trading day is needed
    /// </summary>
    /// <param name="taxEventsInChronologicalOrder"></param>
    private static void ApplySameDayMatchingRule(List<ITradeTaxCalculation> taxEventsInChronologicalOrder)
    {
    }

    /// <summary>
    /// Applies the "Bed and Breakfast" tax rule for trades, matching sell trades to buy trades
    /// that occur within a 30-day window.
    /// If there are multiple disposals during the time window, earlier disposals have to be matched first TCGA92/S106A(5)(b)
    /// </summary>
    private static void ApplyBedAndBreakfastMatchingRule(List<IAssetDatedEvent> taxEventsInChronologicalOrder)
    {
    }


    private static void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType, IEnumerable<CorporateAction>? corporateActionInBetween = null)
    {
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
    }
}

