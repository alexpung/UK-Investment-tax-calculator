using Enumerations;

using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel.Stocks;

using Moq;

namespace UnitTest.Helper;

public static class MockTrade
{
    public static Mock<ITradeTaxCalculation> CreateMockITradeTaxCalculation(decimal quantity, decimal value, TradeType tradeType)
    {
        Mock<ITradeTaxCalculation> mockTrade = new();
        mockTrade.Setup(f => f.AcquisitionDisposal).Returns(tradeType);
        mockTrade.Setup(f => f.MatchHistory).Returns([]);
        mockTrade.Setup(f => f.UnmatchedQty).Returns(quantity);
        mockTrade.Setup(f => f.UnmatchedCostOrProceed).Returns(new WrappedMoney(value));
        mockTrade.Setup(f => f.TradeList).Returns([]);
        mockTrade.Setup(f => f.MatchQty(It.IsAny<decimal>()));
        return mockTrade;
    }

    public static Mock<Trade> CreateMockTrade(string assetName, DateTime dateTime, TradeType tradeType, decimal quantity, decimal baseCurrencyAmount)
    {
        var mockTrade = new Mock<Trade>();
        mockTrade.SetupGet(t => t.AssetName).Returns(assetName);
        mockTrade.SetupGet(t => t.Date).Returns(dateTime);
        mockTrade.SetupGet(t => t.AcquisitionDisposal).Returns(tradeType);
        mockTrade.SetupGet(t => t.Quantity).Returns(quantity);
        mockTrade.SetupGet(t => t.NetProceed).Returns(new WrappedMoney(baseCurrencyAmount));
        return mockTrade;
    }

    public static TradeTaxCalculation CreateTradeTaxCalculation(string assetName, DateTime dateTime, decimal quantity, decimal value, TradeType tradeType)
    {
        Mock<Trade> trade = CreateMockTrade(assetName, dateTime, tradeType, quantity, value);
        return new TradeTaxCalculation(new List<Trade> { trade.Object });
    }
}
