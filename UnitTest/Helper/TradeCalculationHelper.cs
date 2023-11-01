using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel;

namespace UnitTest.Helper;
public static class TradeCalculationHelper
{
    public static List<ITradeTaxCalculation> CalculateTrades(IEnumerable<TaxEvent> taxEvents, out UkSection104Pools section104Pools)
    {
        section104Pools = new UkSection104Pools();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        return result;
    }
}
