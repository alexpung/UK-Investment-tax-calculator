using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser;
using Moq;
using Shouldly;

namespace CapitalGainCalculator.Test;

public class FileParseControllerTest
{
    private readonly Trade _mockTradeObject = new Trade() { AssetName = "Test", Quantity = 100, BuySell = Enum.TradeType.BUY, Date = new DateTime(2022, 1, 1), GrossProceed = new DescribedMoney() { Amount = 100 } };
    private readonly Dividend _mockDividendObject = new Dividend() { AssetName = "Test2", Date = new DateTime(2022, 1, 1), DividendType = Enum.DividendType.WITHHOLDING, Proceed = new DescribedMoney() { Amount = 100 } };
    private readonly StockSplit _mockStockSplitObject = new StockSplit() { AssetName = "Test3", Date = new DateTime(2022, 1, 1), NumberAfterSplit = 1, NumberBeforeSplit = 2 };

    [Fact]
    public void TestReadingNoValidFileInFolder()
    {
        List<TaxEvent> mockResult = new() { _mockTradeObject, _mockDividendObject };
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(mockResult);
        mock.Setup(f => f.CheckFileValidity(It.IsAny<string>())).Returns(false);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object });
        IList<TaxEvent> result = fileParseController.ParseFolder(@".\Test\Resource");
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void TestReadingValidFileInFolder()
    {
        List<TaxEvent> mockResult = new() { _mockTradeObject, _mockDividendObject };
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(mockResult);
        mock.Setup(f => f.CheckFileValidity(It.IsAny<string>())).Returns(true);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object });
        IList<TaxEvent> result = fileParseController.ParseFolder(@".\Test\Resource");
        result.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData(true, true, false, true, 4)]
    [InlineData(false, true, false, true, 2)]
    [InlineData(false, true, true, true, 5)]
    [InlineData(false, false, false, true, 3)]
    [InlineData(false, false, true, true, 6)]
    public void TestReadingWithTwoParsers(bool mock1Call1, bool mock1Call2, bool mock2Call1, bool mock2Call2, int expectedCount)
    {
        List<TaxEvent> mockResult = new() { _mockTradeObject, _mockDividendObject };
        List<TaxEvent> mockResult2 = new() { _mockTradeObject, _mockDividendObject, _mockStockSplitObject };
        Mock<ITaxEventFileParser> mock = new();
        mock.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(mockResult);
        mock.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>())).Returns(mock1Call1).Returns(mock1Call2);
        Mock<ITaxEventFileParser> mock2 = new();
        mock2.Setup(f => f.ParseFile(It.IsAny<string>())).Returns(mockResult2);
        mock2.SetupSequence(f => f.CheckFileValidity(It.IsAny<string>())).Returns(mock2Call1).Returns(mock2Call2);

        FileParseController fileParseController = new(new List<ITaxEventFileParser>() { mock.Object, mock2.Object });
        IList<TaxEvent> result = fileParseController.ParseFolder(@".\Test\Resource");
        result.Count.ShouldBe(expectedCount);
    }
}
