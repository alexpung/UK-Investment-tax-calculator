using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Xml.Linq;

namespace OptionTradeParserTests;

public class IBXmlOptionTradeParserTests
{
    [Fact]
    public void ParseXml_ValidOptionTradeXml_ShouldReturnCorrectOptionTrades()
    {
        string xmlContent = @"
                <FlexQueryResponse queryName='Tax' type='AF'>
                    <FlexStatements count='1'>
                        <FlexStatement accountId='TestAccount' fromDate='04-Jan-21' toDate='31-Dec-21' period='Last365CalendarDays' whenGenerated='01-Jan-22 00:00:00'>
                            <Trades>
                                <Order currency='USD' fxRateToBase='0.72153' assetCategory='OPT' symbol='GOOG  180119C00800000' 
                                       description='GOOG 19JAN18 800.0 C' underlyingSymbol='GOOG' multiplier='100' strike='800' expiry='19-Jan-18' 
                                       putCall='C' dateTime='19-Jan-18 16:20:00' quantity='-1' tradePrice='0' tradeMoney='0' proceeds='0' 
                                       taxes='0' ibCommission='0' ibCommissionCurrency='USD' openCloseIndicator='C' notes='C;Ex' buySell='SELL' 
                                       levelOfDetail='ORDER' />
                            </Trades>
                        </FlexStatement>
                    </FlexStatements>
                </FlexQueryResponse>";

        XElement document = XElement.Parse(xmlContent);

        IList<OptionTrade> result = IBXmlOptionTradeParser.ParseXml(document);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);

        var trade = result[0];
        trade.AssetName.ShouldBe("GOOG  180119C00800000");
        trade.Description.ShouldBe("GOOG 19JAN18 800.0 C");
        trade.Date.ShouldBe(new DateTime(2018, 1, 19, 16, 20, 0, DateTimeKind.Local));
        trade.Underlying.ShouldBe("GOOG");
        trade.StrikePrice.ShouldBe(new WrappedMoney(800, "usd"));
        trade.ExpiryDate.ShouldBe(new DateTime(2018, 1, 19, 0, 0, 0, DateTimeKind.Local));
        trade.PUTCALL.ShouldBe(PUTCALL.CALL);
        trade.TradeReason.ShouldBe(TradeReason.OwnerExeciseOption);
    }
}
