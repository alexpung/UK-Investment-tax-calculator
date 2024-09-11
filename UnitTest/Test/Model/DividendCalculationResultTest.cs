using InvestmentTaxCalculator.Model;

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
}

