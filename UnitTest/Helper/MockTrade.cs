using Enum;
using Model;
using Model.Interfaces;
using Moq;

namespace UnitTest.Helper;

public static class MockTrade
{
    public static Mock<ITradeTaxCalculation> CreateMockITradeTaxCalculation(decimal quantity, decimal value, TradeType tradeType)
    {
        Mock<ITradeTaxCalculation> mockTrade = new();
        mockTrade.Setup(f => f.MatchAll()).Returns((quantity, new WrappedMoney(value)));
        mockTrade.Setup(f => f.BuySell).Returns(tradeType);
        mockTrade.Setup(f => f.MatchHistory).Returns(new List<TradeMatch>());
        mockTrade.Setup(f => f.UnmatchedQty).Returns(quantity);
        mockTrade.Setup(f => f.TradeList).Returns(new List<Trade>());
        mockTrade.Setup(f => f.MatchQty(It.IsAny<decimal>())).Returns<decimal>(x => (x, new WrappedMoney(value * x / quantity)));
        return mockTrade;
    }

    public static Mock<Trade> CreateMockTrade(string assetName, DateTime dateTime, TradeType tradeType, decimal quantity, decimal baseCurrencyAmount)
    {
        var mockTrade = new Mock<Trade>();
        mockTrade.SetupGet(t => t.AssetName).Returns(assetName);
        mockTrade.SetupGet(t => t.Date).Returns(dateTime);
        mockTrade.SetupGet(t => t.BuySell).Returns(tradeType);
        mockTrade.SetupGet(t => t.Quantity).Returns(quantity);
        mockTrade.SetupGet(t => t.NetProceed).Returns(new WrappedMoney(baseCurrencyAmount));
        return mockTrade;
    }
}
