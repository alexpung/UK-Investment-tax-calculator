using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;

namespace UnitTest.Helper;
public static class TradeCalculationHelper
{
    public static List<ITradeTaxCalculation> CalculateTrades(IEnumerable<TaxEvent> taxEvents, out UkSection104Pools section104Pools)
    {
        section104Pools = new UkSection104Pools();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        UkOptionTradeCalculator optionTradeCalculator = new(section104Pools, taxEventLists, new UKTaxYear(), new ToastService());
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        UkFutureTradeCalculator futureCalculator = new(section104Pools, taxEventLists);

        List<ITradeTaxCalculation> result = optionTradeCalculator.CalculateTax();
        result.AddRange(calculator.CalculateTax());
        result.AddRange(futureCalculator.CalculateTax());
        return result;
    }

    public static TradeMatch CreateTradeMatch(TaxMatchType taxMatchType, decimal qty, WrappedMoney allowableCost, WrappedMoney disposalProceed)
    {
        return new TradeMatch()
        {
            Date = DateOnly.MinValue,
            AssetName = "",
            MatchAcquisitionQty = qty,
            MatchDisposalQty = qty,
            TradeMatchType = taxMatchType,
            BaseCurrencyMatchAllowableCost = allowableCost,
            BaseCurrencyMatchDisposalProceed = disposalProceed
        };
    }
}
