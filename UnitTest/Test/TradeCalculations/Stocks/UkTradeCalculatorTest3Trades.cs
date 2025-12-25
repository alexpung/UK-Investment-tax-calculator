using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using NSubstitute;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;
public class UkTradeCalculatorTest3Trades
{
    /// <summary>
    /// Test interaction of Bed and breakfast and short cover
    /// </summary>
    [Fact]
    public void TestCaculateShortPartialCover()
    {
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("08-Apr-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "Commission", Amount = new(1000m) },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("06-May-21 13:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "Commission", Amount = new(1500m) },
        };
        Trade trade3 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("09-May-21 13:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 300,
            GrossProceed = new() { Description = "", Amount = new(5000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade1, trade2, trade3 }, out UkSection104Pools section104Pools);
        result[0].Gain.Amount.ShouldBe(-666.67m, 0.01m);
        result[0].TotalAllowableCost.Amount.ShouldBe(1666.67m, 0.01m);
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SHORTCOVER);
        result[1].Gain.Amount.ShouldBe(-166.67m, 0.01m);
        result[1].TotalAllowableCost.Amount.ShouldBe(1666.67m, 0.01m);
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        section104Pools.GetExistingOrInitialise("DEF").AcquisitionCostInBaseCurrency.Amount.ShouldBe(1666.67m, 0.01m);
        section104Pools.GetExistingOrInitialise("DEF").Quantity.ShouldBe(100);
    }

    [Fact]
    public void TestBreadAndBreakfast3Trades()
    {
        Trade initSection104 = new()
        {
            AssetName = "Mesopotamia plc",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("11-Sep-20 12:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 9500 shares",
            Quantity = 9500,
            GrossProceed = new() { Description = "", Amount = new(1850.0m, "GBP"), FxRate = 1.0m },
        };

        Trade saleTrade1 = new()
        {
            AssetName = "Mesopotamia plc",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("30-Aug-21 14:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of 4,000 shares",
            Quantity = 4000,
            GrossProceed = new() { Description = "", Amount = new(6000.0m, "GBP"), FxRate = 1.0m },
        };


        // This buy trade as bed and breakfast
        Trade purchaseTrade1 = new()
        {
            AssetName = "Mesopotamia plc",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("11-Sep-21 12:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 500 shares",
            Quantity = 500,
            GrossProceed = new() { Description = "", Amount = new(850.0m, "GBP"), FxRate = 1.0m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { initSection104, purchaseTrade1, saleTrade1 }, out UkSection104Pools section104Pools);
        // Bed and Breakfast Matching (500 shares)
        result[2].MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(750m));
        result[2].MatchHistory[0].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(850m));
        result[2].MatchHistory[0].MatchAcquisitionQty.ShouldBe(500);
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        // Section 104 Holding Matching (3,500 shares)
        result[2].MatchHistory[1].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(5250m)); // (6000 * 3500 / 4000)
        result[2].MatchHistory[1].BaseCurrencyMatchAllowableCost.Amount.ShouldBe(681.58m, 0.01m); // (1850 * 3500 / 9500)
        result[2].MatchHistory[1].MatchAcquisitionQty.ShouldBe(3500);
        result[2].MatchHistory[1].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);

        //Total
        result[2].Gain.Amount.ShouldBe(4468.42m, 0.01m); // (6000 * 500 / 4000 - 850) + (6000 * 3500 / 4000) - (1850 * 3500 / 9500)
        result[2].TotalAllowableCost.Amount.ShouldBe(1531.58m, 0.01m);

        // Ensure the Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("Mesopotamia plc").AcquisitionCostInBaseCurrency.Amount.ShouldBe(1168.42m, 0.01m); // 1850 - (1850 * 3500 / 9500)
        section104Pools.GetExistingOrInitialise("Mesopotamia plc").Quantity.ShouldBe(6000);
    }

    [Fact]
    public void TestSameDayAndBedAndBreakfastMatching()
    {
        // Create trades representing the scenario
        Trade purchaseTrade1 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 100 shares",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(2.0m, "GBP"), FxRate = 1.0m }],
            GrossProceed = new() { Description = "", Amount = new(1000.0m, "GBP"), FxRate = 1.0m },
        };

        Trade saleTrade1 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Jan-22 14:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of 200 shares (Same Day)",
            Quantity = 200,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.0m, "GBP"), FxRate = 1.0m }],
            GrossProceed = new() { Description = "", Amount = new(2200.0m, "GBP"), FxRate = 1.0m },
        };

        Trade purchaseTrade2 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Feb-22 10:00:00", CultureInfo.InvariantCulture), // Within 30 days
            Description = "Purchase of 100 shares (Bed and Breakfast)",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(2.0m, "GBP"), FxRate = 1.0m }],
            GrossProceed = new() { Description = "", Amount = new(1100.0m, "GBP"), FxRate = 1.0m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { purchaseTrade1, saleTrade1, purchaseTrade2 }, out _);
        // Same Day Matching (SaleTrade1)
        decimal gainFromOwnedShares = (10.995m * 100) - (10.02m * 100); // £97.5
        decimal lossFromShortCover = (11.02m * 100) - (10.995m * 100);  // £2.5
        decimal totalGain = gainFromOwnedShares - lossFromShortCover;   // £95

        result[1].Gain.Amount.ShouldBe(totalGain, 0.01m);
        result[1].TotalAllowableCost.Amount.ShouldBe(2104.0m, 0.01m); // Cost basis for owned shares
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        result[1].MatchHistory[1].TradeMatchType.ShouldBe(TaxMatchType.SHORTCOVER);
    }

    [Fact]
    public void TestMultipleTradesWithPooling()
    {
        Trade initialPurchase = new()
        {
            AssetName = "AncientArtifacts Ltd",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("15-Jan-20 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 10,000 shares",
            Quantity = 10000,
            GrossProceed = new() { Description = "", Amount = new(2000.0m, "GBP"), FxRate = 1.0m },
        };

        Trade saleTrade = new()
        {
            AssetName = "AncientArtifacts Ltd",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-21 11:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of 3,000 shares",
            Quantity = 3000,
            GrossProceed = new() { Description = "", Amount = new(2500.0m, "GBP"), FxRate = 1.0m },
        };

        Trade additionalPurchase = new()
        {
            AssetName = "AncientArtifacts Ltd",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Sep-21 15:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 2,000 shares",
            Quantity = 2000,
            GrossProceed = new() { Description = "", Amount = new(1500.0m, "GBP"), FxRate = 1.0m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { initialPurchase, saleTrade, additionalPurchase }, out UkSection104Pools section104Pools);
        // Assert the expected results
        // Short Cover Matching (2000 shares)
        result[1].MatchHistory[0].BaseCurrencyMatchDisposalProceed.Amount.ShouldBe(1666.67m, 0.01m);
        result[1].MatchHistory[0].BaseCurrencyMatchAllowableCost.Amount.ShouldBe(1500m, 0.01m);
        result[1].MatchHistory[0].MatchAcquisitionQty.ShouldBe(2000);
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);

        // Section 104 Holding Matching (1000 shares)
        result[1].MatchHistory[1].BaseCurrencyMatchDisposalProceed.Amount.ShouldBe(833.33m, 0.01m);
        result[1].MatchHistory[1].BaseCurrencyMatchAllowableCost.Amount.ShouldBe(200m, 0.01m);
        result[1].MatchHistory[1].MatchAcquisitionQty.ShouldBe(1000);
        result[1].MatchHistory[1].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);

        //Total
        result[1].Gain.Amount.ShouldBe(800m, 0.01m);
        result[1].TotalAllowableCost.Amount.ShouldBe(1700m, 0.01m);

        // Ensure the Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("AncientArtifacts Ltd").AcquisitionCostInBaseCurrency.Amount.ShouldBe(1800m, 0.01m);
        section104Pools.GetExistingOrInitialise("AncientArtifacts Ltd").Quantity.ShouldBe(9000);
    }


    [Theory]
    [InlineData("2006-3-10", "2006-3-20", ResidencyStatus.NonResident, true)]
    [InlineData("2006-3-10", "2006-3-21", ResidencyStatus.NonResident, true)]
    [InlineData("2006-3-10", "2006-3-22", ResidencyStatus.NonResident, false)]
    [InlineData("2006-3-10", "2006-3-22", ResidencyStatus.Resident, true)]
    [InlineData("2006-3-22", "2006-3-29", ResidencyStatus.TemporaryNonResident, false)]
    public void TestBedAndBreakfastMatchingApplyWhenNonResident(string disposaldate, string acquisitiondate, ResidencyStatus residencyStatus, bool rulesApply)
    {
        Trade mockTrade = NSubstitute.Substitute.For<Trade>();
        mockTrade.Quantity.Returns(100);
        mockTrade.AssetType.Returns(AssetCategoryType.STOCK);
        ITradeTaxCalculation disposalTradeTaxCalculation = Substitute.For<ITradeTaxCalculation>();
        disposalTradeTaxCalculation.ResidencyStatusAtTrade.Returns(ResidencyStatus.NonResident);
        disposalTradeTaxCalculation.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        disposalTradeTaxCalculation.Date.Returns(DateTime.Parse(disposaldate, CultureInfo.InvariantCulture));
        ITradeTaxCalculation acquisitionTradeTaxCalculation = Substitute.For<ITradeTaxCalculation>();
        acquisitionTradeTaxCalculation.ResidencyStatusAtTrade.Returns(residencyStatus);
        acquisitionTradeTaxCalculation.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        acquisitionTradeTaxCalculation.Date.Returns(DateTime.Parse(acquisitiondate, CultureInfo.InvariantCulture));
        GroupedTradeContainer<ITradeTaxCalculation> tradesToBeMatched = new([disposalTradeTaxCalculation, acquisitionTradeTaxCalculation], []);
        var result = UkMatchingRules.ApplyBedAndBreakfastMatchingRule(tradesToBeMatched);
        if (rulesApply)
        {
            result.ShouldNotBeEmpty();
            result.First().Item1.ShouldBe(disposalTradeTaxCalculation);
            result.First().Item2.ShouldBe(acquisitionTradeTaxCalculation);
        }
        else
        {
            result.ShouldBeEmpty();
        }
    }
}
