using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using NSubstitute;

namespace UnitTest.Test.Model;

public class TaxEventListsTests
{
    [Fact]
    public void GetTotalNumberOfEvents_ReturnsCorrectTotalNumberOfEvents()
    {
        // Arrange
        var taxEventLists = new TaxEventLists
        {
            Trades =
            [
                Substitute.For<Trade>(),
                Substitute.For<Trade>()
            ],
            CorporateActions =
            [
                Substitute.For<StockSplit>()
            ],
            Dividends =
            [
                Substitute.For<Dividend>()
            ]
        };

        // Act
        int totalNumberOfEvents = taxEventLists.GetTotalNumberOfEvents();

        // Assert
        totalNumberOfEvents.ShouldBe(4);
    }

    [Fact]
    public void AddData_AddsTaxEventLists_CombinesAllLists()
    {
        // Arrange
        var sourceTaxEventLists = new TaxEventLists
        {
            Trades =
            [
                Substitute.For<Trade>(),
            ],
            CorporateActions =
            [
                Substitute.For<StockSplit>(),
                Substitute.For<StockSplit>()
            ],
            Dividends =
            [
                Substitute.For<Dividend>(),
                Substitute.For<Dividend>(),
                Substitute.For<Dividend>(),
            ]
        };

        var targetTaxEventLists = new TaxEventLists();

        // Act
        targetTaxEventLists.AddData(sourceTaxEventLists);

        // Assert
        targetTaxEventLists.Trades.ShouldBe(sourceTaxEventLists.Trades);
        targetTaxEventLists.CorporateActions.ShouldBe(sourceTaxEventLists.CorporateActions);
        targetTaxEventLists.Dividends.ShouldBe(sourceTaxEventLists.Dividends);
    }

    [Fact]
    public void AddData_AddsTaxEvents_CombinesAllLists()
    {
        // Arrange
        var taxEvents = new List<TaxEvent>
        {
            Substitute.For<Trade>(),
            Substitute.For<StockSplit>(),
            Substitute.For<StockSplit>(),
            Substitute.For<Dividend>(),
            Substitute.For<Dividend>(),
            Substitute.For<Dividend>(),
            Substitute.For<FutureContractTrade>(),
            Substitute.For<OptionTrade>(),
        };

        var taxEventLists = new TaxEventLists();

        // Act
        taxEventLists.AddData(taxEvents);

        // Assert
        taxEventLists.Trades.Count.ShouldBe(1);
        taxEventLists.CorporateActions.ShouldBe(taxEvents.OfType<CorporateAction>());
        taxEventLists.Dividends.ShouldBe(taxEvents.OfType<Dividend>());
        taxEventLists.OptionTrades.ShouldBe(taxEvents.OfType<OptionTrade>());
        taxEventLists.FutureContractTrades.ShouldBe(taxEvents.OfType<FutureContractTrade>());
    }
    [Fact]
    public void GetDuplicates_WithRealRecords_IdentifiesExactMatches()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var trade1 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 10, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };
        
        // trade2 is identical to trade1 (Value equality)
        var trade2 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 10, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };
        
        // trade3 differs by quantity
        var trade3 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 20, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };

        var list1 = new TaxEventLists { Trades = [trade1] };
        var list2 = new TaxEventLists { Trades = [trade2, trade3] }; // trade2 duplicates trade1

        // Act
        var duplicates = list1.GetDuplicates(list2);

        // Assert
        // Intersection should coincide with trade1/trade2
        duplicates.Trades.Count.ShouldBe(1);
        duplicates.Trades[0].GetDuplicateSignature().ShouldBe(trade1.GetDuplicateSignature());
    }

    [Fact]
    public void AddData_WithSkipDuplicates_DoesNotAddExistingItems()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        var trade1 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 10, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };
        
        var trade2 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 10, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };
        
        var trade3 = new Trade 
        { 
            AssetName = "Asset", Date = date, Quantity = 20, AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION, 
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            Expenses = System.Collections.Immutable.ImmutableList<DescribedMoney>.Empty
        };

        var list1 = new TaxEventLists { Trades = [trade1] };
        var list2 = new TaxEventLists { Trades = [trade2, trade3] }; // trade2 matches trade1

        // Act
        list1.AddData(list2, skipDuplicates: true);

        // Assert
        list1.Trades.Count.ShouldBe(2); // trade1 and trade3
        list1.Trades.ShouldContain(t => t.Quantity == 20);
        list1.Trades.Count(t => t.Quantity == 10).ShouldBe(1);
    }
}

