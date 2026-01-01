using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations;

public class ResidencyTests
{
    [Theory]
    [InlineData(ResidencyStatus.NonResident, TaxableStatus.NON_TAXABLE)]
    [InlineData(ResidencyStatus.TemporaryNonResident, TaxableStatus.NON_TAXABLE)]
    [InlineData(ResidencyStatus.Resident, TaxableStatus.TAXABLE)]
    public void SameDayResidencyMatchesAreOnlyTaxableWhenResidentInBothCountries(ResidencyStatus residencyStatus, TaxableStatus taxableStatus)
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2021, 6, 15), residencyStatus);
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("08-Apr-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("08-Apr-21 15:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 300,
            GrossProceed = new() { Description = "", Amount = new(5000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade1, trade2 }, out UkSection104Pools section104Pools, ResidencyStatusList);
        result[0].MatchHistory[0].IsTaxable.ShouldBe(taxableStatus);
    }

    [Theory]
    [InlineData(ResidencyStatus.NonResident)]
    [InlineData(ResidencyStatus.TemporaryNonResident)]
    public void BedAndBreadfastDoesNotApplyToNonResidents(ResidencyStatus residencyStatus)
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2021, 6, 15), residencyStatus);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("07-Apr-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("08-Apr-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("10-Apr-21 15:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 300,
            GrossProceed = new() { Description = "", Amount = new(5000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1, trade2 }, out UkSection104Pools _, ResidencyStatusList);
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        result[0].MatchHistory.Count.ShouldBe(1);
    }

    [Fact]
    public void TemporaryNonResidentTaxedWhenReturningToResidence()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(DateOnly.MinValue, new DateOnly(2020, 6, 14), ResidencyStatus.Resident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), ResidencyStatus.TemporaryNonResident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2023, 6, 16), DateOnly.MaxValue, ResidencyStatus.Resident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("14-Jun-20 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Jun-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.TAXABLE);
    }

    [Fact]
    public void TemporaryNonResidentAssetsAcquiredInSamePeriodAreNotTaxable()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), ResidencyStatus.TemporaryNonResident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jul-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.NON_TAXABLE);
    }

    [Fact]
    public void TemporaryNonResidentAssetsAcquiredInPreviousTemporaryNonResidentPeriodAreTaxable()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(DateOnly.MinValue, new DateOnly(2020, 6, 14), ResidencyStatus.TemporaryNonResident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), ResidencyStatus.Resident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2023, 6, 16), new DateOnly(2028, 6, 15), ResidencyStatus.TemporaryNonResident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jul-19 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.TAXABLE);
    }

    [Fact]
    public void TemporaryNonResidentPartialMatchesAreCorrectlySplit()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(DateOnly.MinValue, new DateOnly(2020, 6, 14), ResidencyStatus.Resident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), ResidencyStatus.TemporaryNonResident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2023, 6, 16), DateOnly.MaxValue, ResidencyStatus.Resident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-20 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 200,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 300,
            GrossProceed = new() { Description = "", Amount = new(3000m) },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Jan-23 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 400,
            GrossProceed = new() { Description = "", Amount = new(6000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1, trade2 }, out UkSection104Pools _, ResidencyStatusList);
        result[2].MatchHistory.Count.ShouldBe(2);
        result[2].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.NON_TAXABLE);
        result[2].MatchHistory[0].MatchAcquisitionQty.ShouldBe(300);
        result[2].MatchHistory[1].IsTaxable.ShouldBe(TaxableStatus.TAXABLE);
        result[2].MatchHistory[1].MatchAcquisitionQty.ShouldBe(100);
        result[2].MatchHistory[1].MatchGain.ShouldBe(new WrappedMoney(500m));
    }

    [Fact]
    public void NonResidentAcquisitionAreTaxedIfDisposedWhenResident()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), ResidencyStatus.NonResident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2023, 6, 16), DateOnly.MaxValue, ResidencyStatus.Resident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jul-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-23 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.TAXABLE);
    }

    [Fact]
    public void NonResidentAcquisitionAreTaxedIfDisposedAtNextTemporaryNonResidentPeriod()
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 15), new DateOnly(2020, 6, 15), ResidencyStatus.NonResident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 16), new DateOnly(2022, 1, 1), ResidencyStatus.Resident);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2022, 1, 2), new DateOnly(2028, 6, 15), ResidencyStatus.TemporaryNonResident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jul-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-24 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.TAXABLE);
    }

    [Theory]
    [InlineData(ResidencyStatus.NonResident)]
    [InlineData(ResidencyStatus.TemporaryNonResident)]
    [InlineData(ResidencyStatus.Resident)]
    public void AssetsDisposedWhenNonResidentAreNotTaxable(ResidencyStatus residencyStatus)
    {
        ResidencyStatusRecord ResidencyStatusList = new();
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2015, 6, 15), new DateOnly(2020, 6, 15), residencyStatus);
        ResidencyStatusList.SetResidencyStatus(new DateOnly(2020, 6, 16), new DateOnly(2023, 6, 15), ResidencyStatus.NonResident);
        Trade trade0 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jul-19 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(1000m) },
        };
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("20-Aug-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "", Amount = new(2000m) },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade0, trade1 }, out UkSection104Pools _, ResidencyStatusList);
        result[1].MatchHistory[0].IsTaxable.ShouldBe(TaxableStatus.NON_TAXABLE);
    }
}
