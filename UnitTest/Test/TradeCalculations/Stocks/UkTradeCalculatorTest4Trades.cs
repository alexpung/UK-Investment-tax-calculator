﻿using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Globalization;
using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;
public class UkTradeCalculatorTest4Trades
{
    [Fact]
    public void TestSameDay_BedAndBreakfast_Section104_StockSplit()
    {
        Trade trade1 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "ABC Example Stock",
            Quantity = 200,
            Expenses =
            [
                new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.8m },
                new() { Description = "Tax", Amount = new(20m, "USD"), FxRate = 0.8m },
            ],
            GrossProceed = new() { Description = "", Amount = new(2000m, "USD"), FxRate = 0.8m },
        };
        Trade trade2 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("03-May-21 12:33:56", CultureInfo.InvariantCulture),
            Description = "ABC Example Stock",
            Quantity = 50,
            Expenses =
            [
                new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m },
                new() { Description = "Tax", Amount = new(30m, "USD"), FxRate = 0.85m },
            ],
            GrossProceed = new() { Description = "", Amount = new(510m, "USD"), FxRate = 0.85m },
        };
        Trade trade3 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("03-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "ABC Example Stock",
            Quantity = 200,
            Expenses =
            [
                new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m },
                new() { Description = "Tax", Amount = new(40m, "USD"), FxRate = 0.85m },
            ],
            GrossProceed = new() { Description = "", Amount = new(2200m, "USD"), FxRate = 0.85m },
        };
        Trade trade4 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("04-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "ABC Example Stock",
            Quantity = 100,
            Expenses =
            [
                new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m },
                new() { Description = "Tax", Amount = new(50m, "USD"), FxRate = 0.86m },
            ],
            GrossProceed = new() { Description = "", Amount = new(600m, "USD"), FxRate = 0.86m },
        };
        StockSplit stockSplit = new() { AssetName = "ABC", Date = DateTime.Parse("03-May-21 20:25:00", CultureInfo.InvariantCulture), SplitTo = 2, SplitFrom = 1 };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades([trade1, trade2, trade3, trade4, stockSplit], out UkSection104Pools section104Pools);
        result[2].TotalProceeds.ShouldBe(new WrappedMoney(1834.725m));
        result[2].Gain.ShouldBe(new WrappedMoney(5.56m));
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        result[2].MatchHistory[0].MatchDisposalQty.ShouldBe(50);
        result[2].MatchHistory[0].MatchAcquisitionQty.ShouldBe(50);
        result[2].MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(458.68125m));
        result[2].MatchHistory[0].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(460.275m));
        result[2].MatchHistory[1].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        result[2].MatchHistory[1].MatchDisposalQty.ShouldBe(50);
        result[2].MatchHistory[1].MatchAcquisitionQty.ShouldBe(100);
        result[2].MatchHistory[1].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(458.68125m));
        result[2].MatchHistory[1].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(560.29m));
        result[2].MatchHistory[2].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        result[2].MatchHistory[2].MatchAcquisitionQty.ShouldBe(100);
        result[2].MatchHistory[2].MatchDisposalQty.ShouldBe(100);
        result[2].MatchHistory[2].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(917.3625m));
        result[2].MatchHistory[2].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(808.6m));
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(808.6m));
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(200);
    }

    // Example from https://assets.publishing.service.gov.uk/government/uploads/system/uploads/attachment_data/file/1145311/HS284_Example_3_2023.pdf
    [Fact]
    public void HMRCExample()
    {
        Trade trade1 = new()
        {
            AssetName = "Lobster plc",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Apr-14 12:33:56", CultureInfo.InvariantCulture),
            Description = "Lobster plc",
            Quantity = 1000,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(150m) }],
            GrossProceed = new() { Amount = new(4000m) },
        };

        Trade trade2 = new()
        {
            AssetName = "Lobster plc",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Sep-17 12:33:56", CultureInfo.InvariantCulture),
            Description = "Lobster plc",
            Quantity = 500,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(80m) }],
            GrossProceed = new() { Amount = new(2050m) },
        };

        Trade trade3 = new()
        {
            AssetName = "Lobster plc",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-May-22 12:33:56", CultureInfo.InvariantCulture),
            Description = "Lobster plc",
            Quantity = 700,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(100m) }],
            GrossProceed = new() { Amount = new(3360m) },
        };

        Trade trade4 = new()
        {
            AssetName = "Lobster plc",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Feb-23 12:33:56", CultureInfo.InvariantCulture),
            Description = "Lobster plc",
            Quantity = 400,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(105m) }],
            GrossProceed = new() { Amount = new(2080m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades([trade1, trade2, trade3, trade4], out _);
        result[2].Gain.Amount.ShouldBe(329m, 0.99m);
        result[3].Gain.Amount.ShouldBe(300m, 0.99m);
    }
}
