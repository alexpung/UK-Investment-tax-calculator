namespace UnitTest.Test.Model;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

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

    private static ITradeTaxCalculation SetUpMockTradeTaxCalculation(DateTime dateTime, TradeType tradeType,
        decimal totalAllowableCost, decimal totalProceeds, AssetCategoryType assetCategoryType, decimal gain = 0)
    {
        TradeMatch taxableMatch = new()
        {
            AssetName = "Test Asset",
            Date = DateOnly.FromDateTime(dateTime),
            IsTaxable = TaxableStatus.TAXABLE,
            TradeMatchType = TaxMatchType.SECTION_104
        };
        ITradeTaxCalculation mock = Substitute.For<ITradeTaxCalculation>();
        mock.MatchHistory.Returns([taxableMatch]);
        mock.AssetCategoryType.Returns(assetCategoryType);
        mock.Date.Returns(dateTime);
        mock.AcquisitionDisposal.Returns(tradeType);
        mock.TotalAllowableCost.Returns(new WrappedMoney(totalAllowableCost));
        mock.TotalProceeds.Returns(new WrappedMoney(totalProceeds));
        mock.Gain.Returns(new WrappedMoney(gain));
        mock.ReturnsForAll<WrappedMoney>(WrappedMoney.GetBaseCurrencyZero());
        return mock;
    }

    [Fact]
    public void NumberOfDisposals_ReturnsCorrectNumberOfDisposals()
    {
        // Arrange
        ITradeTaxCalculation mock1 = SetUpMockTradeTaxCalculation(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock2 = SetUpMockTradeTaxCalculation(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local), TradeType.ACQUISITION, 0, 0, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock3 = SetUpMockTradeTaxCalculation(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK);
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
        ITradeTaxCalculation mock1 = SetUpMockTradeTaxCalculation(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 100, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock2 = SetUpMockTradeTaxCalculation(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local), TradeType.ACQUISITION, 0, 200, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock3 = SetUpMockTradeTaxCalculation(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 300, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock4 = SetUpMockTradeTaxCalculation(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 400, AssetCategoryType.STOCK);
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
        ITradeTaxCalculation mock1 = SetUpMockTradeTaxCalculation(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 100, 0, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock2 = SetUpMockTradeTaxCalculation(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local), TradeType.ACQUISITION, 200, 0, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock3 = SetUpMockTradeTaxCalculation(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.ACQUISITION, 300, 0, AssetCategoryType.STOCK);
        ITradeTaxCalculation mock4 = SetUpMockTradeTaxCalculation(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 400, 0, AssetCategoryType.STOCK);
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
        ITradeTaxCalculation mock1 = SetUpMockTradeTaxCalculation(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK, 100);
        ITradeTaxCalculation mock2 = SetUpMockTradeTaxCalculation(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK, -200);
        ITradeTaxCalculation mock3 = SetUpMockTradeTaxCalculation(new DateTime(2022, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK, 300);
        ITradeTaxCalculation mock4 = SetUpMockTradeTaxCalculation(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 0, 0, AssetCategoryType.STOCK, -400);
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
        ITradeTaxCalculation mock1 = SetUpMockTradeTaxCalculation(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 50, 200, categoryType1, 150);
        ITradeTaxCalculation mock2 = SetUpMockTradeTaxCalculation(new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Local), TradeType.DISPOSAL, 100, 350, categoryType2, 250);
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
