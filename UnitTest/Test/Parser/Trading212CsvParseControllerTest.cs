using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
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
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,,,25.00,25.00,3.75,USD,,,,,D123456789,,,";

        var parser = new Trading212CsvParseController();
        var result = parser.ParseFile(csvData);

        result.Dividends.Count.ShouldBe(2); // Dividend + withholding tax

        var dividend = result.Dividends.First(d => d.DividendType == DividendType.DIVIDEND);
        dividend.AssetName.ShouldBe("AAPL");
        dividend.Proceed.Amount.Amount.ShouldBe(25.00m);
        dividend.CompanyLocation.ThreeDigitCode.ShouldBe("USA");
        dividend.Isin.ShouldBe("US0378331005");


        var withholding = result.Dividends.First(d => d.DividendType == DividendType.WITHHOLDING);
        withholding.AssetName.ShouldBe("AAPL");
        withholding.Proceed.Amount.Amount.ShouldBe(3.75m);
        withholding.Isin.ShouldBe("US0378331005");
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
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,,,,,25.00,25.00,3.75,USD,,,,,D789,,,\n" +
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
}
