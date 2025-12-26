namespace UnitTest.Test.Model;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;

using NSubstitute;
using NSubstitute.Extensions;

using System.Collections.Generic;

using Xunit;

public class TradeCalculationResultTests
{
    private static readonly ResidencyStatusRecord _residencyStatusRecord = new();
    private class MockTaxYear : ITaxYear
    {
        public DateOnly GetTaxYearEndDate(int taxYear)
        {
            return new DateOnly(taxYear + 1, 4, 5);
        }

        public DateOnly GetTaxYearStartDate(int taxYear)
        {
            return new DateOnly(taxYear, 4, 6);
        }

        public int ToTaxYear(DateTime dateTime)
        {
            return dateTime.Year;
        }
    }

    [Fact]
    public void NumberOfDisposals_ReturnsCorrectNumberOfDisposals()
    {
        // Arrange
        ITradeTaxCalculation mock1 = Substitute.For<ITradeTaxCalculation>();
        mock1.Date.Returns(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local));
        mock1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock1.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        mock1.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock2 = Substitute.For<ITradeTaxCalculation>();
        mock2.Date.Returns(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local));
        mock2.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        mock2.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        mock2.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock3 = Substitute.For<ITradeTaxCalculation>();
        mock3.Date.Returns(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock3.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock3.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        mock3.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1, mock2, mock3 };
        var result = new TradeCalculationResult(taxYear, _residencyStatusRecord);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021 };

        // Act
        int numberOfDisposals = result.GetNumberOfDisposals(taxYearsFilter);

        // Assert
        numberOfDisposals.ShouldBe(1);
    }

    [Fact]
    public void DisposalProceeds_ReturnsCorrectDisposalProceeds()
    {
        // Arrange
        ITradeTaxCalculation mock1 = Substitute.For<ITradeTaxCalculation>();
        mock1.Date.Returns(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local));
        mock1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock1.TotalProceeds.Returns(new WrappedMoney(100));
        mock1.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock2 = Substitute.For<ITradeTaxCalculation>();
        mock2.Date.Returns(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local));
        mock2.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        mock2.TotalProceeds.Returns(new WrappedMoney(200));
        mock2.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock3 = Substitute.For<ITradeTaxCalculation>();
        mock3.Date.Returns(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock3.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock3.TotalProceeds.Returns(new WrappedMoney(300));
        mock3.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock4 = Substitute.For<ITradeTaxCalculation>();
        mock4.Date.Returns(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock4.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock4.TotalProceeds.Returns(new WrappedMoney(400));
        mock4.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1, mock2, mock3, mock4 };
        var result = new TradeCalculationResult(taxYear, _residencyStatusRecord);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        WrappedMoney disposalProceeds = result.GetDisposalProceeds(taxYearsFilter);

        // Assert
        disposalProceeds.ShouldBe(new WrappedMoney(500));
    }

    [Fact]
    public void AllowableCosts_ReturnsCorrectAllowableCosts()
    {
        // Arrange
        ITradeTaxCalculation mock1 = Substitute.For<ITradeTaxCalculation>();
        mock1.Date.Returns(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local));
        mock1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock1.TotalAllowableCost.Returns(new WrappedMoney(100));
        mock1.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock2 = Substitute.For<ITradeTaxCalculation>();
        mock2.Date.Returns(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local));
        mock2.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        mock2.TotalAllowableCost.Returns(new WrappedMoney(200));
        mock2.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock3 = Substitute.For<ITradeTaxCalculation>();
        mock3.Date.Returns(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock3.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        mock3.TotalAllowableCost.Returns(new WrappedMoney(300));
        mock3.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        ITradeTaxCalculation mock4 = Substitute.For<ITradeTaxCalculation>();
        mock4.Date.Returns(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock4.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock4.TotalAllowableCost.Returns(new WrappedMoney(400));
        mock4.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1, mock2, mock3, mock4 };
        var result = new TradeCalculationResult(taxYear, _residencyStatusRecord);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        WrappedMoney allowableCosts = result.GetAllowableCosts(taxYearsFilter);

        // Assert
        allowableCosts.ShouldBe(new WrappedMoney(500));
    }

    [Fact]
    public void TotalGainLoss_ReturnsCorrectTotalGainLoss()
    {
        // Arrange
        ITradeTaxCalculation mock1 = Substitute.For<ITradeTaxCalculation>();
        mock1.Date.Returns(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local));
        mock1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock1.Gain.Returns(new WrappedMoney(100));
        mock1.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        mock1.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        ITradeTaxCalculation mock2 = Substitute.For<ITradeTaxCalculation>();
        mock2.Date.Returns(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local));
        mock2.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock2.Gain.Returns(new WrappedMoney(-200));
        mock2.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        mock2.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        ITradeTaxCalculation mock3 = Substitute.For<ITradeTaxCalculation>();
        mock3.Date.Returns(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock3.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock3.Gain.Returns(new WrappedMoney(300));
        mock3.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        mock3.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        ITradeTaxCalculation mock4 = Substitute.For<ITradeTaxCalculation>();
        mock4.Date.Returns(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local));
        mock4.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock4.Gain.Returns(new WrappedMoney(-400));
        mock4.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        mock4.AssetCategoryType.Returns(AssetCategoryType.STOCK);
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1, mock2, mock3, mock4 };
        var result = new TradeCalculationResult(taxYear, _residencyStatusRecord);
        result.SetResult(tradeTaxCalculations);

        var taxYearsFilter = new List<int> { 2021, 2023 };

        // Act
        WrappedMoney totalGain = result.GetTotalGain(taxYearsFilter);
        WrappedMoney totalLoss = result.GetTotalLoss(taxYearsFilter);

        // Assert
        totalGain.ShouldBe(new WrappedMoney(100));
        totalLoss.ShouldBe(new WrappedMoney(-600));
    }

    [InlineData(AssetCategoryType.FX, AssetCategoryType.OPTION)]
    [InlineData(AssetCategoryType.OPTION, AssetCategoryType.OPTION)]
    [InlineData(AssetCategoryType.FUTURE, AssetCategoryType.FX)]
    [InlineData(AssetCategoryType.STOCK, AssetCategoryType.OPTION)]
    [InlineData(AssetCategoryType.STOCK, AssetCategoryType.STOCK)]
    [InlineData(AssetCategoryType.OPTION, AssetCategoryType.STOCK)]
    [Theory]
    public void TradeTaxCalculationResult_ReturnCorrectSubTotals(AssetCategoryType categoryType1, AssetCategoryType categoryType2)
    {
        ITradeTaxCalculation mock1 = Substitute.For<ITradeTaxCalculation>();
        mock1.AssetCategoryType.Returns(categoryType1);
        mock1.Date.Returns(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local));
        mock1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock1.Gain.Returns(new WrappedMoney(150));
        mock1.TotalAllowableCost.Returns(new WrappedMoney(50));
        mock1.TotalProceeds.Returns(new WrappedMoney(200));
        ITradeTaxCalculation mock2 = Substitute.For<ITradeTaxCalculation>();
        mock2.AssetCategoryType.Returns(categoryType2);
        mock2.Date.Returns(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local));
        mock2.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        mock2.Gain.Returns(new WrappedMoney(250));
        mock2.TotalAllowableCost.Returns(new WrappedMoney(100));
        mock2.TotalProceeds.Returns(new WrappedMoney(350));
        var taxYear = new MockTaxYear();
        var tradeTaxCalculations = new List<ITradeTaxCalculation> { mock1, mock2 };
        var result = new TradeCalculationResult(taxYear, _residencyStatusRecord);
        result.SetResult(tradeTaxCalculations);
        var taxYearsFilter = new List<int> { 2021 };

        WrappedMoney expectedTotalGain = new(400);
        WrappedMoney expectedTotalAllowableCost = new(150);
        WrappedMoney expectedTotalProceeds = new(550);

        WrappedMoney totalGain = result.GetTotalGain(taxYearsFilter, AssetGroupType.ALL);
        WrappedMoney totalAllowableCost = result.GetAllowableCosts(taxYearsFilter, AssetGroupType.ALL);
        WrappedMoney totalProceeds = result.GetDisposalProceeds(taxYearsFilter, AssetGroupType.ALL);

        totalGain.ShouldBe(expectedTotalGain);
        totalAllowableCost.ShouldBe(expectedTotalAllowableCost);
        totalProceeds.ShouldBe(expectedTotalProceeds);
        (result.GetTotalGain(taxYearsFilter, AssetGroupType.LISTEDSHARES) + result.GetTotalGain(taxYearsFilter, AssetGroupType.OTHERASSETS)).ShouldBe(expectedTotalGain);
        (result.GetAllowableCosts(taxYearsFilter, AssetGroupType.LISTEDSHARES) + result.GetAllowableCosts(taxYearsFilter, AssetGroupType.OTHERASSETS)).ShouldBe(expectedTotalAllowableCost);
        (result.GetDisposalProceeds(taxYearsFilter, AssetGroupType.LISTEDSHARES) + result.GetDisposalProceeds(taxYearsFilter, AssetGroupType.OTHERASSETS)).ShouldBe(expectedTotalProceeds);
    }


}
