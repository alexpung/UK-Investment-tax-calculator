using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;

/// <summary>
/// Tests to validate that proportional calculations in option tax calculations
/// maintain precision and don't accumulate unacceptable rounding errors.
/// Addresses Issue #2 from option_analysis_report.txt
/// </summary>
public class OptionProportionalCalculationPrecisionTest
{
    [Fact]
    public void MultiplePartialMatches_ShouldNotAccumulateRoundingErrors()
    {
        // Scenario: Buy 100 options, close in 10 separate trades of 10 each
        // Verify that sum of proportional costs equals total cost (within 1 penny)

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var buyOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230630C00150000",
            Date = buyDate,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(10000, "USD", 1), // $100 per option
            Expenses = [new DescribedMoney(99.99m, "USD", 1)], // Odd amount to test precision
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 100 AAPL Call Options"
        };

        var sellTrades = new List<OptionTrade>();
        for (int i = 0; i < 10; i++)
        {
            sellTrades.Add(new OptionTrade
            {
                AssetName = "AAPL230630C00150000",
                Date = buyDate.AddDays(i + 1),
                Underlying = "AAPL",
                StrikePrice = new WrappedMoney(150),
                ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
                PUTCALL = PUTCALL.CALL,
                Multiplier = 100,
                TradeReason = TradeReason.OrderedTrade,
                Quantity = 10,
                GrossProceed = new DescribedMoney(1100, "USD", 1), // $110 per option
                Expenses = [new DescribedMoney(5.55m, "USD", 1)], // Odd amount
                AcquisitionDisposal = TradeType.DISPOSAL,
                Description = $"Sell 10 AAPL Call Options (batch {i + 1})"
            });
        }

        var allTrades = new List<TaxEvent> { buyOptionTrade };
        allTrades.AddRange(sellTrades);

        var results = TradeCalculationHelper.CalculateTrades(allTrades, out _);
        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();

        // Find all disposal trades (the sell trades)
        var disposalTrades = optionResults.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();

        // Verify all disposal trades were fully matched
        disposalTrades.All(t => t.UnmatchedQty == 0).ShouldBeTrue();

        // Calculate sum of all proportional costs from all disposal matches
        var totalProportionedCost = disposalTrades
            .SelectMany(t => t.MatchHistory)
            .Sum(match => match.BaseCurrencyMatchAllowableCost.Amount);

        // Total cost should be: 10000 + 99.99 = 10099.99
        var expectedTotalCost = 10099.99m;

        // Verify sum of proportional costs equals total cost within 1 penny
        Math.Abs(totalProportionedCost - expectedTotalCost).ShouldBeLessThanOrEqualTo(0.01m,
            $"Sum of proportional costs ({totalProportionedCost}) should equal total cost ({expectedTotalCost}) within 1 penny");
    }

    [Fact]
    public void LargeQuantityProportionalCalculation_ShouldMaintainPrecision()
    {
        // Test with large quantities to ensure decimal precision is maintained

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var buyOptionTrade = new OptionTrade
        {
            AssetName = "SPY230630P00400000",
            Date = buyDate,
            Underlying = "SPY",
            StrikePrice = new WrappedMoney(400),
            ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 9999, // Large quantity
            GrossProceed = new DescribedMoney(999900, "USD", 1), // $100 per option
            Expenses = [new DescribedMoney(123.45m, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 9999 SPY Put Options"
        };

        var sellTrade = new OptionTrade
        {
            AssetName = "SPY230630P00400000",
            Date = buyDate.AddDays(10),
            Underlying = "SPY",
            StrikePrice = new WrappedMoney(400),
            ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 9999,
            GrossProceed = new DescribedMoney(1099890, "USD", 1), // $110 per option
            Expenses = [new DescribedMoney(67.89m, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell 9999 SPY Put Options"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent> { buyOptionTrade, sellTrade },
            out _
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var disposalTrade = optionResults.First(t => t.AcquisitionDisposal == TradeType.DISPOSAL);

        // Verify the proportional calculation is exact (all quantity matched)
        var totalProportionedCost = disposalTrade.MatchHistory
            .Sum(match => match.BaseCurrencyMatchAllowableCost.Amount);

        var expectedTotalCost = 999900m + 123.45m;

        totalProportionedCost.ShouldBe(expectedTotalCost,
            "Large quantity proportional calculation should be exact when all quantity is matched");
    }

    [Fact]
    public void PartialExercise_ProportionalCostsShouldSumCorrectly()
    {
        // Scenario: Buy 100 options, exercise 33, let 67 expire
        // Verify proportional costs sum correctly

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("31-Jan-23 16:00:00", CultureInfo.InvariantCulture);

        var buyOptionTrade = new OptionTrade
        {
            AssetName = "TSLA230131C00200000",
            Date = buyDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(200),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(5000, "USD", 1), // $50 per option
            Expenses = [new DescribedMoney(33.33m, "USD", 1)], // Odd amount for precision test
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 100 TSLA Call Options"
        };

        var exerciseTrade = new OptionTrade
        {
            AssetName = "TSLA230131C00200000",
            Date = expiryDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(200),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 33,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Exercise 33 TSLA Call Options"
        };

        var underlyingTrade = new Trade
        {
            AssetName = "TSLA",
            Date = expiryDate,
            Quantity = 3300, // 33 options * 100 multiplier
            GrossProceed = new DescribedMoney(660000, "USD", 1), // 33 * 100 * $200
            Expenses = [new DescribedMoney(15, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 3300 shares",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        var expireTrade = new OptionTrade
        {
            AssetName = "TSLA230131C00200000",
            Date = expiryDate,
            Underlying = "TSLA",
            StrikePrice = new WrappedMoney(200),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 67,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "67 TSLA Call Options Expired"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent> { buyOptionTrade, exerciseTrade, underlyingTrade, expireTrade },
            out _
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var disposalTrades = optionResults.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();

        // Verify all disposal quantity was matched
        disposalTrades.All(t => t.UnmatchedQty == 0).ShouldBeTrue();

        // Sum of proportional costs from all matches
        var totalProportionedCost = disposalTrades
            .SelectMany(t => t.MatchHistory)
            .Sum(match => match.BaseCurrencyMatchAllowableCost.Amount);

        // ONLY the expired options (67/100) will show their cost in the match history.
        // The exercised options (33/100) have their cost moved to the underlying.
        var expectedTotalCost = (5000m + 33.33m) * 67 / 100;

        // Verify sum is within 1 penny
        Math.Abs(totalProportionedCost - expectedTotalCost).ShouldBeLessThanOrEqualTo(0.01m,
            $"Sum of proportional costs for partial exercise ({totalProportionedCost}) should equal proportioned cost for expired units ({expectedTotalCost}) within 1 penny");
    }

    [Fact]
    public void RepeatedProportionalCalculations_ShouldNotDrift()
    {
        // Test that calling GetProportionedCostOrProceedForTradeReason multiple times
        // with different quantities doesn't cause cumulative drift

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var buyOptionTrade = new OptionTrade
        {
            AssetName = "NVDA230630C00300000",
            Date = buyDate,
            Underlying = "NVDA",
            StrikePrice = new WrappedMoney(300),
            ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1000,
            GrossProceed = new DescribedMoney(75000, "USD", 1), // $75 per option
            Expenses = [new DescribedMoney(77.77m, "USD", 1)], // Odd amount
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 1000 NVDA Call Options"
        };

        // Create 100 sell trades of 10 options each
        var sellTrades = new List<OptionTrade>();
        for (int i = 0; i < 100; i++)
        {
            sellTrades.Add(new OptionTrade
            {
                AssetName = "NVDA230630C00300000",
                Date = buyDate.AddDays(i + 1),
                Underlying = "NVDA",
                StrikePrice = new WrappedMoney(300),
                ExpiryDate = DateTime.Parse("30-Jun-23 16:00:00", CultureInfo.InvariantCulture),
                PUTCALL = PUTCALL.CALL,
                Multiplier = 100,
                TradeReason = TradeReason.OrderedTrade,
                Quantity = 10,
                GrossProceed = new DescribedMoney(800, "USD", 1), // $80 per option
                Expenses = [new DescribedMoney(1.11m, "USD", 1)],
                AcquisitionDisposal = TradeType.DISPOSAL,
                Description = $"Sell 10 NVDA Call Options (batch {i + 1})"
            });
        }

        var allTrades = new List<TaxEvent> { buyOptionTrade };
        allTrades.AddRange(sellTrades);

        var results = TradeCalculationHelper.CalculateTrades(allTrades, out _);
        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();

        var disposalTrades = optionResults.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();

        // Verify all disposal quantity was matched
        disposalTrades.All(t => t.UnmatchedQty == 0).ShouldBeTrue();

        // Sum of all proportional costs
        var totalProportionedCost = disposalTrades.SelectMany(t => t.MatchHistory)
            .Sum(match => match.BaseCurrencyMatchAllowableCost.Amount);

        var expectedTotalCost = 75000m + 77.77m;

        // With 100 separate proportional calculations, verify we're still within 1 penny
        Math.Abs(totalProportionedCost - expectedTotalCost).ShouldBeLessThanOrEqualTo(0.01m,
            $"After 100 proportional calculations, sum ({totalProportionedCost}) should still equal total ({expectedTotalCost}) within 1 penny");
    }

    [Fact]
    public void MixedOutcomes_ProportionalCostsShouldBalance()
    {
        // Test with mixed outcomes: some exercised, some expired, some closed normally
        // Verify that all proportional costs sum to total cost

        var buyDate = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("31-Mar-23 16:00:00", CultureInfo.InvariantCulture);

        var buyOptionTrade = new OptionTrade
        {
            AssetName = "MSFT230331P00250000",
            Date = buyDate,
            Underlying = "MSFT",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 300,
            GrossProceed = new DescribedMoney(15000, "USD", 1), // $50 per option
            Expenses = [new DescribedMoney(99.99m, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 300 MSFT Put Options"
        };

        // Close 100 normally
        var closeTrade = new OptionTrade
        {
            AssetName = "MSFT230331P00250000",
            Date = buyDate.AddDays(30),
            Underlying = "MSFT",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(6000, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell 100 MSFT Put Options"
        };

        // Exercise 100
        var exerciseTrade = new OptionTrade
        {
            AssetName = "MSFT230331P00250000",
            Date = expiryDate,
            Underlying = "MSFT",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 100,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(15, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Exercise 100 MSFT Put Options"
        };

        var underlyingTrade = new Trade
        {
            AssetName = "MSFT",
            Date = expiryDate,
            Quantity = 10000, // 100 options * 100 multiplier
            GrossProceed = new DescribedMoney(2500000, "USD", 1), // 100 * 100 * $250
            Expenses = [new DescribedMoney(20, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Exercise put to sell 10000 shares",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        // Let 100 expire
        var expireTrade = new OptionTrade
        {
            AssetName = "MSFT230331P00250000",
            Date = expiryDate,
            Underlying = "MSFT",
            StrikePrice = new WrappedMoney(250),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 100,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "100 MSFT Put Options Expired"
        };

        var results = TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent> { buyOptionTrade, closeTrade, exerciseTrade, underlyingTrade, expireTrade },
            out _
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var disposalTrades = optionResults.Where(t => t.AcquisitionDisposal == TradeType.DISPOSAL).ToList();

        // Verify all disposal quantity was matched
        disposalTrades.All(t => t.UnmatchedQty == 0).ShouldBeTrue();

        // Sum of all proportional costs across different outcome types
        var totalProportionedCost = disposalTrades
            .SelectMany(t => t.MatchHistory)
            .Sum(match => match.BaseCurrencyMatchAllowableCost.Amount);

        // ONLY Sell (100) and Expire (100) = 200/300 of the options will show their cost here.
        // Exercise (100) has its cost moved to the underlying asset.
        var expectedTotalCost = (15000m + 99.99m) * 200 / 300;

        // Verify precision is maintained across mixed outcomes
        Math.Abs(totalProportionedCost - expectedTotalCost).ShouldBeLessThanOrEqualTo(0.01m,
            $"Sum of proportional costs across mixed outcomes ({totalProportionedCost}) should equal proportioned cost for Sell+Expire units ({expectedTotalCost}) within 1 penny");
    }

    [Fact]
    public void MultiBatchMatch_ExerciseCostAllocation_ShouldBeRobust()
    {
        // Scenario: A disposal is matched against two separate acquisitions (batches)
        // Verify that exercise cost is correctly apportioned using local match quantities

        var date1 = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var date2 = DateTime.Parse("02-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var expiryDate = DateTime.Parse("31-Mar-23 16:00:00", CultureInfo.InvariantCulture);

        // Batch 1: Earlier acquisition (enters S104)
        var buy1 = new OptionTrade
        {
            AssetName = "GOOG230331C00100000",
            Date = date1,
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 100,
            GrossProceed = new DescribedMoney(10000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION
        };

        // Batch 2: Same-day acquisition (matched via Same-Day rule)
        var buy2 = new OptionTrade
        {
            AssetName = "GOOG230331C00100000",
            Date = date2,
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 50,
            GrossProceed = new DescribedMoney(6000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION
        };

        // Disposal: 150 units, 75 of which are exercised.
        // This will match 50 units same-day and 100 units from S104.
        var disposal = new OptionTrade
        {
            AssetName = "GOOG230331C00100000",
            Date = date2,
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 75, // Exercised
            GrossProceed = new DescribedMoney(0, "USD", 1),
            AcquisitionDisposal = TradeType.DISPOSAL
        };

        // Add additional 75 units that expire to make total 150
        var expire = new OptionTrade
        {
            AssetName = "GOOG230331C00100000",
            Date = date2,
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = expiryDate,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 75,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            AcquisitionDisposal = TradeType.DISPOSAL
        };

        var underlying = new Trade
        {
            AssetName = "GOOG",
            Date = date2,
            Quantity = 7500,
            GrossProceed = new DescribedMoney(750000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            TradeReason = TradeReason.OwnerExerciseOption
        };

        var results = TradeCalculationHelper.CalculateTrades(
            [buy1, buy2, disposal, expire, underlying],
            out _
        );

        var optionResults = results.OfType<OptionTradeTaxCalculation>().ToList();
        var combinedDisposal = optionResults.Single(t => t.AcquisitionDisposal == TradeType.DISPOSAL && t.AssetName == "GOOG230331C00100000");

        // Verify total matched quantity is 150
        combinedDisposal.MatchHistory.Sum(m => m.MatchDisposalQty).ShouldBe(150);

        // Verify cost allocation to underlying.
        // Total cost of 150 options is 10000 + 6000 = 16000.
        // 75/150 (50%) are exercised, so 8000 should be transferred.

        // Check the underlying trade's rollover events
        var totalRollover = underlying.TradeEvents.OfType<ExerciseOrAssignmentRollover>().Sum(r => r.ProceedsAdjustment.Amount);
        totalRollover.ShouldBe(8000m, 0.01m, "Total cost transferred to underlying should be 50% of total cost");

        // Ensure allowable cost remaining in the option disposal is for the other 50% (75 expired options)
        combinedDisposal.TotalAllowableCost.Amount.ShouldBe(8000m, 0.01m, "Allowable cost for expired options should be 50% of total cost");
    }
}
