using Enumerations;

using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel;
using Model.UkTaxModel.Futures;
using Model.UkTaxModel.Stocks;

namespace UnitTest.Helper;
public static class TradeCalculationHelper
{
    public static List<ITradeTaxCalculation> CalculateTrades(IEnumerable<TaxEvent> taxEvents, out UkSection104Pools section104Pools)
    {
        section104Pools = new UkSection104Pools();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        UkFutureTradeCalculator futureCalculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        result.AddRange(futureCalculator.CalculateTax());
        return result;
    }

    public static TradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney allowableCost, WrappedMoney disposalProceed)
    {
        return new TradeMatch()
        {
            Date = DateOnly.MinValue,
            AssetName = "",
            TradeMatchType = taxMatchType,
            BaseCurrencyMatchAllowableCost = allowableCost,
            BaseCurrencyMatchDisposalProceed = disposalProceed
        };
    }
}
