using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using InvestmentTaxCalculator.Model;

using NSubstitute;

namespace UnitTest.Helper;

public static class MockTrade
{
    public static ITradeTaxCalculation CreateMockITradeTaxCalculation(decimal quantity, decimal value, TradeType tradeType)
    {
        ITradeTaxCalculation mockTrade = Substitute.For<ITradeTaxCalculation>();
        mockTrade.AcquisitionDisposal.Returns(tradeType);
        mockTrade.MatchHistory.Returns([]);
        mockTrade.UnmatchedQty.Returns(quantity);
        mockTrade.UnmatchedCostOrProceed.Returns(new WrappedMoney(value));
        mockTrade.TradeList.Returns([]);
        return mockTrade;
    }

    public static Trade CreateMockTrade(string assetName, DateTime dateTime, TradeType tradeType, decimal quantity, decimal baseCurrencyAmount)
    {
        Trade mockTrade = Substitute.For<Trade>();
        mockTrade.AssetName.Returns(assetName);
        mockTrade.Date.Returns(dateTime);
        mockTrade.AcquisitionDisposal.Returns(tradeType);
        mockTrade.Quantity.Returns(quantity);
        mockTrade.NetProceed.Returns(new WrappedMoney(baseCurrencyAmount));
        return mockTrade;
    }

    public static TradeTaxCalculation CreateTradeTaxCalculation(string assetName, DateTime dateTime, decimal quantity, decimal value, TradeType tradeType)
    {
        Trade trade = CreateMockTrade(assetName, dateTime, tradeType, quantity, value);
        return new TradeTaxCalculation([trade]);
    }
}
