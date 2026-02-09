using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using NSubstitute;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class UkTradeCalculatorSpinoffTest
{
    private readonly UkSection104Pools _section104Pools;
    private readonly TradeTaxCalculationFactory _tradeTaxCalculationFactory;
    private readonly ITaxYear _taxYear;

    public UkTradeCalculatorSpinoffTest()
    {
        _taxYear = Substitute.For<ITaxYear>();
        _section104Pools = new UkSection104Pools(_taxYear, new ResidencyStatusRecord());
        _tradeTaxCalculationFactory = new TradeTaxCalculationFactory(new ResidencyStatusRecord());
    }

    private static ITradeAndCorporateActionList CreateMockTradeList(List<Trade> trades, List<CorporateAction> actions)
    {
        var list = Substitute.For<ITradeAndCorporateActionList>();
        list.Trades.Returns(trades);
        list.CorporateActions.Returns(actions);
        return list;
    }

    [Fact]
    public void PureSpinoff_SplitsCostBasisCorrectly()
    {
        // Arrange
        // 1. Buy 100 shares of PARENT at £10 each -> Cost £1000
        var buyParent = new Trade
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Spinoff: For every 1 PARENT share, receive 0.5 SPINOFF shares
        // Market values: PARENT £750 (75%), SPINOFF £250 (25%)
        // Note: Shares-only spinoffs keep fractional shares (for brokers that support them)
        var spinoff = new SpinoffCorporateAction
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 6, 1),
            SpinoffCompanyTicker = "SPINOFF",
            SpinoffSharesPerParentShare = 0.5m, // 100 PARENT -> 50 SPINOFF
            ParentMarketValue = new DescribedMoney(750m, "GBP", 1m),
            SpinoffMarketValue = new DescribedMoney(250m, "GBP", 1m),
            CashInLieu = null // Shares-only: fractional shares are kept
        };

        var tradeList = CreateMockTradeList([buyParent], [spinoff]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // PARENT pool should retain 75% of cost (£750)
        var parentS104 = _section104Pools.GetExistingOrInitialise("PARENT");
        parentS104.Quantity.ShouldBe(100m); // Quantity unchanged
        parentS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(750m);

        // SPINOFF pool should have 50 shares (100 * 0.5 = integer result)
        var spinoffS104 = _section104Pools.GetExistingOrInitialise("SPINOFF");
        spinoffS104.Quantity.ShouldBe(50m); // 100 * 0.5
        spinoffS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(250m);
    }

    [Fact]
    public void SpinoffWithCashInLieu_CreatesDisposalAndAdjustsCost()
    {
        // Arrange
        // 1. Buy 100 shares of PARENT at £1000
        var buyParent = new Trade
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Spinoff with cash-in-lieu for fractional shares
        // Market values split: PARENT 80%, SPINOFF 20%
        // Cash-in-lieu of £50
        // Use ElectTaxDeferral = false to test normal proportional matching (not small cash deferral)
        var spinoff = new SpinoffCorporateAction
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 6, 1),
            SpinoffCompanyTicker = "SPINOFF",
            SpinoffSharesPerParentShare = 0.25m, // 100 PARENT -> 25 SPINOFF
            ParentMarketValue = new DescribedMoney(800m, "GBP", 1m),
            SpinoffMarketValue = new DescribedMoney(200m, "GBP", 1m),
            CashInLieu = new DescribedMoney(50m, "GBP", 1m),
            ElectTaxDeferral = false // Test proportional matching, not deferral
        };

        var tradeList = CreateMockTradeList([buyParent], [spinoff]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // Should have a disposal for the cash-in-lieu
        var disposals = results.Where(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL).ToList();
        disposals.Count.ShouldBe(1);
        var cashDisposal = disposals[0];

        // Proceeds = Cash Received = £50
        cashDisposal.TotalProceeds.Amount.ShouldBe(50m);

        // The spinoff allocation is 20% of £1000 = £200
        // Cash-in-lieu proportion = £50 / (£200 market value SPINOFF + £50 cash) = 50/250 = 20%
        // So allowable cost = 20% of £200 spinoff allocation = £40
        cashDisposal.TotalAllowableCost.Amount.ShouldBe(40m);

        // Gain = 50 - 40 = 10
        cashDisposal.Gain.Amount.ShouldBe(10m);

        // PARENT pool keeps 80% = £800
        var parentS104 = _section104Pools.GetExistingOrInitialise("PARENT");
        parentS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(800m);

        // SPINOFF pool gets remaining spinoff allocation (£200 - £40 used for cash) = £160
        var spinoffS104 = _section104Pools.GetExistingOrInitialise("SPINOFF");
        spinoffS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(160m);
    }

    [Fact]
    public void Spinoff_DifferentMarketValueRatios_AllocatesCorrectly()
    {
        // Arrange
        // Buy 200 shares of MEGA at £5000
        var buyMega = new Trade
        {
            AssetName = "MEGA",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 200m,
            GrossProceed = new DescribedMoney(5000m, "GBP", 1m)
        };

        // Spinoff where parent retains only 40% of value
        // MEGA retains 40%, NEWCO gets 60%
        var spinoff = new SpinoffCorporateAction
        {
            AssetName = "MEGA",
            Date = new DateTime(2023, 6, 1),
            SpinoffCompanyTicker = "NEWCO",
            SpinoffSharesPerParentShare = 1m, // 200 MEGA -> 200 NEWCO
            ParentMarketValue = new DescribedMoney(2000m, "GBP", 1m), // 40%
            SpinoffMarketValue = new DescribedMoney(3000m, "GBP", 1m), // 60%
            CashInLieu = null
        };

        var tradeList = CreateMockTradeList([buyMega], [spinoff]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // MEGA keeps 40% = £2000
        var megaS104 = _section104Pools.GetExistingOrInitialise("MEGA");
        megaS104.Quantity.ShouldBe(200m);
        megaS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(2000m);

        // NEWCO gets 60% = £3000
        var newcoS104 = _section104Pools.GetExistingOrInitialise("NEWCO");
        newcoS104.Quantity.ShouldBe(200m);
        newcoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(3000m);
    }

    [Fact]
    public void NonResidentSpinoff_CashDisposal_IsNotTaxable()
    {
        // Arrange
        // Setup residency record: Resident initially, then Non-Resident from March 2023
        var residencyRecord = new ResidencyStatusRecord();
        residencyRecord.SetResidencyStatus(
            new DateOnly(2023, 3, 1), 
            new DateOnly(2023, 12, 31), 
            InvestmentTaxCalculator.Enumerations.ResidencyStatus.NonResident);

        var section104Pools = new UkSection104Pools(_taxYear, residencyRecord);
        var tradeTaxCalculationFactory = new TradeTaxCalculationFactory(residencyRecord);

        // 1. Buy 100 shares while resident
        var buyParent = new Trade
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Spinoff with cash-in-lieu while non-resident (June 2023)
        // Use ElectTaxDeferral = false to ensure a disposal is created
        var spinoff = new SpinoffCorporateAction
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 6, 1), // Non-resident period
            SpinoffCompanyTicker = "SPINOFF",
            SpinoffSharesPerParentShare = 0.5m,
            ParentMarketValue = new DescribedMoney(800m, "GBP", 1m),
            SpinoffMarketValue = new DescribedMoney(200m, "GBP", 1m),
            CashInLieu = new DescribedMoney(50m, "GBP", 1m),
            ElectTaxDeferral = false // Test residency handling, not deferral
        };

        var tradeList = CreateMockTradeList([buyParent], [spinoff]);
        var calculator = new UkTradeCalculator(
            section104Pools,
            tradeList,
            tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // Should have a cash disposal
        var cashDisposal = results.FirstOrDefault(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL);
        cashDisposal.ShouldNotBeNull();

        // Verify residency status is set correctly
        cashDisposal.ResidencyStatusAtTrade.ShouldBe(InvestmentTaxCalculator.Enumerations.ResidencyStatus.NonResident);

        // Verify the disposal is marked as NON_TAXABLE
        cashDisposal.MatchHistory.Count.ShouldBe(1);
        cashDisposal.MatchHistory[0].IsTaxable.ShouldBe(InvestmentTaxCalculator.Enumerations.TaxableStatus.NON_TAXABLE);
    }

    [Fact]
    public void SpinoffWithTradesBeforeAndAfter_ProcessesCorrectly()
    {
        // Scenario: Buy PARENT -> Spinoff -> Buy more SPINOFF -> Sell some SPINOFF

        // 1. Buy 100 PARENT at £1000
        var buyParent = new Trade
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Spinoff: 1:1 ratio, 60/40 split
        var spinoff = new SpinoffCorporateAction
        {
            AssetName = "PARENT",
            Date = new DateTime(2023, 2, 1),
            SpinoffCompanyTicker = "SPINOFF",
            SpinoffSharesPerParentShare = 1m, // 100 PARENT -> 100 SPINOFF
            ParentMarketValue = new DescribedMoney(600m, "GBP", 1m), // 60%
            SpinoffMarketValue = new DescribedMoney(400m, "GBP", 1m), // 40%
            CashInLieu = null
        };

        // 3. Buy 50 more SPINOFF at £300
        var buySpinoff = new Trade
        {
            AssetName = "SPINOFF",
            Date = new DateTime(2023, 3, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 50m,
            GrossProceed = new DescribedMoney(300m, "GBP", 1m)
        };

        // 4. Sell 75 SPINOFF at £600
        var sellSpinoff = new Trade
        {
            AssetName = "SPINOFF",
            Date = new DateTime(2023, 4, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL,
            Quantity = 75m,
            GrossProceed = new DescribedMoney(600m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyParent, buySpinoff, sellSpinoff], [spinoff]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // PARENT: 100 shares, cost = 60% of £1000 = £600
        var parentS104 = _section104Pools.GetExistingOrInitialise("PARENT");
        parentS104.Quantity.ShouldBe(100m);
        parentS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(600m);

        // SPINOFF after spinoff: 100 shares @ £400 + 50 shares @ £300 = 150 shares @ £700
        // After selling 75: 75 shares remaining
        // Cost sold = 75/150 * £700 = £350
        var spinoffS104 = _section104Pools.GetExistingOrInitialise("SPINOFF");
        spinoffS104.Quantity.ShouldBe(75m); // 150 - 75 sold

        // Remaining cost = £700 - £350 = £350
        spinoffS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(350m);

        // Check disposal gain
        var disposal = results.First(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL);
        disposal.TotalProceeds.Amount.ShouldBe(600m);
        disposal.TotalAllowableCost.Amount.ShouldBe(350m);
        disposal.Gain.Amount.ShouldBe(250m);
    }
}
