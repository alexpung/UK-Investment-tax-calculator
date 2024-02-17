using Model;
using Model.TaxEvents;

using Moq;

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
                new Mock<Trade>().Object,
                new Mock<Trade>().Object
            ],
            CorporateActions =
            [
                new Mock<StockSplit>().Object
            ],
            Dividends =
            [
                new Mock<Dividend>().Object
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
                new Mock<Trade>().Object,
            ],
            CorporateActions =
            [
                new Mock<StockSplit>().Object,
                new Mock<StockSplit>().Object
            ],
            Dividends =
            [
                new Mock<Dividend>().Object,
                new Mock<Dividend>().Object,
                new Mock<Dividend>().Object,
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
            new Mock<Trade>().Object,
            new Mock<StockSplit>().Object,
            new Mock<StockSplit>().Object,
            new Mock<Dividend>().Object,
            new Mock<Dividend>().Object,
            new Mock<Dividend>().Object,
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

