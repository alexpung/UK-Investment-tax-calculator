using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;

namespace CapitalGainCalculator.Test;

public class IBXmlParseControllerTest
{
    private readonly IBParseController _parseController = new(new AssetTypeToLoadSetting());

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
        results.Dividends.Count.ShouldBe(93);
        results.CorporateActions.Count.ShouldBe(2);
        results.Trades.Count.ShouldBe(80);
    }
}
