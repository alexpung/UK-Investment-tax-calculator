using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using NSubstitute;

namespace UnitTest.Test.Model;

public class DividendCalculationResultTests
{
    [Fact]
    public void GetTotalDividend_ReturnsCorrectTotalDividend()
    {
        // Arrange
        DividendSummary dividendSummaryMock1 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock2 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock3 = Substitute.For<DividendSummary>();
        dividendSummaryMock1.TotalTaxableDividend.Returns(new WrappedMoney(100, "Gbp"));
        dividendSummaryMock2.TotalTaxableDividend.Returns(new WrappedMoney(200, "Gbp"));
        dividendSummaryMock3.TotalTaxableDividend.Returns(new WrappedMoney(300, "Gbp"));
        dividendSummaryMock1.TaxYear.Returns(2021);
        dividendSummaryMock2.TaxYear.Returns(2022);
        dividendSummaryMock3.TaxYear.Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1, dividendSummaryMock2, dividendSummaryMock3]);
        var yearFilter = new List<int> { 2021, 2023 };

        // Act
        var totalDividend = result.GetTotalDividend(yearFilter);

        // Assert
        totalDividend.ShouldBe(new WrappedMoney(400, "GBP"));
    }

    [Fact]
    public void GetForeignTaxPaid_ReturnsCorrectForeignTaxPaid()
    {
        // Arrange
        DividendSummary dividendSummaryMock1 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock2 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock3 = Substitute.For<DividendSummary>();
        dividendSummaryMock1.TotalForeignTaxPaid.Returns(new WrappedMoney(30, "Gbp"));
        dividendSummaryMock2.TotalForeignTaxPaid.Returns(new WrappedMoney(50, "Gbp"));
        dividendSummaryMock3.TotalForeignTaxPaid.Returns(new WrappedMoney(70, "Gbp"));
        dividendSummaryMock1.TaxYear.Returns(2021);
        dividendSummaryMock2.TaxYear.Returns(2022);
        dividendSummaryMock3.TaxYear.Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1, dividendSummaryMock2, dividendSummaryMock3]);
        var yearFilter = new List<int> { 2022, 2023 };

        // Act
        var foreignTaxPaid = result.GetForeignTaxPaid(yearFilter);

        // Assert
        foreignTaxPaid.ShouldBe(new WrappedMoney(120, "GBP"));
    }

    [Fact]
    public void TestNoYearSelected()
    {
        // Arrange
        DividendSummary dividendSummaryMock1 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock2 = Substitute.For<DividendSummary>();
        DividendSummary dividendSummaryMock3 = Substitute.For<DividendSummary>();
        dividendSummaryMock1.TotalForeignTaxPaid.Returns(new WrappedMoney(30, "Gbp"));
        dividendSummaryMock2.TotalForeignTaxPaid.Returns(new WrappedMoney(50, "Gbp"));
        dividendSummaryMock3.TotalForeignTaxPaid.Returns(new WrappedMoney(70, "Gbp"));
        dividendSummaryMock1.TaxYear.Returns(2021);
        dividendSummaryMock2.TaxYear.Returns(2022);
        dividendSummaryMock3.TaxYear.Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1, dividendSummaryMock2, dividendSummaryMock3]);
        var yearFilter = new List<int>();

        // Act
        var foreignTaxPaid = result.GetForeignTaxPaid(yearFilter);

        // Assert
        foreignTaxPaid.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }
    private static DividendSummary InterestSummary(int taxYear, params (InterestType type, decimal amount)[] incomes) => new()
    {
        CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
        TaxYear = taxYear,
        RelatedDividendsAndTaxes = [],
        RelatedInterestIncome = [.. incomes.Select((income, i) => new InterestIncome
        {
            AssetName = "ASSET",
            Date = new DateTime(taxYear, 6, 1).AddDays(i),
            InterestType = income.type,
            Amount = new DescribedMoney(income.amount, "GBP", 1m)
        })]
    };

    [Fact]
    public void InterestIncomeGetters_SumSelectedYearsByType()
    {
        DividendCalculationResult result = new();
        result.SetResult([
            InterestSummary(2023,
                (InterestType.SAVINGS, 10m), (InterestType.BOND, 20m), (InterestType.ACCURREDINCOMEPROFIT, 30m),
                (InterestType.ACCURREDINCOMELOSS, -5m), (InterestType.ETFDIVIDEND, 40m),
                (InterestType.EXCESSREPORTABLEINCOME, 50m), (InterestType.INTERESTEXPENSE, -7m)),
            InterestSummary(2024, (InterestType.SAVINGS, 100m), (InterestType.INTERESTEXPENSE, -3m)),
            InterestSummary(2025, (InterestType.SAVINGS, 1000m)),
        ]);
        List<int> years = [2023, 2024];

        result.GetSavingInterest(years).ShouldBe(new WrappedMoney(110m));
        result.GetBondInterest(years).ShouldBe(new WrappedMoney(20m));
        result.GetAccruedIncomeProfit(years).ShouldBe(new WrappedMoney(30m));
        result.GetAccruedIncomeLoss(years).ShouldBe(new WrappedMoney(-5m));
        result.GetEtfDividendIncome(years).ShouldBe(new WrappedMoney(40m));
        result.GetExcessReportableIncomeInterest(years).ShouldBe(new WrappedMoney(50m));
        // Interest paid is not deductible, so it is left out of the income total.
        result.GetTotalInterestIncome(years).ShouldBe(new WrappedMoney(245m));
        result.GetInterestExpense(years).ShouldBe(new WrappedMoney(-10m));
    }
}
