using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace UnitTest.Test.Model;

public class DividendSummaryTest
{
    [Fact]
    public void TotalInterestIncome_IncludesEtfDividendIncome()
    {
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
            TaxYear = 2024,
            RelatedDividendsAndTaxes = [],
            RelatedInterestIncome =
            [
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(10m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 2), InterestType = InterestType.BOND, Amount = new DescribedMoney(20m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 3), InterestType = InterestType.ACCURREDINCOMEPROFIT, Amount = new DescribedMoney(30m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 4), InterestType = InterestType.ACCURREDINCOMELOSS, Amount = new DescribedMoney(-5m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 5), InterestType = InterestType.EXCESSREPORTABLEINCOME, Amount = new DescribedMoney(40m, "GBP", 1m) },
                new InterestIncome { AssetName = "ETF-1", Date = new DateTime(2025, 1, 6), InterestType = InterestType.ETFDIVIDEND, Amount = new DescribedMoney(50m, "GBP", 1m) }
            ]
        };

        summary.TotalInterestIncome.ShouldBe(new WrappedMoney(145m));
    }
}
