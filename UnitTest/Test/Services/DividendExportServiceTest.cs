using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

namespace UnitTest.Test.Services;

public class DividendExportServiceTest
{
    [Fact]
    public void Export_IncludesEtfDividendIncomeLine_AndAddsItToTotalInterestIncome()
    {
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
            TaxYear = 2024,
            RelatedDividendsAndTaxes = [],
            RelatedInterestIncome =
            [
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(5m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 2), InterestType = InterestType.ETFDIVIDEND, Amount = new DescribedMoney(25m, "GBP", 1m) }
            ]
        };

        DividendCalculationResult result = new();
        result.SetResult([summary]);
        DividendExportService service = new(result);

        string output = service.Export([2024]);

        output.ShouldContain("ETF dividend income: £25.00");
        output.ShouldContain("Total interest income: £30.00");
    }

    [Fact]
    public void Export_PrintsZeroForEtfDividendIncome_WhenNoEtfDividendEvents()
    {
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
            TaxYear = 2024,
            RelatedDividendsAndTaxes = [],
            RelatedInterestIncome =
            [
                new InterestIncome { AssetName = "Cash-1", Date = new DateTime(2025, 2, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(12m, "GBP", 1m) }
            ]
        };

        DividendCalculationResult result = new();
        result.SetResult([summary]);
        DividendExportService service = new(result);

        string output = service.Export([2024]);

        output.ShouldContain("ETF dividend income: £0.00");
        output.ShouldContain("Total interest income: £12.00");
    }

    [Fact]
    public void Export_ListsInterestExpensesSeparately_AndExcludesThemFromTotalInterestIncome()
    {
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
            TaxYear = 2024,
            RelatedDividendsAndTaxes = [],
            RelatedInterestIncome =
            [
                new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(10m, "GBP", 1m, "Credit interest") },
                new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 3), InterestType = InterestType.INTERESTEXPENSE, Amount = new DescribedMoney(-4m, "GBP", 1m, "Margin interest") }
            ]
        };

        DividendCalculationResult result = new();
        result.SetResult([summary]);
        DividendExportService service = new(result);

        string output = service.Export([2024]);

        output.ShouldContain("Total interest income: £10.00");
        output.ShouldContain("Interest expense (not deductible, excluded from totals, for reference only): -£4.00");
        int incomeHeader = output.IndexOf("Interest Transactions:");
        int expenseHeader = output.IndexOf("Interest Expense Transactions (not deductible, not included in any summary, for reference only):");
        expenseHeader.ShouldBeGreaterThan(incomeHeader);
        string incomeBlock = output[incomeHeader..expenseHeader];
        string expenseBlock = output[expenseHeader..];
        incomeBlock.ShouldContain("Credit interest");
        incomeBlock.ShouldNotContain("Margin interest");
        expenseBlock.ShouldContain("Margin interest");
        expenseBlock.ShouldNotContain("Credit interest");
    }
}
