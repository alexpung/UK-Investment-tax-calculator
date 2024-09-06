using InvestmentTaxCalculator.Model.TaxEvents;

using InvestmentTaxCalculator.Model;

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
        };

        var taxEventLists = new TaxEventLists();

        // Act
        taxEventLists.AddData(taxEvents);

        // Assert
        taxEventLists.Trades.ShouldBe(taxEvents.OfType<Trade>());
        taxEventLists.CorporateActions.ShouldBe(taxEvents.OfType<CorporateAction>());
        taxEventLists.Dividends.ShouldBe(taxEvents.OfType<Dividend>());
    }
}

