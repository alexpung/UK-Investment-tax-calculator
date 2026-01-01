using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;

using Microsoft.Extensions.Logging;

namespace UnitTest.Helper;

public static class TradeCalculationHelper
{
    public static List<ITradeTaxCalculation> CalculateTrades(IEnumerable<TaxEvent> taxEvents, out UkSection104Pools section104Pools, ResidencyStatusRecord? residencyStatusRecord = null)
    {
        residencyStatusRecord ??= new ResidencyStatusRecord();
        section104Pools = new UkSection104Pools(new UKTaxYear(), residencyStatusRecord);
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        UkOptionTradeCalculator optionTradeCalculator = CreateOptionTradeCalculator(section104Pools, taxEventLists, residencyStatusRecord);
        UkTradeCalculator calculator = CreateUkTradeCalculator(section104Pools, taxEventLists, residencyStatusRecord);
        UkFutureTradeCalculator futureCalculator = CreateUkFutureTradeCalculator(section104Pools, taxEventLists, residencyStatusRecord);

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

    public static UkOptionTradeCalculator CreateOptionTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, ResidencyStatusRecord? residencyStatusRecord = null)
    {
        ILogger<ToastService> logger = NSubstitute.Substitute.For<ILogger<ToastService>>();
        residencyStatusRecord ??= new();
        TradeTaxCalculationFactory tradeTaxCalculationFactory = new(residencyStatusRecord);
        return new UkOptionTradeCalculator(section104Pools, tradeList, new UKTaxYear(), new ToastService(logger), tradeTaxCalculationFactory);
    }

    public static UkTradeCalculator CreateUkTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, ResidencyStatusRecord? residencyStatusRecord = null)
    {
        residencyStatusRecord ??= new();
        TradeTaxCalculationFactory tradeTaxCalculationFactory = new(residencyStatusRecord);
        return new UkTradeCalculator(section104Pools, tradeList, tradeTaxCalculationFactory);
    }

    public static UkFutureTradeCalculator CreateUkFutureTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, ResidencyStatusRecord? residencyStatusRecord = null)
    {
        residencyStatusRecord ??= new();
        TradeTaxCalculationFactory tradeTaxCalculationFactory = new(residencyStatusRecord);
        return new UkFutureTradeCalculator(section104Pools, tradeList, tradeTaxCalculationFactory);
    }
}
