using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class ExcessReportableIncomeTest
{
    [Fact]
    public void TestExcessReportableIncomeAdjustsSection104()
    {
        // 1. Setup a S104 pool with some holdings
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("ETF1", new DateTime(2023, 1, 1), 100, 1000m, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("ETF1");
        buyTrade.MatchWithSection104(ukSection104);

        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1000m));

        // 2. Add Excess Reportable Income
        // Accounting period end 2023-12-31, Distribution date 2024-06-30
        DateTime distributionDate = new(2024, 6, 30);
        decimal incomePerShare = 0.5m;
        decimal totalIncome = 100 * incomePerShare; // 50

        ExcessReportableIncome eri = new()
        {
            AssetName = "ETF1",
            Date = distributionDate,
            IncomeType = ExcessReportableIncomeType.DIVIDEND,
            Amount = new DescribedMoney(totalIncome, "GBP", 1, "ERI")
        };

        eri.ChangeSection104(ukSection104);

        // 3. Verify acquisition cost increased
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1050m));

        // 4. Verify history entry
        ukSection104.Section104HistoryList.Count.ShouldBe(2);
        ukSection104.Section104HistoryList[1].ValueChange.ShouldBe(new WrappedMoney(50m));
        ukSection104.Section104HistoryList[1].Explanation.ShouldContain("Excess reportable income");
    }
}
