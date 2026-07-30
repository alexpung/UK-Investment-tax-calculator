using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

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

