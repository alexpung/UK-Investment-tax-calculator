using Model;
using Moq;

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
        dividendSummaryMock1.Setup(i => i.TotalTaxableDividend).Returns(new WrappedMoney(100, "Gbp"));
        dividendSummaryMock2.Setup(i => i.TotalTaxableDividend).Returns(new WrappedMoney(200, "Gbp"));
        dividendSummaryMock3.Setup(i => i.TotalTaxableDividend).Returns(new WrappedMoney(300, "Gbp"));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object]);
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
        Mock<DividendSummary> dividendSummaryMock1 = new();
        Mock<DividendSummary> dividendSummaryMock2 = new();
        Mock<DividendSummary> dividendSummaryMock3 = new();
        dividendSummaryMock1.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(30, "Gbp"));
        dividendSummaryMock2.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(50, "Gbp"));
        dividendSummaryMock3.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(70, "Gbp"));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object]);
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
        Mock<DividendSummary> dividendSummaryMock1 = new();
        Mock<DividendSummary> dividendSummaryMock2 = new();
        Mock<DividendSummary> dividendSummaryMock3 = new();
        dividendSummaryMock1.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(30, "Gbp"));
        dividendSummaryMock2.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(50, "Gbp"));
        dividendSummaryMock3.Setup(i => i.TotalForeignTaxPaid).Returns(new WrappedMoney(70, "Gbp"));
        dividendSummaryMock1.Setup(i => i.TaxYear).Returns(2021);
        dividendSummaryMock2.Setup(i => i.TaxYear).Returns(2022);
        dividendSummaryMock3.Setup(i => i.TaxYear).Returns(2023);
        var result = new DividendCalculationResult();
        result.SetResult([dividendSummaryMock1.Object, dividendSummaryMock2.Object, dividendSummaryMock3.Object]);
        var yearFilter = new List<int>();

        // Act
        var foreignTaxPaid = result.GetForeignTaxPaid(yearFilter);

        // Assert
        foreignTaxPaid.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }
}

