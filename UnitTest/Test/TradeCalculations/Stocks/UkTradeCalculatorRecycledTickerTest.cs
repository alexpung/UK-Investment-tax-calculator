using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

/// <summary>
/// A ticker freed by a delisting and later reassigned by the exchange to an unrelated company must not share a
/// Section 104 pool with the original holder of that ticker. Both shares report under the same primary ticker, so
/// the grouping key has to come from the share identity rather than from the ticker string.
/// </summary>
public class UkTradeCalculatorRecycledTickerTest
{
    private const string OldCompanyIsin = "GB00RCYOLD01";
    private const string NewCompanyIsin = "GB00RCYNEW02";

    [Fact]
    public void TestRecycledTickerDoesNotShareSection104PoolWithOriginalCompany()
    {
        // Original company: buy 400 @ GBP 10, sell 200 @ GBP 12, leaving 200 shares costing GBP 2000 in its pool.
        Trade oldCompanyBuy = CreateTrade("RCY", OldCompanyIsin, TradeType.ACQUISITION, "12-Jan-21 09:00:00", 400, 4000);
        Trade oldCompanySell = CreateTrade("RCY", OldCompanyIsin, TradeType.DISPOSAL, "30-Jun-21 09:00:00", 200, 2400);
        // Unrelated company assigned the same ticker years later: buy 300 @ GBP 50, sell 100 @ GBP 65.
        Trade newCompanyBuy = CreateTrade("RCY", NewCompanyIsin, TradeType.ACQUISITION, "05-Mar-23 09:00:00", 300, 15000);
        Trade newCompanySell = CreateTrade("RCY", NewCompanyIsin, TradeType.DISPOSAL, "20-Sep-23 09:00:00", 100, 6500);

        List<Trade> trades = [oldCompanyBuy, oldCompanySell, newCompanyBuy, newCompanySell];
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(trades);

        List<ITradeTaxCalculation> results = TradeCalculationHelper.CalculateTrades(trades, out UkSection104Pools section104Pools);

        ITradeTaxCalculation oldCompanyDisposal = GetDisposalOn(results, "30-Jun-21");
        oldCompanyDisposal.TotalAllowableCost.ShouldBe(new WrappedMoney(2000m)); // 4000 * 200/400
        oldCompanyDisposal.Gain.ShouldBe(new WrappedMoney(400m));

        // Only the unrelated company's own 300 shares at GBP 15000 may be pooled here: 15000 * 100/300 = 5000.
        // Pooling the 200 shares left over from the delisted company would give cost 3400 and gain 3100 instead.
        ITradeTaxCalculation newCompanyDisposal = GetDisposalOn(results, "20-Sep-23");
        newCompanyDisposal.TotalAllowableCost.ShouldBe(new WrappedMoney(5000m));
        newCompanyDisposal.Gain.ShouldBe(new WrappedMoney(1500m));

        UkSection104 oldCompanyPool = section104Pools.GetExistingOrInitialise($"RCY ({OldCompanyIsin})");
        oldCompanyPool.Quantity.ShouldBe(200);
        oldCompanyPool.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(2000m));

        UkSection104 newCompanyPool = section104Pools.GetExistingOrInitialise($"RCY ({NewCompanyIsin})");
        newCompanyPool.Quantity.ShouldBe(200);
        newCompanyPool.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(10000m));

        section104Pools.GetSection104s().Count.ShouldBe(2);
    }

    [Fact]
    public void TestRenamedTickerKeepingIsinStillSharesOneSection104Pool()
    {
        // The counterpart: a rename that keeps the ISIN must still collapse to a single pool, so the fix for the
        // recycled ticker does not start splitting shares that are genuinely the same.
        Trade buyUnderOldTicker = CreateTrade("FB", "US30303M1027", TradeType.ACQUISITION, "10-Feb-21 15:30:00", 100, 16000);
        Trade buyUnderNewTicker = CreateTrade("META", "US30303M1027", TradeType.ACQUISITION, "15-Mar-22 15:30:00", 100, 24000);
        Trade sellUnderNewTicker = CreateTrade("META", "US30303M1027", TradeType.DISPOSAL, "20-Oct-22 15:30:00", 150, 36000);

        List<Trade> trades = [buyUnderOldTicker, buyUnderNewTicker, sellUnderNewTicker];
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(trades);

        List<ITradeTaxCalculation> results = TradeCalculationHelper.CalculateTrades(trades, out UkSection104Pools section104Pools);

        ITradeTaxCalculation disposal = GetDisposalOn(results, "20-Oct-22");
        disposal.TotalAllowableCost.ShouldBe(new WrappedMoney(30000m)); // 40000 * 150/200, i.e. both buys pooled
        disposal.Gain.ShouldBe(new WrappedMoney(6000m));

        section104Pools.GetSection104s().Count.ShouldBe(1);
        UkSection104 pool = section104Pools.GetExistingOrInitialise("META");
        pool.Quantity.ShouldBe(50);
        pool.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(10000m));
    }

    private static ITradeTaxCalculation GetDisposalOn(List<ITradeTaxCalculation> results, string date) =>
        results.Single(result => result.AcquisitionDisposal == TradeType.DISPOSAL &&
                                 result.Date.Date == DateTime.Parse(date, CultureInfo.InvariantCulture).Date);

    private static Trade CreateTrade(string assetName, string isin, TradeType tradeType, string date, decimal quantity, decimal grossProceedInGbp)
    {
        return new Trade
        {
            AssetName = assetName,
            Isin = isin,
            AcquisitionDisposal = tradeType,
            Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
            Quantity = quantity,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(grossProceedInGbp, "GBP"), FxRate = 1 },
        };
    }
}
