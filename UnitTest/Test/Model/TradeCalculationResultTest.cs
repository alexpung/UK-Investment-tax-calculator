namespace UnitTest.Test.Model;

using Enum;
using global::Model;
using global::Model.Interfaces;
using Moq;
using NMoneys;
using System.Collections.Generic;
using Xunit;

public class TradeCalculationResultTests
{
    private class MockTaxYear : ITaxYear
    {
        public int ToTaxYear(DateTime dateTime)
        {
            return dateTime.Year;
        }
    }

    [Fact]
    public void NumberOfDisposals_ReturnsCorrectNumberOfDisposals()
    {
        // Arrange
        Mock<ITradeTaxCalculation> mock1 = new();
        mock1.Setup(i => i.Date).Returns(new DateTime(2021, 1, 1));
        mock1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        Mock<ITradeTaxCalculation> mock2 = new();
        mock2.Setup(i => i.Date).Returns(new DateTime(2021, 2, 1));
        mock2.Setup(i => i.BuySell).Returns(TradeType.BUY);
        Mock<ITradeTaxCalculation> mock3 = new();
        mock3.Setup(i => i.Date).Returns(new DateTime(2022, 3, 1));
        mock3.Setup(i => i.BuySell).Returns(TradeType.SELL);
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1.Object, mock2.Object, mock3.Object };
        var result = new TradeCalculationResult(taxYear);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021 };

        // Act
        int numberOfDisposals = result.NumberOfDisposals(taxYearsFilter);

        // Assert
        numberOfDisposals.ShouldBe(1);
    }

    [Fact]
    public void DisposalProceeds_ReturnsCorrectDisposalProceeds()
    {
        // Arrange
        Mock<ITradeTaxCalculation> mock1 = new();
        mock1.Setup(i => i.Date).Returns(new DateTime(2021, 1, 1));
        mock1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock1.Setup(i => i.TotalProceeds).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        Mock<ITradeTaxCalculation> mock2 = new();
        mock2.Setup(i => i.Date).Returns(new DateTime(2021, 2, 1));
        mock2.Setup(i => i.BuySell).Returns(TradeType.BUY);
        mock2.Setup(i => i.TotalProceeds).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        Mock<ITradeTaxCalculation> mock3 = new();
        mock3.Setup(i => i.Date).Returns(new DateTime(2022, 3, 1));
        mock3.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock3.Setup(i => i.TotalProceeds).Returns(BaseCurrencyMoney.BaseCurrencyAmount(300));
        Mock<ITradeTaxCalculation> mock4 = new();
        mock4.Setup(i => i.Date).Returns(new DateTime(2023, 3, 1));
        mock4.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock4.Setup(i => i.TotalProceeds).Returns(BaseCurrencyMoney.BaseCurrencyAmount(400));
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1.Object, mock2.Object, mock3.Object, mock4.Object };
        var result = new TradeCalculationResult(taxYear);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        Money disposalProceeds = result.DisposalProceeds(taxYearsFilter);

        // Assert
        disposalProceeds.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(500));
    }

    [Fact]
    public void AllowableCosts_ReturnsCorrectAllowableCosts()
    {
        // Arrange
        Mock<ITradeTaxCalculation> mock1 = new();
        mock1.Setup(i => i.Date).Returns(new DateTime(2021, 1, 1));
        mock1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock1.Setup(i => i.TotalAllowableCost).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        Mock<ITradeTaxCalculation> mock2 = new();
        mock2.Setup(i => i.Date).Returns(new DateTime(2021, 2, 1));
        mock2.Setup(i => i.BuySell).Returns(TradeType.BUY);
        mock2.Setup(i => i.TotalAllowableCost).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        Mock<ITradeTaxCalculation> mock3 = new();
        mock3.Setup(i => i.Date).Returns(new DateTime(2022, 3, 1));
        mock3.Setup(i => i.BuySell).Returns(TradeType.BUY);
        mock3.Setup(i => i.TotalAllowableCost).Returns(BaseCurrencyMoney.BaseCurrencyAmount(300));
        Mock<ITradeTaxCalculation> mock4 = new();
        mock4.Setup(i => i.Date).Returns(new DateTime(2023, 3, 1));
        mock4.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock4.Setup(i => i.TotalAllowableCost).Returns(BaseCurrencyMoney.BaseCurrencyAmount(400));
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1.Object, mock2.Object, mock3.Object, mock4.Object };
        var result = new TradeCalculationResult(taxYear);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        Money allowableCosts = result.AllowableCosts(taxYearsFilter);

        // Assert
        allowableCosts.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(500));
    }

    [Fact]
    public void TotalGainLoss_ReturnsCorrectTotalGainLoss()
    {
        // Arrange
        Mock<ITradeTaxCalculation> mock1 = new();
        mock1.Setup(i => i.Date).Returns(new DateTime(2021, 1, 1));
        mock1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock1.Setup(i => i.Gain).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        Mock<ITradeTaxCalculation> mock2 = new();
        mock2.Setup(i => i.Date).Returns(new DateTime(2021, 2, 1));
        mock2.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock2.Setup(i => i.Gain).Returns(BaseCurrencyMoney.BaseCurrencyAmount(-200));
        Mock<ITradeTaxCalculation> mock3 = new();
        mock3.Setup(i => i.Date).Returns(new DateTime(2022, 3, 1));
        mock3.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock3.Setup(i => i.Gain).Returns(BaseCurrencyMoney.BaseCurrencyAmount(300));
        Mock<ITradeTaxCalculation> mock4 = new();
        mock4.Setup(i => i.Date).Returns(new DateTime(2023, 3, 1));
        mock4.Setup(i => i.BuySell).Returns(TradeType.SELL);
        mock4.Setup(i => i.Gain).Returns(BaseCurrencyMoney.BaseCurrencyAmount(-400));
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1.Object, mock2.Object, mock3.Object, mock4.Object };
        var result = new TradeCalculationResult(taxYear);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        Money totalGain = result.TotalGain(taxYearsFilter);
        Money totalLoss = result.TotalLoss(taxYearsFilter);

        // Assert
        totalGain.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(100));
        totalLoss.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(-600));
    }
}


