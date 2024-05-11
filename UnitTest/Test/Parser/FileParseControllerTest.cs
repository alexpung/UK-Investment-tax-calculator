using Enumerations;

using Microsoft.AspNetCore.Components.Forms;

using Model;
using Model.TaxEvents;

using NSubstitute;

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
        IBrowserFile mockFile = Substitute.For<IBrowserFile>();
        mockFile.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(textToMock)));
        return mockFile;
    }

    [Fact]
    public async Task TestReadingInvalidFileShouldHaveNothing()
    {
        ITaxEventFileParser mockFileParser = Substitute.For<ITaxEventFileParser>();
        mockFileParser.ParseFile(Arg.Any<string>()).Returns(_mockResult);
        mockFileParser.CheckFileValidity(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mockFileParser });
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Invalid text"));
        result.CorporateActions.Count.ShouldBe(0);
        result.Dividends.Count.ShouldBe(0);
        result.Trades.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TestReadingValidFile()
    {
        ITaxEventFileParser mock = Substitute.For<ITaxEventFileParser>();
        mock.ParseFile(Arg.Any<string>()).Returns(_mockResult);
        mock.CheckFileValidity(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock });
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
        ITaxEventFileParser mock = Substitute.For<ITaxEventFileParser>();
        mock.ParseFile(Arg.Any<string>()).Returns(_mockResult);
        mock.CheckFileValidity(Arg.Any<string>(), Arg.Any<string>()).Returns(mock1Call1, mock1Call2);
        ITaxEventFileParser mock2 = Substitute.For<ITaxEventFileParser>();
        mock2.ParseFile(Arg.Any<string>()).Returns(_mockResult2);
        mock2.CheckFileValidity(Arg.Any<string>(), Arg.Any<string>()).Returns(mock2Call1, mock2Call2);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock, mock2 });
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Valid text"));
        result.AddData(await fileParseController.ReadFile(MockIBrowserFile("Valid text2")));
        int actualCount = result.CorporateActions.Count + result.Dividends.Count + result.Trades.Count;
        actualCount.ShouldBe(expectedCount);
    }
}
