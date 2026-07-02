using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

/// <summary>
/// Excess reportable income liability is fixed by the holding at the end of the fund reporting period
/// (SI 2009/3001 reg. 94(3)). Units disposed of in the "gap period" between the period end and the fund
/// distribution date take their share of the base cost uplift at the disposal (reg. 99(5)), while the
/// retained units' share is added to the section 104 pool on the fund distribution date (reg. 99(4)).
/// </summary>
public class UkTradeCalculatorEriGapPeriodTest
{
    private static ExcessReportableIncome CreateEri(decimal totalAmount) => new()
    {
        AssetName = "REPORT_FUND",
        Date = DateTime.Parse("30-Jun-24 00:00:00", CultureInfo.InvariantCulture),
        ReportingPeriodEndDate = DateTime.Parse("31-Dec-23 00:00:00", CultureInfo.InvariantCulture),
        IncomeType = ExcessReportableIncomeType.DIVIDEND,
        Amount = new DescribedMoney(totalAmount, "GBP", 1, "ERI")
    };

    private static Trade CreateTrade(TradeType tradeType, string date, decimal quantity, decimal amount) => new()
    {
        AssetName = "REPORT_FUND",
        AcquisitionDisposal = tradeType,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(amount) },
    };

    [Fact]
    public void TestGapPeriodSection104DisposalGetsItsShareOfTheUplift()
    {
        Trade buy = CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m);
        Trade gapPeriodSell = CreateTrade(TradeType.DISPOSAL, "30-Apr-24 10:00:00", 400, 5000m);
        ExcessReportableIncome eri = CreateEri(500m); // 0.5 per unit for the 1000 units held at period end

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([buy, gapPeriodSell, eri]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        ITradeTaxCalculation disposal = result.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL);
        disposal.MatchHistory.Count.ShouldBe(1);
        disposal.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        disposal.TotalAllowableCost.Amount.ShouldBe(4200m, 0.01m); // 4000 + 400 * 0.5 (reg. 99(5))
        disposal.Gain.Amount.ShouldBe(800m, 0.01m);

        UkSection104 pool = section104Pools.GetExistingOrInitialise("REPORT_FUND");
        pool.Quantity.ShouldBe(600m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(6300m, 0.01m); // 6000 + 600 * 0.5 (reg. 99(4))
    }

    [Fact]
    public void TestBedAndBreakfastGapPeriodDisposalGetsNoUplift()
    {
        // The gap period disposal is matched with a repurchase within 30 days (s.106A TCGA 1992), so the units
        // disposed of are the repurchased ones, not the period end holding. The period end units never leave the
        // section 104 pool and the full ERI uplift belongs to the pool.
        Trade buy = CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m);
        Trade gapPeriodSell = CreateTrade(TradeType.DISPOSAL, "30-Apr-24 10:00:00", 400, 5000m);
        Trade rebuy = CreateTrade(TradeType.ACQUISITION, "10-May-24 10:00:00", 400, 4400m);
        ExcessReportableIncome eri = CreateEri(500m);

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([buy, gapPeriodSell, rebuy, eri]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        ITradeTaxCalculation disposal = result.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL);
        disposal.MatchHistory.Count.ShouldBe(1);
        disposal.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        disposal.TotalAllowableCost.Amount.ShouldBe(4400m, 0.01m); // no ERI uplift on the disposal
        disposal.Gain.Amount.ShouldBe(600m, 0.01m);

        UkSection104 pool = section104Pools.GetExistingOrInitialise("REPORT_FUND");
        pool.Quantity.ShouldBe(1000m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(10500m, 0.01m); // full uplift stays with the pool
    }

    [Fact]
    public void TestFullDisposalInGapPeriodDoesNotPoisonLaterAcquisition()
    {
        // The whole holding is sold in the gap period and a fresh position is opened after the fund distribution
        // date. The entire uplift belongs to the gap period disposal and must not leak into the new pool.
        Trade buy = CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m);
        Trade gapPeriodSell = CreateTrade(TradeType.DISPOSAL, "30-Apr-24 10:00:00", 1000, 14500m);
        Trade laterBuy = CreateTrade(TradeType.ACQUISITION, "01-Aug-24 10:00:00", 100, 1500m);
        ExcessReportableIncome eri = CreateEri(500m);

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([buy, gapPeriodSell, laterBuy, eri]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        ITradeTaxCalculation disposal = result.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL);
        disposal.TotalAllowableCost.Amount.ShouldBe(10500m, 0.01m); // 10000 + full 500 uplift (reg. 99(5))
        disposal.Gain.Amount.ShouldBe(4000m, 0.01m);

        UkSection104 pool = section104Pools.GetExistingOrInitialise("REPORT_FUND");
        pool.Quantity.ShouldBe(100m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(1500m, 0.01m); // unaffected by the ERI
    }
}
