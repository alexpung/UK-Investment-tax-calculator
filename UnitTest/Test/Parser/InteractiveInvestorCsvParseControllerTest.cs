using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser.FreeTradeCsv;
using InvestmentTaxCalculator.Parser.InteractiveInvestorCsv;
using InvestmentTaxCalculator.Parser.Trading212Csv;

namespace UnitTest.Test.Parser;

public class InteractiveInvestorCsvParseControllerTest
{
    private const string SampleCsvHeader = "Date,Settlement Date,Symbol,Sedol,Quantity,Price,Description,Reference,Debit,Credit,Running Balance";

    [Fact]
    public void TestParseBuyTrade()
    {
        string csvData = SampleCsvHeader + "\n" +
            "03/04/2024,05/04/2024,VOD,BH4HKS3,1000,0.6954,VODAFONE GROUP PLC ORD USD0.20 20/21 : 1000 @ 0.6954,B240403001,703.35,,1296.65";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        var trade = result.Trades[0];
        trade.AssetName.ShouldBe("VOD");
        trade.Quantity.ShouldBe(1000);
        trade.AcquisitionDisposal.ShouldBe(TradeType.ACQUISITION);
        trade.GrossProceed.Amount.Amount.ShouldBe(703.35m);
        trade.Date.ShouldBe(new DateTime(2024, 4, 3));
    }

    [Fact]
    public void TestParseSellTrade()
    {
        string csvData = SampleCsvHeader + "\n" +
            "10/06/2024,12/06/2024,VOD,BH4HKS3,\"1,000\",0.7215,VODAFONE GROUP PLC ORD USD0.20 20/21 : 1000 @ 0.7215,S240610001,,\"713.55\",2010.20";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        var trade = result.Trades[0];
        trade.AssetName.ShouldBe("VOD");
        trade.Quantity.ShouldBe(1000);
        trade.AcquisitionDisposal.ShouldBe(TradeType.DISPOSAL);
        trade.GrossProceed.Amount.Amount.ShouldBe(713.55m);
    }

    [Fact]
    public void TestBuyingDividendNamedEtfIsATradeNotADividend()
    {
        // A purchase of a security with "DIVIDEND" in its name must not be classified as dividend income.
        string csvData = SampleCsvHeader + "\n" +
            "03/04/2024,05/04/2024,VHYL,BK8VW78,10,45.50,VANGUARD FTSE ALLWLD HIGH DIVIDEND YIELD : 10 @ 45.50,B240403002,462.95,,833.70";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Trades.Count.ShouldBe(1);
        result.Dividends.Count.ShouldBe(0);
    }

    [Fact]
    public void TestParseDividend()
    {
        string csvData = SampleCsvHeader + "\n" +
            "15/05/2024,15/05/2024,VOD,BH4HKS3,n/a,n/a,DIVIDEND PAYMENT VODAFONE GROUP PLC 4.50 USD/SHARE,DIV0515,,38.50,1335.15";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.Dividends.Count.ShouldBe(1);
        var dividend = result.Dividends[0];
        dividend.AssetName.ShouldBe("VOD");
        dividend.DividendType.ShouldBe(DividendType.DIVIDEND);
        dividend.Proceed.Amount.Amount.ShouldBe(38.50m);
        dividend.CompanyLocation.ShouldBe(InvestmentTaxCalculator.Model.CountryCode.UnknownRegion);
        dividend.Date.ShouldBe(new DateTime(2024, 5, 15));
    }

    [Fact]
    public void TestParseInterest()
    {
        string csvData = SampleCsvHeader + "\n" +
            "01/07/2024,01/07/2024,,,n/a,n/a,INTEREST ON CASH BALANCE,INT0701,,1.25,1336.40";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.InterestIncomes.Count.ShouldBe(1);
        var interest = result.InterestIncomes[0];
        interest.AssetName.ShouldBe("Broker interest");
        interest.Amount.Amount.Amount.ShouldBe(1.25m);
        interest.InterestType.ShouldBe(InterestType.SAVINGS);
    }

    [Fact]
    public void TestNonTaxRowsAreIgnored()
    {
        string csvData = SampleCsvHeader + "\n" +
            "01/04/2024,01/04/2024,,,n/a,n/a,DEBIT CARD PAYMENT,TOPUP01,,2000.00,2000.00\n" +
            "30/04/2024,30/04/2024,,,n/a,n/a,MONTHLY SERVICE PLAN FEE,FEE0430,4.99,,1995.01\n" +
            "02/05/2024,02/05/2024,,,n/a,n/a,CASH WITHDRAWAL,WDL0502,500.00,,1495.01";

        var parser = new InteractiveInvestorCsvParseController();
        var result = parser.ParseFile(csvData);

        result.GetTotalNumberOfEvents().ShouldBe(0);
    }

    [Fact]
    public void TestCheckFileValidity()
    {
        string csvData = SampleCsvHeader + "\n" +
            "03/04/2024,05/04/2024,VOD,BH4HKS3,1000,0.6954,VODAFONE GROUP PLC : 1000 @ 0.6954,B240403001,703.35,,1296.65";

        var parser = new InteractiveInvestorCsvParseController();
        parser.CheckFileValidity(csvData, "text/csv").ShouldBeTrue();
        // Browsers with Excel installed frequently report CSV files with an Excel MIME type
        parser.CheckFileValidity(csvData, "application/vnd.ms-excel").ShouldBeTrue();
        // Content type may carry parameters or differ in casing
        parser.CheckFileValidity(csvData, "text/csv; charset=utf-8").ShouldBeTrue();
        parser.CheckFileValidity(csvData, "Text/CSV").ShouldBeTrue();
        parser.CheckFileValidity(csvData, "text/xml").ShouldBeFalse();
    }

    [Fact]
    public void TestRejectsOtherBrokerCsvAndOtherParsersRejectIiCsv()
    {
        string iiCsv = SampleCsvHeader + "\n" +
            "03/04/2024,05/04/2024,VOD,BH4HKS3,1000,0.6954,VODAFONE GROUP PLC : 1000 @ 0.6954,B240403001,703.35,,1296.65";
        string trading212Csv = "Action,Time,ISIN,Ticker,Name,No. of shares,Total (GBP),Withholding tax\n" +
            "Market buy,2024-01-15 10:30:00,US0378331005,AAPL,Apple Inc.,10,-1500.00,";
        string freetradeCsv = "Type,Timestamp,Ticker,Title,ISIN,Quantity,Price per share,Total Amount,Currency,Buy / Sell,Dividend Pay Date,Dividend Amount Per Share,Dividend Withheld Tax Amount\n" +
            "ORDER,2024-01-15 10:30:00,AAPL,Apple Inc.,US0378331005,10,150.00,1500.00,USD,BUY,,,";

        var iiParser = new InteractiveInvestorCsvParseController();
        iiParser.CheckFileValidity(trading212Csv, "text/csv").ShouldBeFalse();
        iiParser.CheckFileValidity(freetradeCsv, "text/csv").ShouldBeFalse();

        new Trading212CsvParseController().CheckFileValidity(iiCsv, "text/csv").ShouldBeFalse();
        new FreeTradeCsvParseController().CheckFileValidity(iiCsv, "text/csv").ShouldBeFalse();
    }
}
