using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class ReturnOfCapitalTest
{
    [Fact]
    public void TestReturnOfCapitalReducesCostBasis()
    {
        // 1. Arrange: Buy shares
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1), 100, 1000m, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("IBM");
        buyTrade.MatchWithSection104(ukSection104);
        
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1000m));

        // 2. Act: Return of Capital event
        ReturnOfCapitalCorporateAction roc = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            Amount = new DescribedMoney(100, "GBP", 1.0m, "ROC"),
            Isin = "US123456789"
        };
        roc.ChangeSection104(ukSection104);

        // 3. Assert: Cost basis should be reduced (1000 - 100 = 900)
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(900m));
        
        ukSection104.Section104HistoryList.Count.ShouldBe(2);
        var history = ukSection104.Section104HistoryList[1];
        history.ValueChange.ShouldBe(new WrappedMoney(-100m));
        history.Explanation.ShouldContain("Return of capital");
    }

    [Fact]
    public void TestReturnOfCapitalVisibilityInDividends()
    {
        // Arrange
        Dividend rocDividend = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            DividendType = DividendType.RETURN_OF_CAPITAL,
            Proceed = new DescribedMoney(100, "GBP", 1.0m, "ROC"),
            Isin = "US123456789"
        };

        // Act & Assert
        // Should show in visible amount
        rocDividend.DividendReceived.ShouldBe(new WrappedMoney(100m));
        // Should NOT show in withholding
        rocDividend.WithholdingTaxPaid.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        
        // Arrange DividendSummary
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("US"),
            TaxYear = 2020,
            RelatedDividendsAndTaxes = [rocDividend],
            RelatedInterestIncome = []
        };

        // Assert: Summary should exclude it from taxable total
        summary.TotalTaxableDividend.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }
}
