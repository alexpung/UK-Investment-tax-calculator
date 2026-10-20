using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.FreeTradeCsv;
using InvestmentTaxCalculator.Parser.Trading212Csv;

using Microsoft.AspNetCore.Components.Forms;

using NSubstitute;

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
    private readonly StockSplit _mockStockSplitObject = new() { AssetName = "Test3", Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local), SplitTo = 1, SplitFrom = 2 };
    private readonly TaxEventLists _mockResult = new();
    private readonly TaxEventLists _mockResult2 = new();

    public FileParseControllerTest()
    {
        _mockResult.AddData([_mockTradeObject, _mockDividendObject]);
        _mockResult2.AddData([_mockTradeObject, _mockDividendObject, _mockStockSplitObject]);
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
        FileParseController fileParseController = new([mockFileParser]);
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
        FileParseController fileParseController = new([mock]);
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

        FileParseController fileParseController = new([mock, mock2]);
        TaxEventLists result = await fileParseController.ReadFile(MockIBrowserFile("Valid text"));
        result.AddData(await fileParseController.ReadFile(MockIBrowserFile("Valid text2")));
        int actualCount = result.CorporateActions.Count + result.Dividends.Count + result.Trades.Count;
        actualCount.ShouldBe(expectedCount);
    }

    private static IBrowserFile MockCsvFile(string textToMock, string name = "statement.csv")
    {
        IBrowserFile mockFile = MockIBrowserFile(textToMock);
        mockFile.Name.Returns(name);
        mockFile.ContentType.Returns("text/csv");
        return mockFile;
    }

    private const string RenamedTrading212Csv = "Type of action,Executed at,ISIN,Symbol,Shares,Amount,Amount currency\n" +
        "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,10,-1181.10,GBP";

    private static CsvColumnMappingChoice MapRenamedTrading212(CsvColumnMappingRequest request)
    {
        CsvColumnMappingChoice candidate = request.Candidates.Single(c => c.Parser.FormatName == "Trading 212");
        CsvColumnMap map = candidate.ColumnMap.Clone();
        map.Set("Action", "Type of action");
        map.Set("Time", "Executed at");
        map.Set("Ticker", "Symbol");
        map.Set("No. of shares", "Shares");
        map.Set("Total (GBP)", "Amount");
        map.Set("Currency (Total)", "Amount currency");
        return candidate with { ColumnMap = map };
    }

    [Fact]
    public async Task TestUnrecognisedCsvAsksForColumnMapping()
    {
        FileParseController controller = new([new Trading212CsvParseController(), new FreeTradeCsvParseController()]);
        CsvColumnMappingRequest? received = null;

        TaxEventLists result = await controller.ReadFile(MockCsvFile(RenamedTrading212Csv), request =>
        {
            received = request;
            return Task.FromResult<CsvColumnMappingChoice?>(MapRenamedTrading212(request));
        });

        received.ShouldNotBeNull();
        received.FileName.ShouldBe("statement.csv");
        received.Headers.ShouldBe(["Type of action", "Executed at", "ISIN", "Symbol", "Shares", "Amount", "Amount currency"]);
        received.SampleRows.Count.ShouldBe(1);
        received.Candidates.Count.ShouldBe(2);
        // ISIN is found automatically using the known header name.
        received.Candidates[0].ColumnMap.TryGetHeader("ISIN", out string? isinHeader).ShouldBeTrue();
        isinHeader.ShouldBe("ISIN");
        result.Trades.Count.ShouldBe(1);
        result.Trades[0].GrossProceed.Amount.Amount.ShouldBe(1181.10m);
    }

    [Fact]
    public async Task TestConfirmedColumnMappingIsReusedForFilesWithSameLayout()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        int requestCount = 0;
        Task<CsvColumnMappingChoice?> Request(CsvColumnMappingRequest request)
        {
            requestCount++;
            return Task.FromResult<CsvColumnMappingChoice?>(MapRenamedTrading212(request));
        }

        await controller.ReadFile(MockCsvFile(RenamedTrading212Csv), Request);
        TaxEventLists second = await controller.ReadFile(MockCsvFile(RenamedTrading212Csv, "second.csv"), Request);

        requestCount.ShouldBe(1);
        second.Trades.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TestCancelledColumnMappingImportsNothing()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        TaxEventLists result = await controller.ReadFile(MockCsvFile(RenamedTrading212Csv), _ => Task.FromResult<CsvColumnMappingChoice?>(null));
        result.GetTotalNumberOfEvents().ShouldBe(0);
    }

    [Fact]
    public async Task TestIncompleteColumnMappingThrows()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        await Should.ThrowAsync<InvalidDataException>(() => controller.ReadFile(MockCsvFile(RenamedTrading212Csv),
            request => Task.FromResult<CsvColumnMappingChoice?>(request.Candidates[0])));
    }

    [Fact]
    public async Task TestNonCsvFileDoesNotAskForColumnMapping()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        bool asked = false;
        IBrowserFile file = MockIBrowserFile(RenamedTrading212Csv);
        file.Name.Returns("statement.xml");
        file.ContentType.Returns("text/xml");
        await controller.ReadFile(file, _ => { asked = true; return Task.FromResult<CsvColumnMappingChoice?>(null); });
        asked.ShouldBeFalse();
    }

    [Fact]
    public async Task TestExcelWorkbookDoesNotAskForColumnMapping()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        bool asked = false;
        IBrowserFile file = MockIBrowserFile("binary workbook content");
        file.Name.Returns("statement.xls");
        file.ContentType.Returns("application/vnd.ms-excel");
        await controller.ReadFile(file, _ => { asked = true; return Task.FromResult<CsvColumnMappingChoice?>(null); });
        asked.ShouldBeFalse();
    }

    [Fact]
    public async Task TestMappingRequestValidatesChoices()
    {
        FileParseController controller = new([new Trading212CsvParseController()]);
        IReadOnlyList<string>? suggestedProblems = null;
        IReadOnlyList<string>? withoutCurrencyProblems = null;
        IReadOnlyList<string>? mappedProblems = null;
        await controller.ReadFile(MockCsvFile(RenamedTrading212Csv), request =>
        {
            suggestedProblems = request.Validate(request.Candidates[0]);
            CsvColumnMappingChoice mapped = MapRenamedTrading212(request);
            mappedProblems = request.Validate(mapped);
            CsvColumnMap withoutCurrency = mapped.ColumnMap.Clone();
            withoutCurrency.Set("Currency (Total)", null);
            withoutCurrencyProblems = request.Validate(mapped with { ColumnMap = withoutCurrency });
            return Task.FromResult<CsvColumnMappingChoice?>(null);
        });
        suggestedProblems.ShouldNotBeNull().ShouldNotBeEmpty();
        mappedProblems.ShouldNotBeNull().ShouldBeEmpty();
        // "Amount" is not the legacy "Total (GBP)" header, so the currency column must be mapped too.
        withoutCurrencyProblems.ShouldNotBeNull().ShouldHaveSingleItem().ShouldContain("Currency (Total)");
    }
}
