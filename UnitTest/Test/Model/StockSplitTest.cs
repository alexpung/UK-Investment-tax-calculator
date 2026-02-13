using InvestmentTaxCalculator.Model.TaxEvents;

namespace UnitTest.Test.Model;

using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using NSubstitute;

using Shouldly;

using System;
using System.Globalization;

using Xunit;

public class StockSplitTests
{
    private readonly StockSplit _stockSplit;

    public StockSplitTests()
    {
        _stockSplit = new StockSplit
        {
            SplitTo = 2,
            SplitFrom = 1,
            AssetName = "ABC",
            Date = DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)
        };
    }

    [Fact]
    public void Reason_ShouldReturnCorrectReasonMessage()
    {
        // Act
        var reason = _stockSplit.Reason;

        // Assert
        reason.ShouldContain("ABC");
        reason.ShouldContain("split 2 for 1");
    }

    [Fact]
    public void TradeMatching_TradesOutsideStockSplitDate_NoAdjustment()
    {
        // Arrange
        var trade1 = Substitute.For<ITradeTaxCalculation>();
        var trade2 = Substitute.For<ITradeTaxCalculation>();
        var matchAdjustment = new MatchAdjustment { MatchAdjustmentFactor = 1 };

        trade1.Date.Returns(DateTime.Parse("02-Jan-23 10:00:00", CultureInfo.InvariantCulture));
        trade2.Date.Returns(DateTime.Parse("01-Feb-23 10:00:00", CultureInfo.InvariantCulture));
        trade1.AssetName.Returns("ABC");
        trade2.AssetName.Returns("ABC");

        // Act
        var result = _stockSplit.TradeMatching(trade1, trade2, matchAdjustment);

        // Assert
        result.MatchAdjustmentFactor.ShouldBe(1);
        result.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void TradeMatching_TradesWithinStockSplitDate_AdjustsCorrectly()
    {
        // Arrange
        var trade1 = Substitute.For<ITradeTaxCalculation>();
        var trade2 = Substitute.For<ITradeTaxCalculation>();
        var matchAdjustment = new MatchAdjustment { MatchAdjustmentFactor = 1 };

        trade1.Date.Returns(DateTime.Parse("31-Dec-22 09:00:00", CultureInfo.InvariantCulture));
        trade2.Date.Returns(DateTime.Parse("05-Jan-23 10:00:00", CultureInfo.InvariantCulture));
        trade1.AssetName.Returns("ABC");
        trade2.AssetName.Returns("ABC");

        // Act
        var result = _stockSplit.TradeMatching(trade1, trade2, matchAdjustment);

        // Assert
        result.MatchAdjustmentFactor.ShouldBe(2);
        result.CorporateActions.ShouldContain(_stockSplit);
    }

    [Fact]
    public void TradeMatching_DifferentAssets_NoAdjustment()
    {
        // Arrange
        var trade1 = Substitute.For<ITradeTaxCalculation>();
        var trade2 = Substitute.For<ITradeTaxCalculation>();
        var matchAdjustment = new MatchAdjustment { MatchAdjustmentFactor = 1 };

        trade1.Date.Returns(DateTime.Parse("01-Jan-23 09:00:00", CultureInfo.InvariantCulture));
        trade2.Date.Returns(DateTime.Parse("05-Jan-23 10:00:00", CultureInfo.InvariantCulture));
        trade1.AssetName.Returns("XYZ");
        trade2.AssetName.Returns("XYZ");

        // Act
        var result = _stockSplit.TradeMatching(trade1, trade2, matchAdjustment);

        // Assert
        result.MatchAdjustmentFactor.ShouldBe(1);
        result.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void TradeMatching_SameDayTrades_NoAdjustment()
    {
        // Arrange
        var trade1 = Substitute.For<ITradeTaxCalculation>();
        var trade2 = Substitute.For<ITradeTaxCalculation>();
        var matchAdjustment = new MatchAdjustment { MatchAdjustmentFactor = 1 };

        trade1.Date.Returns(DateTime.Parse("01-Jan-23 09:00:00", CultureInfo.InvariantCulture));
        trade2.Date.Returns(DateTime.Parse("01-Jan-23 15:00:00", CultureInfo.InvariantCulture));
        trade1.AssetName.Returns("ABC");
        trade2.AssetName.Returns("ABC");

        // Act
        var result = _stockSplit.TradeMatching(trade1, trade2, matchAdjustment);

        // Assert
        result.MatchAdjustmentFactor.ShouldBe(1);
        result.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeSection104_ValidSection104_UpdatesQuantityCorrectly()
    {
        // Arrange
        var section104 = new UkSection104("ABC")
        {
            Quantity = 100,
            AcquisitionCostInBaseCurrency = new WrappedMoney(1000) // Mocked acquisition cost
        };

        // Act
        _stockSplit.ChangeSection104(section104);

        // Assert
        section104.Quantity.ShouldBe(200); // 100 * (2/1) = 200
        section104.Section104HistoryList.Count.ShouldBe(1);
        section104.Section104HistoryList[0].ValueChange.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
    }

    [Fact]
    public void ChangeSection104_DifferentAssetName_NoChange()
    {
        // Arrange
        var section104 = new UkSection104("XYZ")
        {
            Quantity = 100,
            AcquisitionCostInBaseCurrency = new WrappedMoney(1000) // Mocked acquisition cost
        };

        // Act
        _stockSplit.ChangeSection104(section104);

        // Assert
        section104.Quantity.ShouldBe(100); // No change as asset name is different
        section104.Section104HistoryList.ShouldBeEmpty();
    }
}
