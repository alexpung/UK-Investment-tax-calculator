﻿using Enumerations;

using Microsoft.AspNetCore.Components.Forms;

using Model;
using Model.TaxEvents;

using Moq;

using Parser;

using System.Text;

namespace UnitTest.Test.Parser;

public class FileParseControllerTest
{
    private readonly Trade _mockTradeObject = new()
    {
        AssetName = "Test",
        Quantity = 100,
        AcquisitionDisposal = TradeType.ACQUISITION,
        Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local),
        GrossProceed = new DescribedMoney() { Amount = new WrappedMoney(100, "GBP") }
    };
    private readonly Dividend _mockDividendObject = new()
    {
        AssetName = "Test2",
        Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local),
        DividendType = DividendType.WITHHOLDING,
        Proceed = new DescribedMoney() { Amount = new WrappedMoney(100, "GBP") }
    };
    private readonly StockSplit _mockStockSplitObject = new() { AssetName = "Test3", Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local), NumberAfterSplit = 1, NumberBeforeSplit = 2 };
    private readonly TaxEventLists _mockResult = new();
    private readonly TaxEventLists _mockResult2 = new();

    public FileParseControllerTest()
    {
        _mockResult.AddData(new List<TaxEvent> { _mockTradeObject, _mockDividendObject });
        _mockResult2.AddData(new List<TaxEvent> { _mockTradeObject, _mockDividendObject, _mockStockSplitObject });
    }

    private static IBrowserFile MockIBrowserFile(string textToMock)
    {
        Mock<IBrowserFile> mockFile = new();
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(new MemoryStream(Encoding.UTF8.GetBytes(textToMock)));
        return mockFile.Object;
    }

    [Fact]
    public async Task TestReadingInvalidFileShouldHaveNothing()
    {
        Mock<ITaxEventFileParser> mockFileParser = new();
        mockFileParser.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mockFileParser.Setup(f => f.CheckFileValidity(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mockFileParser.Object });
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Invalid text"));
        result.CorporateActions.Count.ShouldBe(0);
        result.Dividends.Count.ShouldBe(0);
        result.Trades.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TestReadingValidFile()
    {
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mock.Setup(f => f.CheckFileValidity(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object });
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Valid text"));
        result.CorporateActions.Count.ShouldBe(0);
        result.Dividends.Count.ShouldBe(1);
        result.Trades.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(true, true, false, true, 4)]
    [InlineData(false, true, false, true, 2)]
    [InlineData(false, true, true, true, 5)]
    [InlineData(false, false, false, true, 3)]
    [InlineData(false, false, true, true, 6)]
    public async Task TestReadingWithTwoParsers(bool mock1Call1, bool mock1Call2, bool mock2Call1, bool mock2Call2, int expectedCount)
    {
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult);
        mock.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>(), It.IsAny<string>())).Returns(mock1Call1).Returns(mock1Call2);
        Mock<ITaxEventFileParser> mock2 = new();
        mock2.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(_mockResult2);
        mock2.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>(), It.IsAny<string>())).Returns(mock2Call1).Returns(mock2Call2);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object, mock2.Object });
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Valid text"));
        result.AddData(await fileParseController.ReadFile(MockIBrowserFile("Valid text2")));
        int actualCount = result.CorporateActions.Count + result.Dividends.Count + result.Trades.Count;
        actualCount.ShouldBe(expectedCount);
    }
}
