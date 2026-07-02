using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace InvestmentTaxCalculator.UnitTest.Test.Model.TaxEvents;

using Shouldly;

using Xunit;

public class FundEqualisationTest
{
    [Fact]
    public void Reason_WithRelatedEvent_FormatsCorrectly()
    {

        var equalisation = new FundEqualisation
        {
            AssetName = "Test Fund",
            Date = new DateTime(2023, 1, 1),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation"),
            RelatedEventDescription = "Related Dividend"
        };

        var reason = equalisation.Reason;

        // WrappedMoney uses NMoneys which formats GBP with £ symbol and 2 decimals by default.
        var dateString = new DateTime(2023, 1, 1).ToString("d");
        reason.ShouldContain($"Test Fund fund equalisation of £100.00 on {dateString} (Related Dividend)");
        reason.ShouldNotContain("..");
    }

    [Fact]
    public void Reason_WithoutRelatedEvent_FormatsCorrectly()
    {

        var equalisation = new FundEqualisation
        {
            AssetName = "Test Fund",
            Date = new DateTime(2023, 1, 1),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation"),
            RelatedEventDescription = null
        };

        var reason = equalisation.Reason;

        var dateString = new DateTime(2023, 1, 1).ToString("d");
        reason.ShouldContain($"Test Fund fund equalisation of £100.00 on {dateString}");
        reason.ShouldNotContain("()");
        reason.ShouldNotContain(" .");
    }

    [Fact]
    public void ChangeSection104_WithoutReportingPeriodEnd_ReducesPoolOnly()
    {
        // Legacy behaviour: no reporting period end recorded, the whole amount reduces the pool at the event date.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 400, 5000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        var equalisation = new FundEqualisation
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation")
        };
        equalisation.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(4000m)); // unchanged
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(5900m)); // 6000 - 100
    }

    [Fact]
    public void ChangeSection104_GapPeriodDisposal_TakesShareOfReduction()
    {
        // 1000 units held at the reporting period end, 400 sold before the distribution date: the sold units'
        // share of the equalisation reduces the disposal allowable cost, the rest reduces the pool.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 400, 5000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        var equalisation = new FundEqualisation
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation") // 0.1 per unit for 1000 units
        };
        equalisation.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(3960m)); // 4000 - 400 * 0.1
        sellTrade.Gain.ShouldBe(new WrappedMoney(1040m));
        ukSection104.Quantity.ShouldBe(600m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(5940m)); // 6000 - 600 * 0.1
    }

    [Fact]
    public void ChangeSection104_GapPeriodFullDisposal_DoesNotDriveEmptyPoolNegative()
    {
        // Selling the whole holding before the distribution date: the reduction lands on the disposal and the
        // empty pool must not be driven to a negative cost.
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 1000, 10000m, TradeType.ACQUISITION);
        TradeTaxCalculation sellTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2024, 4, 30), 1000, 14000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);
        sellTrade.MatchWithSection104(ukSection104);

        var equalisation = new FundEqualisation
        {
            AssetName = "ETF1",
            Date = new DateTime(2024, 6, 30),
            ReportingPeriodEndDate = new DateTime(2023, 12, 31),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation")
        };
        equalisation.ChangeSection104(ukSection104);

        sellTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(9900m)); // 10000 - 100
        ukSection104.Quantity.ShouldBe(0m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }
}
