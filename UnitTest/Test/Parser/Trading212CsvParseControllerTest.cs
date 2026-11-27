using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.Trading212Csv;
using InvestmentTaxCalculator.Services;

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
    public void TestParseDividendIsGrossedUpByForeignWithholdingTax()
    {
        // Both Total and Price / share are net of withholding tax. 10 shares x 0.85 USD net = 8.50 USD credited as 6.80 GBP,
        // so 1.50 USD withheld = 1.50 x 6.80 / 8.50 = 1.20 GBP and the gross dividend is 8.00 GBP.
        string csvData = SampleCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,10,0.85,USD,Not available,,6.80,1.50,USD,,,,,,,,";

        var result = new Trading212CsvParseController().ParseFile(csvData);

        result.Dividends.Count.ShouldBe(2); // Dividend + withholding tax

        var dividend = result.Dividends.Single(d => d.DividendType == DividendType.DIVIDEND);
        dividend.AssetName.ShouldBe("AAPL");
        dividend.Proceed.BaseCurrencyAmount.Amount.ShouldBe(8.00m);
        dividend.CompanyLocation.ThreeDigitCode.ShouldBe("USA");
        dividend.Isin.ShouldBe("US0378331005");

        var withholding = result.Dividends.Single(d => d.DividendType == DividendType.WITHHOLDING);
        withholding.AssetName.ShouldBe("AAPL");
        withholding.Proceed.Amount.Amount.ShouldBe(-1.50m);
        withholding.Proceed.Amount.Currency.ShouldBe("USD");
        withholding.Proceed.FxRate.ShouldBe(0.8m);
        withholding.Proceed.BaseCurrencyAmount.Amount.ShouldBe(-1.20m);
        withholding.Isin.ShouldBe("US0378331005");
    }

    [Fact]
    public void TestReportedUsdWithholdingIsNotRecordedAsGbp()
    {
        // 0.17 USD withheld used to be recorded as 0.17 GBP. 10 x 0.096 USD = 0.96 USD net was credited as 0.75 GBP.
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Dividends paid by us corporations),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,10,0.096,USD,Not available,,,0.75,GBP,0.17,USD,";

        var dividends = new Trading212CsvParseController().ParseFile(csvData).Dividends;

        dividends.Single(d => d.DividendType == DividendType.WITHHOLDING).Proceed.BaseCurrencyAmount.Amount.ShouldBe(-0.1328125m);
        dividends.Single(d => d.DividendType == DividendType.DIVIDEND).Proceed.BaseCurrencyAmount.Amount.ShouldBe(0.8828125m);
    }

    [Fact]
    public void TestExchangeRateColumnIsNotUsedForWithholdingTax()
    {
        // Newer exports fill Exchange rate on dividend rows, quoted the reverse way from trade rows, so the implied rate is used instead.
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Ordinary),2025-12-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,10,0.85,USD,0.80000000,,,6.80,GBP,1.50,USD,";

        var withholding = new Trading212CsvParseController().ParseFile(csvData).Dividends.Single(d => d.DividendType == DividendType.WITHHOLDING);

        withholding.Proceed.BaseCurrencyAmount.Amount.ShouldBe(-1.20m);
    }

    [Fact]
    public void TestWithholdingTaxInGbpIsNotConverted()
    {
        string csvData = SampleCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,10,0.85,USD,Not available,,6.80,1.20,GBP,,,,,,,,";

        var dividends = new Trading212CsvParseController().ParseFile(csvData).Dividends;

        var withholding = dividends.Single(d => d.DividendType == DividendType.WITHHOLDING);
        withholding.Proceed.Amount.Currency.ShouldBe("GBP");
        withholding.Proceed.FxRate.ShouldBe(1m);
        withholding.Proceed.BaseCurrencyAmount.Amount.ShouldBe(-1.20m);
        dividends.Single(d => d.DividendType == DividendType.DIVIDEND).Proceed.BaseCurrencyAmount.Amount.ShouldBe(8.00m);
    }

    [Fact]
    public void TestWithholdingTaxInPenceIsConvertedToPounds()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Property income),2024-03-10 09:00:00,GB00B1XZS820,LAND,Land Securities,,,100,8.00,GBX,Not available,,,8.00,GBP,200,GBX,";

        var dividends = new Trading212CsvParseController().ParseFile(csvData).Dividends;

        dividends.Single(d => d.DividendType == DividendType.WITHHOLDING).Proceed.BaseCurrencyAmount.Amount.ShouldBe(-2.00m);
        dividends.Single(d => d.DividendType == DividendType.DIVIDEND).Proceed.BaseCurrencyAmount.Amount.ShouldBe(10.00m);
    }

    [Fact]
    public void TestZeroWithholdingTaxGivesNoWithholdingRecord()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Ordinary),2023-06-30 15:19:57,IE00BSKRJZ44,IS04,iShares USD Treasury Bond 20+yr (Dist),,,154.5055800000,0.05,USD,Not available,,,6.10,GBP,0.00,USD,";

        var dividend = new Trading212CsvParseController().ParseFile(csvData).Dividends.ShouldHaveSingleItem();

        dividend.DividendType.ShouldBe(DividendType.DIVIDEND);
        dividend.Proceed.BaseCurrencyAmount.Amount.ShouldBe(6.10m);
    }

    [Fact]
    public void TestForeignWithholdingTaxWithNoWayToConvertThrows()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,,,,,,,0.75,GBP,0.17,USD,";

        Should.Throw<InvalidDataException>(() => new Trading212CsvParseController().ParseFile(csvData)).Message.ShouldContain("USD");
    }

    // Header and Arbor Realty row from a public export (2025), with the totals in GBP. Some real rows have more trailing commas than the header.
    private const string PublicExportHeader = "Action,Time,ISIN,Ticker,Name,Notes,ID,No. of shares,Price / share,Currency (Price / share),Exchange rate,Result,Currency (Result),Total,Currency (Total),Withholding tax,Currency (Withholding tax),Charge amount,Currency (Charge amount),Deposit fee,Currency (Deposit fee),Stamp duty reserve tax,Currency (Stamp duty reserve tax),Currency conversion fee,Currency (Currency conversion fee),French transaction tax,Currency (French transaction tax)";

    [Fact]
    public void TestPublicExportRowWithExtraTrailingFieldsIsParsed()
    {
        string csvData = PublicExportHeader + "\n" +
            "Dividend (Dividend),2024-05-31 11:53:53,US0389231087,ABR,\"Arbor Realty Trust\",,,8.2102740000,0.37,USD,Not available,,,2.40,\"GBP\",0.53,USD,,,,,,,,,,,,";

        var dividends = new Trading212CsvParseController().ParseFile(csvData).Dividends;

        decimal expectedWithholdingGbp = 0.53m * 2.40m / (8.2102740000m * 0.37m);
        dividends.Single(d => d.DividendType == DividendType.WITHHOLDING).Proceed.BaseCurrencyAmount.Amount.ShouldBe(-expectedWithholdingGbp, 0.0000001m);
        dividends.Single(d => d.DividendType == DividendType.DIVIDEND).Proceed.BaseCurrencyAmount.Amount.ShouldBe(2.40m + expectedWithholdingGbp, 0.0000001m);
    }

    [Fact]
    public void TestManufacturedDividendIsPaymentInLieu()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Dividend manufactured payment),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,10,0.85,USD,Not available,,,6.80,GBP,1.50,USD,";

        var dividends = new Trading212CsvParseController().ParseFile(csvData).Dividends;

        dividends.Single(d => d.DividendType != DividendType.WITHHOLDING).DividendType.ShouldBe(DividendType.DIVIDEND_IN_LIEU);
    }

    [Fact]
    public void TestInterestDistributionIsSavingsIncome()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Interest),2024-03-10 09:00:00,IE00BSKRJZ44,IDTL,iShares USD Treasury Bond 20+yr (Dist),,,100,0.05,USD,Not available,,,3.95,GBP,0.00,USD,";

        var result = new Trading212CsvParseController().ParseFile(csvData);

        result.Dividends.ShouldBeEmpty();
        var interest = result.InterestIncomes.ShouldHaveSingleItem();
        interest.InterestType.ShouldBe(InterestType.ETFDIVIDEND);
        interest.Amount.BaseCurrencyAmount.Amount.ShouldBe(3.95m);
        interest.Isin.ShouldBe("IE00BSKRJZ44");
    }

    [Fact]
    public void TestReturnOfCapitalReducesCostInsteadOfBeingIncome()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend (Return of capital),2024-03-10 09:00:00,BMG162521014,BIP,Brookfield Infrastructure Partners,,,10,0.40,USD,Not available,,,3.20,GBP,0.00,USD,";

        var result = new Trading212CsvParseController().ParseFile(csvData);

        result.Dividends.ShouldBeEmpty();
        var returnOfCapital = result.CorporateActions.ShouldHaveSingleItem().ShouldBeOfType<ReturnOfCapitalCorporateAction>();
        returnOfCapital.AssetName.ShouldBe("BIP");
        returnOfCapital.Amount.BaseCurrencyAmount.Amount.ShouldBe(3.20m);
    }

    [Fact]
    public void TestUnsupportedActionsAreSkippedWithOneWarningEach()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Dividend adjustment,2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,,,,,,,0.20,GBP,,,\n" +
            "Stock split open,2024-06-10 09:00:00,US67066G1040,NVDA,NVIDIA,,,10,,,,,,0.00,GBP,,,\n" +
            "Stock split close,2024-06-10 09:00:00,US67066G1040,NVDA,NVIDIA,,,1,,,,,,0.00,GBP,,,\n" +
            "Deposit,2024-01-01 09:00:00,,,,,,,,,,,,1000.00,GBP,,,\n" +
            "Currency conversion,2024-01-02 09:00:00,,,,,,,,,,,,0.00,,,,";
        var toastService = new ToastService(Microsoft.Extensions.Logging.Abstractions.NullLogger<ToastService>.Instance);
        List<string> warnings = [];
        toastService.ShowToastTrigger += toast => warnings.Add(toast.Content);

        var result = new Trading212CsvParseController(toastService).ParseFile(csvData);

        result.AllEvents.ShouldBeEmpty();
        warnings.Count.ShouldBe(3);
        warnings.ShouldContain(w => w.Contains("Dividend adjustment") && w.Contains("(1 row)"));
        warnings.ShouldContain(w => w.Contains("Stock split open"));
        warnings.ShouldContain(w => w.Contains("Stock split close"));
    }

    [Fact]
    public void TestZeroTotalSellFromTakeoverIsNotADisposal()
    {
        string csvData = NewFormatCsvHeader + "\n" +
            "Market sell,2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,10,0,USD,1.27,,,0.00,GBP,,,";

        new Trading212CsvParseController().ParseFile(csvData).Trades.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("2024-04-05 23:30:00", 2024, 4, 6, 0, 30)] // BST: 5 April in UTC is 6 April in the UK, the next tax year
    [InlineData("2024-04-05 23:30:00+00:00", 2024, 4, 6, 0, 30)]
    [InlineData("2024-01-15 10:30:00", 2024, 1, 15, 10, 30)] // GMT: unchanged
    public void TestUtcTimesAreConvertedToUkTime(string time, int year, int month, int day, int hour, int minute)
    {
        string csvData = NewFormatCsvHeader + "\n" +
            $"Market buy,{time},US0378331005,AAPL,Apple Inc.,,T123,10,150.00,USD,1.27,,GBP,-1181.10,GBP,,,";

        new Trading212CsvParseController().ParseFile(csvData).Trades.ShouldHaveSingleItem().Date.ShouldBe(new DateTime(year, month, day, hour, minute, 0));
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
