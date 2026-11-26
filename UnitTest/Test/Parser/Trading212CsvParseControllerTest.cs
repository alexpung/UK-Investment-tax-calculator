using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.Trading212Csv;

namespace UnitTest.Test.Parser;

public class Trading212CsvParseControllerTest
{
    private const string SampleCsvHeader = "Action,Time,ISIN,Ticker,Name,No. of shares,Price / share,Currency (Price / share),Exchange rate,Result (GBP),Total (GBP),Withholding tax,Currency (Withholding tax),Stamp duty reserve tax,Currency conversion fee,French transaction tax,Notes,ID,Currency conversion from,Currency conversion to,Currency conversion fee (GBP)";

    [Fact]
    public void TestParseBuyOrder()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123456789,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        var trade = result.Trades[0];
        trade.AssetName.ShouldBe("AAPL");
        trade.Quantity.ShouldBe(10);
        trade.AcquisitionDisposal.ShouldBe(TradeType.ACQUISITION);
        trade.GrossProceed.Amount.Amount.ShouldBe(1181.10m);
        trade.Date.Year.ShouldBe(2024);
        trade.Date.Month.ShouldBe(1);
        trade.Date.Day.ShouldBe(15);
        trade.Isin.ShouldBe("US0378331005");
    }

    [Fact]
    public void TestParseSellOrder()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market sell,2024-02-20 14:45:00,US0378331005,AAPL,Apple Inc.,5,160.00,USD,1.25,800.00,640.00,,,,,,,T987654321,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        var trade = result.Trades[0];
        trade.AssetName.ShouldBe("AAPL");
        trade.Quantity.ShouldBe(5);
        trade.AcquisitionDisposal.ShouldBe(TradeType.DISPOSAL);
        trade.GrossProceed.Amount.Amount.ShouldBe(640.00m);
        trade.Isin.ShouldBe("US0378331005");
        trade.Expenses.Count.ShouldBe(0); // No fees
    }

    [Fact]
    public void TestParseDividend()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,100,0.25,USD,1.25,17.00,17.00,3.75,USD,,,,,D123456789,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.Dividends.Count.ShouldBe(2); // Dividend + withholding tax

        var dividend = result.Dividends.First(d => d.DividendType == DividendType.DIVIDEND);
        dividend.AssetName.ShouldBe("AAPL");
        dividend.Proceed.Amount.Amount.ShouldBe(17.00m);
        dividend.CompanyLocation.ThreeDigitCode.ShouldBe("USA");
        dividend.Isin.ShouldBe("US0378331005");


        var withholding = result.Dividends.First(d => d.DividendType == DividendType.WITHHOLDING);
        withholding.AssetName.ShouldBe("AAPL");
        withholding.Proceed.Amount.Amount.ShouldBe(-3.75m);
        withholding.Proceed.Amount.Currency.ShouldBe("USD");
        withholding.Proceed.BaseCurrencyAmount.Amount.ShouldBe(-3.00m);
        withholding.Isin.ShouldBe("US0378331005");
    }

    [Fact]
    public void TestWithholdingTaxInGbpIsNotConverted()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,100,0.25,USD,1.25,17.00,17.00,3.00,GBP,,,,,D123456789,,,";

        var withholding = new Trading212CsvParseController().ParseFile(csvData).Dividends.Single(d => d.DividendType == DividendType.WITHHOLDING);

        withholding.Proceed.Amount.Currency.ShouldBe("GBP");
        withholding.Proceed.FxRate.ShouldBe(1m);
        withholding.Proceed.BaseCurrencyAmount.Amount.ShouldBe(-3.00m);
    }

    [Fact]
    public void TestForeignWithholdingTaxWithoutExchangeRateUsesImpliedRate()
    {
        // Reported case: 0.17 USD withheld used to be recorded as 0.17 GBP. Exchange rate is "Not available" on dividend rows,
        // so the rate is implied: 10 x 0.113 USD = 1.13 USD gross, less 0.17 USD = 0.96 USD net, received as 0.75 GBP.
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,D789,10,0.113,USD,Not available,,GBP,0.75,GBP,0.17,USD,";

        var withholding = new Trading212CsvParseController().ParseFile(csvData).Dividends.Single(d => d.DividendType == DividendType.WITHHOLDING);

        withholding.Proceed.Amount.Amount.ShouldBe(-0.17m);
        withholding.Proceed.Amount.Currency.ShouldBe("USD");
        Math.Round(withholding.Proceed.BaseCurrencyAmount.Amount, 4).ShouldBe(Math.Round(-0.17m * 0.75m / 0.96m, 4));
    }

    [Fact]
    public void TestForeignWithholdingTaxWithNoWayToConvertThrows()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,D789,,,,,,GBP,0.75,GBP,0.17,USD,";

        Should.Throw<InvalidDataException>(() => new Trading212CsvParseController().ParseFile(csvData)).Message.ShouldContain("USD");
    }

    [Fact]
    public void TestDividendWithBlankIsinGivesUnknownRegion()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123456789,,,\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,,AAPL,Apple Inc.,100,0.25,USD,1.25,17.00,17.00,3.75,USD,,,,,D123456789,,,";

        var result = new Trading212CsvParseController().ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        result.Dividends.Count.ShouldBe(2);
        result.Dividends.ShouldAllBe(d => d.CompanyLocation == CountryCode.UnknownRegion && d.Isin == string.Empty);
    }

    [Fact]
    public void TestTradeWithBlankIsinIsParsed()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123456789,,,";

        var result = new Trading212CsvParseController().ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        result.Trades[0].AssetName.ShouldBe("AAPL");
        result.Trades[0].Isin.ShouldBe(string.Empty);
    }

    [Fact]
    public void TestParseInterest()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Interest on cash,2024-04-01 00:00:00,,,,,,,,,5.50,,,,,,I123456789,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.InterestIncomes.Count.ShouldBe(1);
        var interest = result.InterestIncomes[0];
        interest.AssetName.ShouldBe("Broker interest");
        interest.Amount.Amount.Amount.ShouldBe(5.50m);
        interest.InterestType.ShouldBe(InterestType.SAVINGS);
    }

    [Fact]
    public void TestParseMultipleTransactions()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123,,,\n" +
            "Market sell,2024-02-20 14:45:00,US0378331005,AAPL,Apple Inc.,5,160.00,USD,1.25,800.00,640.00,,,,,,,T456,,,\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,100,0.25,USD,1.25,17.00,17.00,3.75,USD,,,,,D789,,,\n" +
            "Interest on cash,2024-04-01 00:00:00,,,,,,,,,5.50,,,,,,I012,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(2);
        result.Dividends.Count.ShouldBe(2); // Dividend + withholding
        result.InterestIncomes.Count.ShouldBe(1);
    }

    [Fact]
    public void TestCheckFileValidity_ValidCsv()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123,,,";

        var parser = new Trading212CsvParseController();
        bool isValid = parser.CheckFileValidity(csvData, "text/csv");

        isValid.ShouldBeTrue();
    }

    [Fact]
    public void TestCheckFileValidity_WrongContentType()
    {
        string csvData = SampleCsvHeader;

        var parser = new Trading212CsvParseController();
        bool isValid = parser.CheckFileValidity(csvData, "application/json");

        isValid.ShouldBeFalse();
    }

    // Header layout of newer exports (issue #270): "Time (UTC)" and a plain "Total" with a separate currency column.
    private const string NewFormatCsvHeader = "Action,Time (UTC),ISIN,Ticker,Name,Notes,ID,No. of shares,Price / share,Currency (Price / share),Exchange rate,Result,Currency (Result),Total,Currency (Total),Withholding tax,Currency (Withholding tax),Merchant name";

    [Fact]
    public void TestNewFormatHeadersAreRecognisedAndParsed()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,,T123,10,150.00,USD,1.27,,GBP,-1181.10,GBP,,,\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,D789,100,0.25,USD,Not available,,GBP,17.00,GBP,3.75,USD,\n" +
            "Interest on cash,2024-04-01 00:00:00,,,,,I012,,,,,,,5.50,GBP,,,";

        var parser = new Trading212CsvParseController();
        parser.CheckFileValidity(csvData, "text/csv").ShouldBeTrue();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        result.Trades[0].GrossProceed.Amount.Amount.ShouldBe(1181.10m);
        result.Trades[0].Date.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0));
        result.Dividends.Count.ShouldBe(2);
        result.InterestIncomes.Count.ShouldBe(1);
        result.InterestIncomes[0].Amount.Amount.Amount.ShouldBe(5.50m);
    }

    [Fact]
    public void TestNonGbpTotalThrows()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,,T123,10,150.00,USD,1.27,,USD,-1500.00,USD,,,";

        Should.Throw<InvalidDataException>(() => new Trading212CsvParseController().ParseFile(csvData)).Message.ShouldContain("USD");
    }

    [Fact]
    public void TestParseWithUserColumnMap()
    {
        string csvData = "Type of action,Executed at,ISIN,Symbol,Shares,Amount,Amount currency\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,10,-1181.10,GBP";
        var parser = new Trading212CsvParseController();
        parser.CheckFileValidity(csvData, "text/csv").ShouldBeFalse();

        var map = new CsvColumnMap();
        map.Set("Action", "Type of action");
        map.Set("Time", "Executed at");
        map.Set("ISIN", "ISIN");
        map.Set("Ticker", "Symbol");
        map.Set("No. of shares", "Shares");
        map.Set("Total (GBP)", "Amount");
        map.Set("Currency (Total)", "Amount currency");
        var result = parser.ParseFile(csvData, map);

        result.Trades.Count.ShouldBe(1);
        result.Trades[0].AssetName.ShouldBe("AAPL");
        result.Trades[0].Quantity.ShouldBe(10);
        result.Trades[0].GrossProceed.Amount.Amount.ShouldBe(1181.10m);
    }

    [Fact]
    public void TestPlainTotalWithoutCurrencyMappingIsRejected()
    {
        string csvData = "Action,Time,ISIN,Ticker,No. of shares,Amount,Amount currency\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,10,-1500.00,USD";
        var parser = new Trading212CsvParseController();
        var map = CsvColumnMapper.AutoMap(parser.Columns, CsvColumnMapper.ReadHeaders(csvData));
        map.Set("Total (GBP)", "Amount");

        parser.ValidateColumnMap(csvData, map).ShouldHaveSingleItem().ShouldContain("Currency (Total)");
        Should.Throw<InvalidDataException>(() => parser.ParseFile(csvData, map));

        map.Set("Currency (Total)", "Amount currency");
        parser.ValidateColumnMap(csvData, map).ShouldBeEmpty();
        Should.Throw<InvalidDataException>(() => parser.ParseFile(csvData, map)).Message.ShouldContain("USD");
    }

    [Fact]
    public void TestLegacyTotalGbpHeaderNeedsNoCurrencyMapping()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,150.00,USD,1.27,-1500.00,-1181.10,,,5.91,2.36,,,T123,,,";
        var parser = new Trading212CsvParseController();
        var map = CsvColumnMapper.AutoMap(parser.Columns, CsvColumnMapper.ReadHeaders(csvData));

        parser.ValidateColumnMap(csvData, map).ShouldBeEmpty();
    }
}
