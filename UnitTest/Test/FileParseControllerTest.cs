﻿using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser;
using Moq;
using NMoneys;
using Shouldly;

namespace CapitalGainCalculator.Test;

public class FileParseControllerTest
{
    private readonly Trade _mockTradeObject = new Trade() { AssetName = "Test", Quantity = 100, BuySell = Enum.TradeType.BUY, Date = new DateTime(2022, 1, 1), GrossProceed = new DescribedMoney() { Amount = new Money(100, CurrencyIsoCode.GBP) } };
    private readonly Dividend _mockDividendObject = new Dividend() { AssetName = "Test2", Date = new DateTime(2022, 1, 1), DividendType = Enum.DividendType.WITHHOLDING, Proceed = new DescribedMoney() { Amount = new Money(100, CurrencyIsoCode.GBP) } };
    private readonly StockSplit _mockStockSplitObject = new StockSplit() { AssetName = "Test3", Date = new DateTime(2022, 1, 1), NumberAfterSplit = 1, NumberBeforeSplit = 2 };
    private readonly TaxEventLists _mockResult = new();
    private readonly TaxEventLists _mockResult2 = new();

    public FileParseControllerTest()
    {
        _mockResult.AddData(new List<TaxEvent> { _mockTradeObject, _mockDividendObject });
        _mockResult2.AddData(new List<TaxEvent> { _mockTradeObject, _mockDividendObject, _mockStockSplitObject });
    }



    [Fact]
    public void TestReadingNoValidFileInFolderShouldHaveNothing()
    {
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mock.Setup(f => f.CheckFileValidity(It.IsAny<string>())).Returns(false);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object });
        TaxEventLists result = fileParseController.ParseFolder(@".\Test\Resource");
        result.CorporateActions.Count.ShouldBe(0);
        result.Dividends.Count.ShouldBe(0);
        result.Trades.Count.ShouldBe(0);
    }

    [Fact]
    public void TestReadingValidFileInFolder()
    {
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mock.Setup(f => f.CheckFileValidity(It.IsAny<string>())).Returns(true);
        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object });
        TaxEventLists result = fileParseController.ParseFolder(@".\Test\Resource");
        result.CorporateActions.Count.ShouldBe(0);
        result.Dividends.Count.ShouldBe(2);
        result.Trades.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(true, true, false, true, 4)]
    [InlineData(false, true, false, true, 2)]
    [InlineData(false, true, true, true, 5)]
    [InlineData(false, false, false, true, 3)]
    [InlineData(false, false, true, true, 6)]
    public void TestReadingWithTwoParsers(bool mock1Call1, bool mock1Call2, bool mock2Call1, bool mock2Call2, int expectedCount)
    {
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mock.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>())).Returns(mock1Call1).Returns(mock1Call2);
        Mock<ITaxEventFileParser> mock2 = new();
        mock2.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult2);
        mock2.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>())).Returns(mock2Call1).Returns(mock2Call2);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object, mock2.Object });
        TaxEventLists result = fileParseController.ParseFolder(@".\Test\Resource");
        int actualCount = result.CorporateActions.Count + result.Dividends.Count + result.Trades.Count;
        actualCount.ShouldBe(expectedCount);
    }
}
