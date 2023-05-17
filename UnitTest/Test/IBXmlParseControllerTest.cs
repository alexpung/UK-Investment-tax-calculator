using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;
using System.Collections;

namespace CapitalGainCalculator.Test;

public class IBXmlParseControllerTest
{


    [Fact]
    public void TestCheckingInvalidIBXml()
    {
        string testFilePath = @".\Test\Resource\InvalidFile.xml";
        IBParseController iBParseController = new(new AssetTypeToLoadSetting());
        iBParseController.CheckFileValidity(testFilePath).ShouldBeFalse();
    }

    [Fact]
    public void TestCheckingValidIBXml()
    {
        string testFilePath = @".\Test\Resource\TaxExample.xml";
        IBParseController iBParseController = new(new AssetTypeToLoadSetting());
        iBParseController.CheckFileValidity(testFilePath).ShouldBeTrue();
    }

    [Theory]
    [ClassData(typeof(AssetTypeToLoadSettingTestData))]
    public void TestParseValidIBXml(AssetTypeToLoadSetting assetTypeToLoadSetting)
    {
        string testFilePath = @".\Test\Resource\TaxExample.xml";
        IBParseController iBParseController = new(assetTypeToLoadSetting);
        TaxEventLists results = iBParseController.ParseFile(testFilePath);
        if (assetTypeToLoadSetting.LoadStocks)
        {
            results.Trades.Count.ShouldBe(58);
            results.CorporateActions.Count.ShouldBe(2);
        }
        else
        {
            results.Trades.Count.ShouldBe(0);
            results.CorporateActions.Count.ShouldBe(0);
        }
        if (assetTypeToLoadSetting.LoadDividends)
        {
            results.Dividends.Count.ShouldBe(47);
        }
        else results.Dividends.Count.ShouldBe(0);
    }
}

public class AssetTypeToLoadSettingTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = true, LoadDividends = true, LoadFutures = true, LoadFx = true, LoadOptions = true } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = true, LoadDividends = false, LoadFutures = false, LoadFx = false, LoadOptions = false } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = false, LoadDividends = true, LoadFutures = false, LoadFx = false, LoadOptions = false } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = false, LoadDividends = false, LoadFutures = true, LoadFx = false, LoadOptions = false } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = false, LoadDividends = false, LoadFutures = false, LoadFx = true, LoadOptions = false } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = false, LoadDividends = false, LoadFutures = false, LoadFx = false, LoadOptions = true } };
        yield return new object[] { new AssetTypeToLoadSetting() { LoadStocks = false, LoadDividends = true, LoadFutures = true, LoadFx = true, LoadOptions = true } };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
