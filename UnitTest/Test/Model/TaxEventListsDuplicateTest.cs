using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using Shouldly;
using System.Collections.Immutable;

namespace UnitTest.Test.Model;

public class TaxEventListsDuplicateTest
{
    [Fact]
    public void AddData_WithSkipDuplicates_ShouldFilterStockSplits()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var split1 = new StockSplit
        {
            AssetName = "Asset",
            Date = date,
            SplitTo = 2,
            SplitFrom = 1
        };
        // Identical to split1
        var split2 = new StockSplit
        {
            AssetName = "Asset",
            Date = date,
            SplitTo = 2,
            SplitFrom = 1
        };
        
        var list1 = new TaxEventLists { CorporateActions = [split1] };
        var list2 = new TaxEventLists { CorporateActions = [split2] };

        // Act
        list1.AddData(list2, skipDuplicates: true);

        // Assert
        list1.CorporateActions.Count.ShouldBe(1);
    }

    [Fact]
    public void AddData_WithSkipDuplicates_ShouldFilterOptionTrades()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var expiry = date.AddMonths(1);
        var option1 = new OptionTrade
        {
            AssetName = "OPT",
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 1,
            GrossProceed = new DescribedMoney(10, "USD", 1),
            Underlying = "U",
            StrikePrice = new WrappedMoney(100, "USD"),
            ExpiryDate = expiry,
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            Expenses = ImmutableList<DescribedMoney>.Empty
        };
        var option2 = option1 with { }; // Clone

        var list1 = new TaxEventLists { OptionTrades = [option1] };
        var list2 = new TaxEventLists { OptionTrades = [option2] };

        // Act
        list1.AddData(list2, skipDuplicates: true);

        // Assert
        list1.OptionTrades.Count.ShouldBe(1);
    }

    [Fact]
    public void AddData_WithSkipDuplicates_ShouldFilterFutureContractTrades()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var future1 = new FutureContractTrade
        {
            AssetName = "FUT",
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 1,
            GrossProceed = new DescribedMoney(10, "USD", 1),
            ContractValue = new DescribedMoney(1000, "USD", 1),
            PositionType = PositionType.OPENLONG,
            Expenses = ImmutableList<DescribedMoney>.Empty
        };
        var future2 = future1 with { };

        var list1 = new TaxEventLists { FutureContractTrades = [future1] };
        var list2 = new TaxEventLists { FutureContractTrades = [future2] };

        // Act
        list1.AddData(list2, skipDuplicates: true);

        // Assert
        list1.FutureContractTrades.Count.ShouldBe(1);
    }

    [Fact]
    public void AddData_WithSkipDuplicates_ShouldFilterCashSettlements()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var cash1 = new CashSettlement
        {
            AssetName = "CASH",
            Date = date,
            Amount = new WrappedMoney(100, "USD"),
            Description = "Settlement",
            TradeReason = TradeReason.OrderedTrade
        };
        var cash2 = cash1 with { };

        var list1 = new TaxEventLists { CashSettlements = [cash1] };
        var list2 = new TaxEventLists { CashSettlements = [cash2] };

        // Act
        list1.AddData(list2, skipDuplicates: true);

        // Assert
        list1.CashSettlements.Count.ShouldBe(1);
    }
}
