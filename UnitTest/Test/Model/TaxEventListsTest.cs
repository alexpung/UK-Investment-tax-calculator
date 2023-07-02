using Model;
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
            Trades = new List<Trade>
            {
                new Mock<Trade>().Object,
                new Mock<Trade>().Object
            },
            CorporateActions = new List<CorporateAction>
            {
                new Mock<StockSplit>().Object
            },
            Dividends = new List<Dividend>
            {
                new Mock<Dividend>().Object
            }
        };

        // Act
        int totalNumberOfEvents = taxEventLists.GetTotalNumberOfEvents();

        // Assert
        Assert.Equal(4, totalNumberOfEvents);
    }

    [Fact]
    public void AddData_AddsTaxEventLists_CombinesAllLists()
    {
        // Arrange
        var sourceTaxEventLists = new TaxEventLists
        {
            Trades = new List<Trade>
            {
                new Mock<Trade>().Object,
            },
            CorporateActions = new List<CorporateAction>
            {
                new Mock<StockSplit>().Object,
                new Mock<StockSplit>().Object
            },
            Dividends = new List<Dividend>
            {
                new Mock<Dividend>().Object,
                new Mock<Dividend>().Object,
                new Mock<Dividend>().Object,
            }
        };

        var targetTaxEventLists = new TaxEventLists();

        // Act
        targetTaxEventLists.AddData(sourceTaxEventLists);

        // Assert
        Assert.Equal(sourceTaxEventLists.Trades, targetTaxEventLists.Trades);
        Assert.Equal(sourceTaxEventLists.CorporateActions, targetTaxEventLists.CorporateActions);
        Assert.Equal(sourceTaxEventLists.Dividends, targetTaxEventLists.Dividends);
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
        Assert.Equal(taxEvents.OfType<Trade>(), taxEventLists.Trades);
        Assert.Equal(taxEvents.OfType<CorporateAction>(), taxEventLists.CorporateActions);
        Assert.Equal(taxEvents.OfType<Dividend>(), taxEventLists.Dividends);
    }
}

