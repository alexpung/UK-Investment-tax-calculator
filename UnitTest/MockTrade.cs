using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using Moq;

namespace UnitTest;
public static class MockTrade
{
    public static Mock<ITradeTaxCalculation> CreateMockTrade(decimal quantity, decimal value, TradeType tradeType)
    {
        Mock<ITradeTaxCalculation> mockTrade = new();
        mockTrade.Setup(f => f.MatchAll()).Returns((quantity, value));
        mockTrade.Setup(f => f.BuySell).Returns(tradeType);
        mockTrade.Setup(f => f.MatchHistory).Returns(new List<TradeMatch>());
        mockTrade.Setup(f => f.UnmatchedQty).Returns(quantity);
        mockTrade.Setup(f => f.TradeList).Returns(new List<Trade>());
        mockTrade.Setup(f => f.MatchQty(It.IsAny<decimal>())).Returns<decimal>(x => (x, x * value / quantity));
        return mockTrade;
    }
}
