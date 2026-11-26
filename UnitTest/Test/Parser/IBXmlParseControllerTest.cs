using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Xml.Linq;

namespace UnitTest.Test.Parser;

public class IBXmlParseControllerTest
{
    private readonly string _taxExampleXml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Test", "resource", "TaxExample.xml"));
    private readonly string _invalidFileXml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Test", "resource", "InvalidFile.xml"));

    [Fact]
    public void TestCheckingInvalidIBXml()
    {
        IBParseController iBParseController = new(new AssetTypeToLoadSetting());
        iBParseController.CheckFileValidity(_invalidFileXml, "text/xml").ShouldBeFalse();
    }

    [Fact]
    public void TestCheckingValidIBXml()
    {
        IBParseController iBParseController = new(new AssetTypeToLoadSetting());
        iBParseController.CheckFileValidity(_taxExampleXml, "text/xml").ShouldBeTrue();
    }

    [Fact]
    public void TestRejectingInvalidFileType()
    {
        IBParseController iBParseController = new(new AssetTypeToLoadSetting());
        iBParseController.CheckFileValidity(_taxExampleXml, "text").ShouldBeFalse();
    }

    [Fact]
    public void TestBlankDividendIsinDoesNotDropRestOfFile()
    {
        XElement document = XElement.Parse(_taxExampleXml);
        XElement dividendRow = document.Descendants("CashTransaction")
            .First(row => row.Attribute("type")?.Value == "Dividends" && row.Attribute("levelOfDetail")?.Value == "DETAIL");
        dividendRow.SetAttributeValue("isin", "");
        TaxEventLists expected = new IBParseController(new AssetTypeToLoadSetting()).ParseFile(_taxExampleXml);

        TaxEventLists results = new IBParseController(new AssetTypeToLoadSetting()).ParseFile(document.ToString());

        results.Trades.Count.ShouldBeGreaterThan(0);
        results.Trades.Count.ShouldBe(expected.Trades.Count);
        results.CorporateActions.Count.ShouldBe(expected.CorporateActions.Count);
        results.InterestIncomes.Count.ShouldBe(expected.InterestIncomes.Count);
        results.Dividends.Count.ShouldBe(expected.Dividends.Count);
        results.Dividends.ShouldContain(dividend => dividend.Isin == string.Empty && dividend.CompanyLocation == CountryCode.UnknownRegion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestBlankOrMissingTradeIsinDoesNotDropRestOfFile(bool removeAttribute)
    {
        XElement document = XElement.Parse(_taxExampleXml);
        List<XElement> tradeRows = [.. document.Descendants("Order").Where(row => row.Attribute("isin") is not null)];
        tradeRows.ShouldNotBeEmpty();
        foreach (XElement row in tradeRows)
        {
            if (removeAttribute) row.Attribute("isin")!.Remove();
            else row.SetAttributeValue("isin", "");
        }
        TaxEventLists expected = new IBParseController(new AssetTypeToLoadSetting()).ParseFile(_taxExampleXml);

        TaxEventLists results = new IBParseController(new AssetTypeToLoadSetting()).ParseFile(document.ToString());

        results.Trades.Count.ShouldBeGreaterThan(0);
        results.Trades.Count.ShouldBe(expected.Trades.Count);
        results.Trades.ShouldAllBe(trade => trade.Isin == string.Empty);
        results.OptionTrades.Count.ShouldBe(expected.OptionTrades.Count);
        results.FutureContractTrades.Count.ShouldBe(expected.FutureContractTrades.Count);
        results.Dividends.Count.ShouldBe(expected.Dividends.Count);
    }

    [Fact]
    public void TestParseValidIBXml()
    {
        // TODO: Write expected result
        AssetTypeToLoadSetting assetTypeToLoadSetting = new();
        IBParseController iBParseController = new(assetTypeToLoadSetting);
        TaxEventLists results = iBParseController.ParseFile(_taxExampleXml);

    }

    [Fact]
    public void TestFundTradesLoadedWhenLoadFundsEnabled()
    {
        AssetTypeToLoadSetting assetTypeToLoadSetting = new()
        {
            LoadStocks = false,
            LoadOptions = false,
            LoadFutures = false,
            LoadFx = false,
            LoadDividends = false,
            LoadInterestIncome = false
        };
        IBParseController iBParseController = new(assetTypeToLoadSetting);
        TaxEventLists results = iBParseController.ParseFile(_taxExampleXml);
        results.Trades.Count.ShouldBe(2);
        results.Trades.ShouldAllBe(trade => trade.AssetType == AssetCategoryType.FUND);
    }

    [Fact]
    public void TestFundTradesNotLoadedWhenLoadFundsDisabled()
    {
        AssetTypeToLoadSetting assetTypeToLoadSetting = new() { LoadFunds = false };
        IBParseController iBParseController = new(assetTypeToLoadSetting);
        TaxEventLists results = iBParseController.ParseFile(_taxExampleXml);
        results.Trades.ShouldAllBe(trade => trade.AssetType != AssetCategoryType.FUND);
    }
}

