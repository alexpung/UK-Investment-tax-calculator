using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class ExcessReportableIncomeTest
{
    [Fact]
    public void TestExcessReportableIncomeAdjustsSection104()
    {
        // 1. Setup a S104 pool with some holdings
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 100, 1000m, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);

        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1000m));

        // 2. Add Excess Reportable Income
        // Accounting period end 2023-12-31, Distribution date 2024-06-30
        DateTime distributionDate = new(2024, 6, 30);
        decimal incomePerShare = 0.5m;
        decimal totalIncome = 100 * incomePerShare; // 50

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = distributionDate,
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(totalIncome, "GBP", 1, "ERI")
        };

        eri.ChangeSection104(ukSection104);

        // 3. Verify acquisition cost increased
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1050m));

        // 4. Verify history entry
        ukSection104.Section104HistoryList.Count.ShouldBe(2);
        ukSection104.Section104HistoryList[1].ValueChange.ShouldBe(new WrappedMoney(50m));
        ukSection104.Section104HistoryList[1].Explanation.ShouldContain("Excess reportable income");
    }

    [Fact]
    public void TestGapPeriodPartialDisposalSplitsUpliftBetweenDisposalAndPool()
    {
        // Hold 1000 units at the reporting period end (31/12/2023), sell 400 in the gap period before the
        // fund distribution date (30/6/2024). Reg. 99(5): the sold units' ERI uplifts the disposal base cost,
        // only the retained units' share is added to the pool on the fund distribution date.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 400, 5000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);
        sellTrade.Gain.ShouldBe(new WrappedMoney(1000m)); // 5000 - 4000 before the ERI uplift

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(500m, "GBP", 1, "ERI") // 0.5 per unit for 1000 units held at period end
        };
        eri.ChangeSection104(ukSection104);

        // Sold units: 400 * 0.5 = 200 added to the disposal allowable cost
        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(4200m));
        sellTrade.Gain.ShouldBe(new WrappedMoney(800m));
        sellTrade.MatchHistory[0].AdditionalInformation.ShouldContain("reg. 99(5)");
        // Retained units: 600 * 0.5 = 300 added to the pool
        ukSection104.Quantity.ShouldBe(600m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(6300m));
    }

    [Fact]
    public void TestGapPeriodFullDisposalLeavesPoolUntouched()
    {
        // Selling the whole holding in the gap period must send the entire uplift to the disposal and must not
        // add cost to the now empty section 104 pool (which would distort future unrelated acquisitions).
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 1000, 14500m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(500m, "GBP", 1, "ERI")
        };
        eri.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(10500m));
        sellTrade.Gain.ShouldBe(new WrappedMoney(4000m));
        ukSection104.Quantity.ShouldBe(0m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }

    [Fact]
    public void TestLegacyEriWithoutPeriodEndDefaultsToSixMonthsBeforeDistribution()
    {
        // Older saved files carry no reporting period end date; the period end is assumed to be 6 months before
        // the fund distribution date (reg. 94(4)) so gap period disposals are still handled.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 400, 5000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(500m, "GBP", 1, "ERI")
        };
        eri.EffectiveReportingPeriodEndDate.ShouldBe(new DateOnly(2023, 12, 31));
        eri.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(4200m));
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(6300m));
    }

    [Fact]
    public void TestGapPeriodDisposalAfterFurtherAcquisitionIsApportionedProRata()
    {
        // 1000 units held at period end, 500 more bought in the gap period, then 600 sold from the fungible pool.
        // The disposal is treated as disposing of period end units pro rata: 600 * 1000/1500 = 400 units.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation buyTrade2 = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 2, 1), 500, 6000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 600, 9000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        buyTrade2.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);
        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(6400m)); // 16000 * 600/1500

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(500m, "GBP", 1, "ERI") // 0.5 per unit for the 1000 units held at period end
        };
        eri.ChangeSection104(ukSection104);

        // Disposal takes 400 period end units * 0.5 = 200; pool keeps 600 period end units * 0.5 = 300
        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(6600m));
        ukSection104.Quantity.ShouldBe(900m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(9900m)); // 16000 - 6400 + 300
    }

    [Fact]
    public void TestDisposalBeforePeriodEndDoesNotGetUplift()
    {
        // Units sold before the reporting period end are not held at the period end and carry no ERI.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 11, 1), 400, 5000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(300m, "GBP", 1, "ERI") // 0.5 per unit for the 600 units held at period end
        };
        eri.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(4000m)); // unchanged
        ukSection104.Quantity.ShouldBe(600m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(6300m)); // 6000 + 300
    }
}
