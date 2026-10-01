using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Xml.Linq;

namespace UnitTest.Test.Parser;

public class IBXmlInterestParseTest
{
    [Fact]
    public void ParseXml_DebitInterest_IsParsedAsInterestExpense()
    {
        XElement document = XElement.Parse(@"<StmtFunds>
            <StatementOfFundsLine levelOfDetail=""Currency"" activityCode=""DINT"" settleDate=""03-Feb-25"" amount=""-12.5"" currency=""USD"" fxRateToBase=""0.8"" activityDescription=""USD Debit Interest for Jan-2025"" />
            <StatementOfFundsLine levelOfDetail=""Currency"" activityCode=""CINT"" settleDate=""03-Feb-25"" amount=""3"" currency=""USD"" fxRateToBase=""0.8"" activityDescription=""USD Credit Interest for Jan-2025"" />
        </StmtFunds>");

        List<InterestIncome> result = IBXmlInterestIncomeParser.ParseXml(document);

        result.Count.ShouldBe(2);
        InterestIncome expense = result.Single(i => i.IsInterestExpense);
        expense.InterestType.ShouldBe(InterestType.INTERESTEXPENSE);
        expense.AssetName.ShouldBe("Broker interest");
        expense.Amount.Amount.Amount.ShouldBe(-12.5m);
        expense.Amount.BaseCurrencyAmount.Amount.ShouldBe(-10m);
        result.Single(i => !i.IsInterestExpense).InterestType.ShouldBe(InterestType.SAVINGS);
    }
}
