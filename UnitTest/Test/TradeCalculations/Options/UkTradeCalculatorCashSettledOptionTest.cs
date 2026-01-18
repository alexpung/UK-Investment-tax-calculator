using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;

public class UkTradeCalculatorCashSettledOptionTest
{
    [Fact]
    public void CashSettledOption_ShouldUseCorrectStatementOfFundsAmount()
    {
        string xmlContent = @"
<FlexQueryResponse queryName=""Flex"" type=""AF"">
<FlexStatements count=""1"">
<FlexStatement accountId=""MockAccount"" fromDate=""01-Jan-23"" toDate=""31-Dec-23"" whenGenerated=""01-Jan-24 12:00:00"">
<StmtFunds>
<StatementOfFundsLine currency=""GBP"" fxRateToBase=""1"" date=""27-Feb-23"" assetCategory=""OPT"" symbol=""MOCK   230317P00022500"" activityDescription=""Sell -5 MOCK 17MAR23 22.5 P"" amount=""801.8086"" />
<StatementOfFundsLine currency=""GBP"" fxRateToBase=""1"" date=""02-Mar-23"" assetCategory=""OPT"" symbol=""MOCK   230317P00022500"" activityDescription=""Option Cash Settlement for: Assignment ( MOCK 17MAR23 22.5 P )"" amount=""-664.256"" />
</StmtFunds>
<Trades>
<Order assetCategory=""OPT"" currency=""USD"" fxRateToBase=""0.82897"" dateTime=""27-Feb-23 15:39:52"" quantity=""-5"" buySell=""SELL"" symbol=""MOCK   230317P00022500"" description=""MOCK 17MAR23 22.5 P"" multiplier=""100"" strike=""22.5"" putCall=""P"" underlyingSymbol=""MOCK"" expiry=""17-Mar-23"" proceeds=""975"" ibCommission=""-7.76525"" ibCommissionCurrency=""USD"" taxes=""0"" notes="""" isin="""" levelOfDetail=""ORDER"" />
<Order assetCategory=""OPT"" currency=""USD"" fxRateToBase=""0.83032"" dateTime=""02-Mar-23 16:20:00"" quantity=""4"" buySell=""BUY"" symbol=""MOCK   230317P00022500"" description=""MOCK 17MAR23 22.5 P"" multiplier=""100"" strike=""22.5"" putCall=""P"" underlyingSymbol=""MOCK"" expiry=""17-Mar-23"" proceeds=""0"" ibCommission=""0"" ibCommissionCurrency=""USD"" taxes=""0"" notes=""A;C"" isin="""" levelOfDetail=""ORDER"" />
</Trades>
</FlexStatement>
</FlexStatements>
</FlexQueryResponse>";

        var controller = new IBParseController(new AssetTypeToLoadSetting
        {
            LoadOptions = true,
            LoadFutures = false,
            LoadFx = false,
            LoadStocks = false,
            LoadDividends = false,
            LoadInterestIncome = false
        });
        var taxEventLists = controller.ParseFile(xmlContent);

        var allTrades = new List<TaxEvent>();
        allTrades.AddRange(taxEventLists.Trades);
        allTrades.AddRange(taxEventLists.OptionTrades);

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [.. allTrades, .. taxEventLists.CashSettlements.Cast<TaxEvent>()],
            out _
        );

        var optionCalc = result.OfType<OptionTradeTaxCalculation>().Single(c => c.AcquisitionDisposal == TradeType.DISPOSAL);
        var matches = optionCalc.MatchHistory.Where(m => m.MatchAcquisitionQty > 0).ToList();
        var match = matches.First(m => m.AdditionalInformation.Contains("cash settled"));

        match.BaseCurrencyMatchAllowableCost.Amount.ShouldBe(664.26m, 0.01m);
    }
}
