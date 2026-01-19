using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using System.Globalization;
using UnitTest.Helper;
using Xunit;
using Shouldly;

namespace UnitTest.Test.Reproduction;

public class S104ExplanationDiscrepancyTest
{
    [Fact]
    public void ShortPutOptionAssigned_S104ExplanationShouldMatchValueChange()
    {
        // 1. Setup
        var date = DateTime.Parse("05-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var shortPutOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125P00140000",
            Date = date,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(500, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Put Option"
        };

        var assignedPutOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125P00140000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "AAPL Put Option Assigned",
        };

        var buyStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(14000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Assigned put to buy 100 shares of AAPL",
            TradeReason = TradeReason.OptionAssigned
        };

        // 2. Act
        TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent>() { shortPutOptionTrade, assignedPutOptionTrade, buyStockTrade },
            out UkSection104Pools section104Pools
        );

        // 3. Assert
        var pool = section104Pools.GetExistingOrInitialise("AAPL");
        pool.Quantity.ShouldBe(100);
        
        // Net premium received = 500 - 5 = 495.
        // For a PUT assignment (buy underlying), premium received is deducted from acquisition cost.
        // Acquisition cost = 14000 - 495 = 13505.
        pool.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(13505));

        var lastHistory = pool.Section104HistoryList.Last();
        lastHistory.ValueChange.ShouldBe(new WrappedMoney(13505));
        
        // This is where the bug was: the explanation used to only show "Base cost: 14000.00"
        // and miss the "Option premium adjustment" of -495.
        lastHistory.Explanation.ShouldContain("Option premium adjustment", Case.Insensitive);
        lastHistory.Explanation.ShouldContain("-£495.00", Case.Insensitive);
    }

    [Fact]
    public void LongCallOptionExercised_S104ExplanationShouldMatchValueChange()
    {
        // 1. Setup
        var date = DateTime.Parse("05-Jan-23 09:30:00", CultureInfo.InvariantCulture);
        var buyCallOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230225C00160000",
            Date = date,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(160),
            ExpiryDate = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(700, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL Call Option"
        };

        var exerciseCallOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230225C00160000",
            Date = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(160),
            ExpiryDate = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL Call Option Exercised",
        };

        var buyStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(16000, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 100 shares",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        // 2. Act
        TradeCalculationHelper.CalculateTrades(
            new List<TaxEvent>() { buyCallOptionTrade, exerciseCallOptionTrade, buyStockTrade },
            out UkSection104Pools section104Pools
        );

        // 3. Assert
        var pool = section104Pools.GetExistingOrInitialise("AAPL");
        pool.Quantity.ShouldBe(100);

        // Acquisition cost = 16000 (strike price) + 5 (stock acquisition expense) 
        // + 700 (option premium) + 5 (option premium expense) + 10 (option exercise expense)
        // = 16720.
        pool.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(16720));

        var lastHistory = pool.Section104HistoryList.Last();
        lastHistory.ValueChange.ShouldBe(new WrappedMoney(16720));

        // Explanation should contain the adjustment
        lastHistory.Explanation.ShouldContain("Option premium adjustment", Case.Insensitive);
        // Adjustment amount = 705 (premium + expenses) + 10 (exercise expense) = 715.
        lastHistory.Explanation.ShouldContain("£715.00", Case.Insensitive);
    }
}
