using Model;
using Moq;
using NMoneys;

namespace UnitTest.Test.Model;

public class DividendCalculationResultTests
{
    [Fact]
    public void GetTotalDividend_ReturnsCorrectTotalDividend()
    {
        // Arrange
        Mock<DividendSummary> dividendSummaryMock1 = new();
        Mock<DividendSummary> dividendSummaryMock2 = new();
        Mock<DividendSummary> dividendSummaryMock3 = new();
        dividendSummaryMock1.Setup(i => i.TotalTaxableDividend).Returns(new Money(100, Currency.Gbp));
        dividendSummaryMock2.Setup(i => i.TotalTaxableDividend).Returns(new Money(200, Currency.Gbp));
        dividendSummaryMock3.Setup(i => i.TotalTaxableDividend).Returns(new Money(300, Currency.Gbp));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult(new List<DividendSummary>() { dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object });
        var yearFilter = new List<int> { 2021, 2023 };

        // Act
        var totalDividend = result.GetTotalDividend(yearFilter);

        // Assert
        totalDividend.ShouldBe(new Money(400, Currency.Gbp));
    }

    [Fact]
    public void GetForeignTaxPaid_ReturnsCorrectForeignTaxPaid()
    {
        // Arrange
        Mock<DividendSummary> dividendSummaryMock1 = new();
        Mock<DividendSummary> dividendSummaryMock2 = new();
        Mock<DividendSummary> dividendSummaryMock3 = new();
        dividendSummaryMock1.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(30, Currency.Gbp));
        dividendSummaryMock2.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(50, Currency.Gbp));
        dividendSummaryMock3.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(70, Currency.Gbp));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult(new List<DividendSummary>() { dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object });
        var yearFilter = new List<int> { 2022, 2023 };

        // Act
        var foreignTaxPaid = result.GetForeignTaxPaid(yearFilter);

        // Assert
        foreignTaxPaid.ShouldBe(new Money(120, Currency.Gbp));
    }

    [Fact]
    public void TestNoYearSelected()
    {
        // Arrange
        Mock<DividendSummary> dividendSummaryMock1 = new();
        Mock<DividendSummary> dividendSummaryMock2 = new();
        Mock<DividendSummary> dividendSummaryMock3 = new();
        dividendSummaryMock1.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(30, Currency.Gbp));
        dividendSummaryMock2.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(50, Currency.Gbp));
        dividendSummaryMock3.Setup(i => i.TotalForeignTaxPaid).Returns(new Money(70, Currency.Gbp));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult(new List<DividendSummary>() { dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object });
        var yearFilter = new List<int>();

        // Act
        var foreignTaxPaid = result.GetForeignTaxPaid(yearFilter);

        // Assert
        foreignTaxPaid.ShouldBe(BaseCurrencyMoney.BaseCurrencyZero);
    }
}

