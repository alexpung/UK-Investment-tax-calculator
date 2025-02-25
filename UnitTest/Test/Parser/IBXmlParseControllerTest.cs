﻿using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

namespace UnitTest.Test.Parser;

public class IBXmlParseControllerTest
{
    private readonly string _taxExampleXml = File.ReadAllText(@".\Test\Resource\TaxExample.xml");
    private readonly string _invalidFileXml = File.ReadAllText(@".\Test\Resource\InvalidFile.xml");

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
}

