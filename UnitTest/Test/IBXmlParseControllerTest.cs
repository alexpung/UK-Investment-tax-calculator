using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;

namespace CapitalGainCalculator.Test;

public class IBXmlParseControllerTest
{
    private readonly IBParseController _parseController;

    public IBXmlParseControllerTest()
    {
        AssetTypeToLoadSetting assetTypeToLoadSetting = new()
        {
            LoadStocks = true,
            LoadOptions = false,
            LoadFutures = false,
            LoadDividends = true,
            LoadFx = false
        };
        _parseController = new(assetTypeToLoadSetting);
    }

    [Fact]
    public void TestCheckingInvalidIBXml()
    {
        string testFilePath = @".\Test\Resource\InvalidFile.xml";
        _parseController.CheckFileValidity(testFilePath).ShouldBeFalse();
    }

    [Fact]
    public void TestCheckingValidIBXml()
    {
        string testFilePath = @".\Test\Resource\TaxExample.xml";
        _parseController.CheckFileValidity(testFilePath).ShouldBeTrue();
    }

    [Fact]
    public void TestParseValidIBXml()
    {
        string testFilePath = @".\Test\Resource\TaxExample.xml";
        TaxEventLists results = _parseController.ParseFile(testFilePath);
        results.Dividends.Count.ShouldBe(47);
        results.CorporateActions.Count.ShouldBe(2);
        results.Trades.Count.ShouldBe(58);
    }
}
