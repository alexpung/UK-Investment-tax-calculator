using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;

/// <summary>
/// Tests for partial exercise scenarios where some options are exercised and some expire.
/// Addresses EDGE CASE #1 from option_analysis_report.txt
/// </summary>
public class PartialExerciseScenarioTest
{
    [Theory]
    [InlineData(70, 30)] // Exercise 70%, expire 30%
    [InlineData(50, 50)] // Exercise 50%, expire 50%
    [InlineData(30, 70)] // Exercise 30%, expire 70%
    [InlineData(1, 99)]  // Exercise 1%, expire 99%
    [InlineData(99, 1)]  // Exercise 99%, expire 1%
    public void LongCallOption_PartialExercisePartialExpire_ShouldCalculateCorrectly(int exerciseQty, int expireQty)
    {
        // Scenario: Buy 100 call options, exercise some, let rest expire

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("31-Jan-23 16:00:00", CultureInfo.InvariantCulture);

        var totalQty = exerciseQty + expireQty;
        var premiumPerOption = 50m;
        var totalPremium = totalQty * premiumPerOption;
        var expenses = 25.50m;

        var buyOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230131C00150000",
            Date = buyDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = totalQty,
            GrossProceed = new DescribedMoney(totalPremium, "USD", 1),
            Expenses = [new DescribedMoney(expenses, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = $"Buy {totalQty} AAPL Call Options"
        };

        var exerciseTrade = new OptionTrade
        {
            AssetName = "AAPL230131C00150000",
            Date = expiryDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = exerciseQty,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = $"Exercise {exerciseQty} AAPL Call Options"
        };

        var underlyingTrade = new Trade
        {
            AssetName = "AAPL",
            Date = expiryDate,
            Quantity = exerciseQty * 100, // multiplier
            GrossProceed = new DescribedMoney(exerciseQty * 100 * 150, "USD", 1), // strike price
            Expenses = [new DescribedMoney(15, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = $"Exercise call to buy {exerciseQty * 100} shares",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        var expireTrade = new OptionTrade
        {
            AssetName = "AAPL230131C00150000",
            Date = expiryDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = expireQty,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = $"{expireQty} AAPL Call Options Expired"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent> { buyOptionTrade, exerciseTrade, underlyingTrade, expireTrade },
            out UkSection104Pools section104Pools
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var acquisitionCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.ACQUISITION);
        var disposalCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.DISPOSAL);

        // Verify all quantity was matched
        acquisitionCalc.UnmatchedQty.ShouldBe(0, "All option quantity should be matched");
        disposalCalc.UnmatchedQty.ShouldBe(0, "All disposal quantity should be matched");

        // Verify quantities on disposal side
        disposalCalc.ExpiredQty.ShouldBe(expireQty, "Expired quantity should be tracked on disposal side");
        disposalCalc.OwnerExercisedQty.ShouldBe(exerciseQty, "Exercised quantity should be tracked on disposal side");

        // Verify S104 pool for underlying stock
        var applePool = section104Pools.GetExistingOrInitialise("AAPL");
        applePool.Quantity.ShouldBe(exerciseQty * 100, "S104 should contain exercised shares");

        // Calculate expected S104 cost
        // For exercised options: strike price + premium proportion + expenses proportion + exercise cost
        // premium proportion = (totalPremium + initialExpenses) * exerciseQty / totalQty
        // exercise cost = exercise trade expense
        var exercisePremiumProportion = (totalPremium + expenses) * exerciseQty / totalQty;
        var exerciseCommission = 10m; // exercise trade expense
        var underlyingStrikeCost = exerciseQty * 100 * 150m; // strike price * shares
        var underlyingBuyCommission = 15m;
        var expectedS104Cost = underlyingStrikeCost + underlyingBuyCommission + exercisePremiumProportion + exerciseCommission;

        applePool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(expectedS104Cost, 0.01m,
            $"S104 cost should include strike price, premium proportion, and all expenses");
    }

    [Fact]
    public void LongPutOption_PartialExercisePartialExpire_ShouldCalculateCorrectly()
    {
        // Scenario: Buy 100 put options, exercise 40, let 60 expire

        var buyDate = DateTime.Parse("01-Feb-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("28-Feb-23 16:00:00", CultureInfo.InvariantCulture);

        var buyOptionTrade = new OptionTrade
        {
            AssetName = "TSLA230228P00180000",
            Date = buyDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(180),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(8000, "USD", 1), // $80 per option
            Expenses = [new DescribedMoney(50, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "Buy 100 TSLA Put Options"
        };

        var exerciseTrade = new OptionTrade
        {
            AssetName = "TSLA230228P00180000",
            Date = expiryDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(180),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 40,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(12, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "Exercise 40 TSLA Put Options"
        };

        var underlyingTrade = new Trade
        {
            AssetName = "TSLA",
            Date = expiryDate,
            Quantity = 4000, // 40 options * 100 multiplier
            GrossProceed = new DescribedMoney(720000, "USD", 1), // 40 * 100 * $180
            Expenses = [new DescribedMoney(20, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Exercise put to sell 4000 shares",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        var expireTrade = new OptionTrade
        {
            AssetName = "TSLA230228P00180000",
            Date = expiryDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(180),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 60,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "60 TSLA Put Options Expired"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent> { buyOptionTrade, exerciseTrade, underlyingTrade, expireTrade },
            out _
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var acquisitionCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.ACQUISITION);
        var disposalCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.DISPOSAL);

        // Verify quantities
        acquisitionCalc.UnmatchedQty.ShouldBe(0);
        disposalCalc.UnmatchedQty.ShouldBe(0);
        disposalCalc.ExpiredQty.ShouldBe(60);
        disposalCalc.OwnerExercisedQty.ShouldBe(40);

        // Verify matches exist
        disposalCalc.MatchHistory.Count.ShouldBeGreaterThan(0, "Disposal calculation should have matches");

        // Expired options (60) should result in loss of premium
        // Exercised options (40) premium is rolled to underlying
    }

    [Fact]
    public void MultiplePartialExercises_ShouldProportionCorrectly()
    {
        // Scenario: Buy 100 options, exercise 25 on day 1, exercise 25 on day 2, let 50 expire

        var buyDate = DateTime.Parse("01-Mar-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("31-Mar-23 16:00:00", CultureInfo.InvariantCulture);

        var buyOptionTrade = new OptionTrade
        {
            AssetName = "NVDA230331C00250000",
            Date = buyDate,
            Underlying = "NVDA",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(10000, "USD", 1), // $100 per option
            Expenses = [new DescribedMoney(100, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "Buy 100 NVDA Call Options"
        };

        // First exercise
        var exercise1Trade = new OptionTrade
        {
            AssetName = "NVDA230331C00250000",
            Date = expiryDate.AddDays(-5),
            Underlying = "NVDA",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 25,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(8, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "Exercise 25 NVDA Call Options (batch 1)"
        };

        var underlying1Trade = new Trade
        {
            AssetName = "NVDA",
            Date = expiryDate.AddDays(-5),
            Quantity = 2500,
            GrossProceed = new DescribedMoney(625000, "USD", 1), // 25 * 100 * $250
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 2500 shares (batch 1)",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        // Second exercise
        var exercise2Trade = new OptionTrade
        {
            AssetName = "NVDA230331C00250000",
            Date = expiryDate.AddDays(-2),
            Underlying = "NVDA",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 25,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(8, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "Exercise 25 NVDA Call Options (batch 2)"
        };

        var underlying2Trade = new Trade
        {
            AssetName = "NVDA",
            Date = expiryDate.AddDays(-2),
            Quantity = 2500,
            GrossProceed = new DescribedMoney(625000, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 2500 shares (batch 2)",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        // Expiration
        var expireTrade = new OptionTrade
        {
            AssetName = "NVDA230331C00250000",
            Date = expiryDate,
            Underlying = "NVDA",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 50,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN,
            Description = "50 NVDA Call Options Expired"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            [buyOptionTrade, exercise1Trade, underlying1Trade, exercise2Trade, underlying2Trade, expireTrade],
            out UkSection104Pools section104Pools
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var acquisitionCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.ACQUISITION);
        var disposalCalcs = optionResults.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();

        // Verify all quantity matched
        acquisitionCalc.UnmatchedQty.ShouldBe(0);
        disposalCalcs.Sum(c => c.OwnerExercisedQty).ShouldBe(50, "Total exercised quantity should be 50");
        disposalCalcs.Sum(c => c.ExpiredQty).ShouldBe(50, "Total expired quantity should be 50");

        // Verify S104 pool contains both exercise batches
        var nvdaPool = section104Pools.GetExistingOrInitialise("NVDA");
        nvdaPool.Quantity.ShouldBe(5000, "S104 should contain shares from both exercises");

        // Each exercise should get 25% of the premium (25 out of 100 options)
        var premiumPerExercise = (10000m + 100m) * 0.25m; // (premium + expenses) * 25%
        var expectedCostPerBatch = 625000m + 10m + premiumPerExercise + 8m; // strike + underlying expense + premium proportion + exercise expense
        var expectedTotalCost = expectedCostPerBatch * 2;

        nvdaPool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(expectedTotalCost, 0.01m,
            "S104 cost should include proportioned premium for both exercise batches");
    }

    /// <summary>
    /// Test partial match with exercise via public API
    /// Scenario: Buy 100 options, sell back 60, exercise 40
    /// This verifies the bug fix where matchExerciseQty calculation is correct
    /// </summary>
    [Fact]
    public void PartialDisposalWithPartialExercise_ShouldCalculateCorrectly()
    {
        // Arrange: Buy 100 call options, then sell 60 back and exercise 40
        var buyDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var disposalDate = new DateTime(2023, 2, 15, 0, 0, 0, DateTimeKind.Local);
        var exerciseDate = new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local);

        var buyTrade = new OptionTrade
        {
            AssetName = "AAPL230317C00150000",
            Date = buyDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = exerciseDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(1000, "GBP", 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            SettlementMethod = SettlementMethods.UNKNOWN
        };

        // Sell back 60 of the 100
        var sellTrade = new OptionTrade
        {
            AssetName = "AAPL230317C00150000",
            Date = disposalDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = exerciseDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 60,
            GrossProceed = new DescribedMoney(800, "GBP", 1),
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN
        };

        // Exercise remaining 40
        var exerciseTrade = new OptionTrade
        {
            AssetName = "AAPL230317C00150000",
            Date = exerciseDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = exerciseDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 40,
            GrossProceed = new DescribedMoney(0, "GBP", 1),
            Expenses = [new DescribedMoney(10, "GBP", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            SettlementMethod = SettlementMethods.UNKNOWN
        };

        var underlyingTrade = new Trade
        {
            AssetName = "AAPL",
            Date = exerciseDate,
            Quantity = 4000,  // 40 options * 100 multiplier
            GrossProceed = new DescribedMoney(600000, "GBP", 1),  // 40 * 100 * 150
            Expenses = [new DescribedMoney(15, "GBP", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            TradeReason = TradeReason.OwnerExerciseOption
        };

        // Act
        var results = TradeCalculationHelper.CalculateTrades(
            [buyTrade, sellTrade, exerciseTrade, underlyingTrade],
            out UkSection104Pools section104Pools
        );

        // Assert
        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var acquisitionCalc = optionResults.First(t => t.AcquisitionDisposal == TradeType.ACQUISITION);

        // All 100 options matched: 60 via disposal + 40 via exercise
        acquisitionCalc.UnmatchedQty.ShouldBe(0, "All 100 options should be matched");

        // Verify underlying shares in S104 pool
        var applePool = section104Pools.GetExistingOrInitialise("AAPL");
        applePool.Quantity.ShouldBe(4000, "S104 should contain 4000 shares from exercised options");
    }
}
