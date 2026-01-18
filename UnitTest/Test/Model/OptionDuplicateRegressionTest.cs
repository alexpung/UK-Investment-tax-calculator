using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using Shouldly;
using System.Collections.Immutable;

namespace UnitTest.Test.Model;

public class OptionDuplicateRegressionTest
{
    [Fact]
    public void AddData_WithSkipDuplicates_ShouldFilterOptionTrades_AfterOptionHelperModification()
    {
        // 1. Arrange: Create an OptionTrade with GrossProceed = 0 (as parsed)
        var date = new DateTime(2023, 1, 1);
        var option1 = new OptionTrade
        {
            AssetName = "OPT",
            Date = date,
            AcquisitionDisposal = TradeType.DISPOSAL,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Underlying = "U",
            StrikePrice = new WrappedMoney(100, "USD"),
            ExpiryDate = date,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Expenses = []
        };
        
        var list1 = new TaxEventLists { OptionTrades = [option1] };
        
        // 2. Simulate OptionHelper modifying the existing trade
        // In the real app, this happens when a CashSettlement is matched
        option1.GrossProceed = option1.GrossProceed with { Amount = new WrappedMoney(500, "USD") };
        option1.SettlementMethod = SettlementMethods.CASH;

        // 3. New import (list2) has the original trade (amount 0) as parsed from XML
        var option2 = option1 with { 
            GrossProceed = new DescribedMoney(0, "USD", 1),
            SettlementMethod = SettlementMethods.UNKNOWN
        };
        var list2 = new TaxEventLists { OptionTrades = [option2] };

        // 4. Act: Try to add list2 to list1 with skipDuplicates
        list1.AddData(list2, skipDuplicates: true);

        // 5. Assert: Should only have 1 trade
        list1.OptionTrades.Count.ShouldBe(1, "Duplicate option added because GrossProceed amount changed");
    }
}
