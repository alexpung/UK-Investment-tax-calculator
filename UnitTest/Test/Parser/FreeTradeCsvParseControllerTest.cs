using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.FreeTradeCsv;

namespace UnitTest.Test.Parser;

public class FreeTradeCsvParseControllerTest
{
    private const string SampleCsvHeader = "Type,Timestamp,Ticker,Title,ISIN,Quantity,Price per share,Total Amount,Currency,Buy / Sell,Dividend Pay Date,Dividend Amount Per Share,Dividend Withheld Tax Amount";

    [Fact]
    public void TestParseOrder()
    {
        string csvData = SampleCsvHeader + "\n" +
            "ORDER,2024-01-15 10:30:00,AAPL,Apple Inc.,US0378331005,10,150.00,1500.00,USD,BUY,,,";

        var parser = new FreeTradeCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        var trade = result.Trades[0];
        trade.AssetName.ShouldBe("AAPL");
        trade.Quantity.ShouldBe(10);
        trade.AcquisitionDisposal.ShouldBe(TradeType.ACQUISITION);
        trade.Isin.ShouldBe("US0378331005");
    }

    [Fact]
    public void TestParseDividend()
    {
        string csvData = SampleCsvHeader + "\n" +
            "DIVIDEND,2024-03-10 09:00:00,AAPL,Apple Inc.,US0378331005,,,25.00,USD,,2024-03-10,0.25,3.75";

        var parser = new FreeTradeCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Dividends.Count.ShouldBe(2); // Dividend + withholding

        var dividend = result.Dividends.First(d => d.DividendType == DividendType.DIVIDEND);
        dividend.AssetName.ShouldBe("AAPL");
        dividend.Proceed.Amount.Amount.ShouldBe(25.00m);
        dividend.Isin.ShouldBe("US0378331005");
        dividend.CompanyLocation.CountryName.ShouldBe("United States of America");

        var withholding = result.Dividends.First(d => d.DividendType == DividendType.WITHHOLDING);
        withholding.Proceed.Amount.Amount.ShouldBe(3.75m);
        withholding.Isin.ShouldBe("US0378331005");
    }

    [Fact]
    public void TestParseInterest()
    {
        string csvData = SampleCsvHeader + "\n" +
            "INTEREST_FROM_CASH,2024-04-01 00:00:00,,,,,,5.50,GBP,,,";

        var parser = new FreeTradeCsvParseController();
        var result = parser.ParseFile(csvData);

        result.InterestIncomes.Count.ShouldBe(1);
        var interest = result.InterestIncomes[0];
        interest.AssetName.ShouldBe("Broker interest");
        interest.Amount.Amount.Amount.ShouldBe(5.50m);
    }

    [Fact]
    public void TestDividendColumnsMustBeMappedOnlyWhenFileHasDividends()
    {
        const string renamedHeader = "Type,Timestamp,Ticker,Title,ISIN,Quantity,Price per share,Total Amount,Currency,Buy / Sell,Paid on,Dividend Amount Per Share,Tax withheld";
        string ordersOnly = renamedHeader + "\n" +
            "ORDER,2024-01-15T10:30:00.000Z,AAPL,Apple Inc.,US0378331005,10,150.00,1500.00,USD,BUY,,,";
        string withDividend = ordersOnly + "\n" +
            "DIVIDEND,2024-03-10T09:00:00.000Z,AAPL,Apple Inc.,US0378331005,,,25.00,USD,,2024-03-10,0.25,3.75";
        var parser = new FreeTradeCsvParseController();
        CsvColumnMap map = CsvColumnMapper.AutoMap(parser.Columns, CsvColumnMapper.ReadHeaders(ordersOnly));

        parser.ValidateColumnMap(ordersOnly, map).ShouldBeEmpty();
        parser.ValidateColumnMap(withDividend, map).Count.ShouldBe(2);
        Should.Throw<InvalidDataException>(() => parser.ParseFile(withDividend, map));

        map.Set("Dividend Pay Date", "Paid on");
        map.Set("Dividend Withheld Tax Amount", "Tax withheld");
        parser.ValidateColumnMap(withDividend, map).ShouldBeEmpty();
        parser.ParseFile(withDividend, map).Dividends.Count.ShouldBe(2);
    }
}
