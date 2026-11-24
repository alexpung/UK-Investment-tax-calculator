using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser.FreeTradeCsv;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;
using InvestmentTaxCalculator.Parser.Trading212Csv;

using System.Xml.Linq;

namespace UnitTest.Test.Reproduction;

/// <summary>
/// Withholding tax used to be stored as negative by the Interactive Brokers parser but positive by the Freetrade and
/// Trading 212 parsers, so mixing sources made the foreign tax paid total cancel out. Every source now stores it as negative.
/// </summary>
public class WithholdingTaxSignAcrossSourcesTest
{
    [Fact]
    public void WithholdingTaxFromEverySourceIsNegativeAndAddsUp()
    {
        XElement ibXml = XElement.Load(Path.Combine(AppContext.BaseDirectory, "Test", "resource", "TaxExample.xml"));
        IEnumerable<Dividend> ibWithholding = IBXmlDividendParser.ParseXml(ibXml);

        string freetradeCsv = "Type,Timestamp,Ticker,Title,ISIN,Quantity,Price per share,Total Amount,Currency,Buy / Sell,Dividend Pay Date,Dividend Amount Per Share,Dividend Withheld Tax Amount\n" +
            "DIVIDEND,2024-03-10 09:00:00,AAPL,Apple Inc.,US0378331005,,,25.00,USD,,2024-03-10,0.25,3.75";
        IEnumerable<Dividend> freetradeWithholding = new FreeTradeCsvParseController().ParseFile(freetradeCsv).Dividends;

        string trading212Csv = "Action,Time,ISIN,Ticker,Name,No. of shares,Price / share,Currency (Price / share),Exchange rate,Result (GBP),Total (GBP),Withholding tax,Currency (Withholding tax)\n" +
            "Dividend (Ordinary),2024-03-10 09:00:00,US0378331005,AAPL,Apple Inc.,10,0.25,USD,1.0,,25.00,2.00,GBP";
        IEnumerable<Dividend> trading212Withholding = new Trading212CsvParseController().ParseFile(trading212Csv).Dividends;

        List<Dividend> withholding = [.. ibWithholding.Concat(freetradeWithholding).Concat(trading212Withholding)
            .Where(dividend => dividend.DividendType == DividendType.WITHHOLDING)];

        withholding.Count.ShouldBe(3);
        withholding.ShouldAllBe(dividend => dividend.Proceed.BaseCurrencyAmount.Amount < 0);

        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.UnknownRegion,
            TaxYear = 2024,
            RelatedDividendsAndTaxes = withholding,
            RelatedInterestIncome = [],
        };
        summary.TotalForeignTaxPaid.ShouldBe(new WrappedMoney(-166.5m - 3.75m - 2m));
    }
}
