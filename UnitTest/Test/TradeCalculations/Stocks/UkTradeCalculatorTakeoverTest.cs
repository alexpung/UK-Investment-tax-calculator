using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using NSubstitute;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class UkTradeCalculatorTakeoverTest
{
    private readonly UkSection104Pools _section104Pools;
    private readonly TradeTaxCalculationFactory _tradeTaxCalculationFactory;
    private readonly ITaxYear _taxYear;

    public UkTradeCalculatorTakeoverTest()
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
    public void SharesOnlyTakeover_TransfersS104PoolCorrectly()
    {
        // Arrange
        // 1. Buy 100 shares of OLDCO at £10 each -> Cost £1000
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover: 1 OLDCO -> 2 NEWCO
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 2.0m, // 100 OLDCO -> 200 NEWCO
            CashComponent = null,
            ElectTaxDeferral = false,
            NewSharesMarketValue = null // Not needed for shares-only
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // OLDCO pool should be empty
        var oldCoS104 = _section104Pools.GetExistingOrInitialise("OLDCO");
        oldCoS104.Quantity.ShouldBe(0m);
        oldCoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(0m);

        // NEWCO pool should have transferred values
        var newCoS104 = _section104Pools.GetExistingOrInitialise("NEWCO");
        newCoS104.Quantity.ShouldBe(200m); // 100 * 2
        newCoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(1000m); // Original cost transferred
    }

    [Fact]
    public void SharesPlusCash_SmallCash_ElectedDeferral_ReducesCostBasis()
    {
        // Arrange
        // 1. Buy 100 shares of OLDCO at £1000
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover: 1 OLDCO -> 1 NEWCO + £5 Cash (Total Cash £500)
        // Cash £500 is < £3000, so small cash rules apply
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m, // 100 OLDCO -> 100 NEWCO
            CashComponent = new DescribedMoney(500m, "GBP", 1m), // £500 Total Cash
            ElectTaxDeferral = true, // User elects to defer
            NewSharesMarketValue = new DescribedMoney(2000, "GBP", 1m) // New shares worth £2000
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // No cash disposal created
        results.Count(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL).ShouldBe(0);
        // NEWCO pool cost should be reduced by cash amount
        var newCoS104 = _section104Pools.GetExistingOrInitialise("NEWCO");
        newCoS104.Quantity.ShouldBe(100m);
        newCoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(500m); // £1000 - £500 cash
    }

    [Fact]
    public void SharesPlusCash_SmallCash_Excess_CalculatingCostCorrectly()
    {
        // Arrange
        // 1. Buy 100 shares of OLDCO at £100 (Very low cost basis)
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(100m, "GBP", 1m)
        };

        // 2. Takeover with £500 Cash (Small < £3000)
        // But Cash (£500) > Cost (£100). Excess is £400.
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m,
            CashComponent = new DescribedMoney(500m, "GBP", 1m),
            ElectTaxDeferral = true,
            NewSharesMarketValue = new DescribedMoney(2000, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        var cashDisposal = results.First(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL);

        // Proceeds = £500-£100
        cashDisposal.TotalProceeds.Amount.ShouldBe(400m);

        // Allowable Cost used = 0 since all cost basis used up to reduce cash gain.
        cashDisposal.TotalAllowableCost.Amount.ShouldBe(0m);

        // Gain = 400 - 0 = 400
        cashDisposal.Gain.Amount.ShouldBe(400m);

        // New Company Pool should have 0 cost
        var newCoS104 = _section104Pools.GetExistingOrInitialise("NEWCO");
        newCoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(0m);
    }

    [Fact]
    public void SharesPlusCash_NonSmallCash_CreatesDisposalAndTransfers()
    {
        // Arrange
        // 1. Buy 800 shares of A for £1000
        var buyOld = new Trade
        {
            AssetName = "A",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 800m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover by B
        // Get 1600 shares of B (Value £9600)
        // Get Cash £3200
        // Total Value £12800
        // Cash is > £3000, so normal (part-disposal) rules apply
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "A",
            Date = new DateTime(2023, 6, 1),
            AcquiringCompanyTicker = "B",
            OldToNewRatio = 2.0m, // 800 A -> 1600 B
            CashComponent = new DescribedMoney(3200m, "GBP", 1m),
            ElectTaxDeferral = false,
            NewSharesMarketValue = new DescribedMoney(9600m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        var results = calculator.CalculateTax();

        // Assert
        // Should have a disposal for the cash
        var disposals = results.Where(r => r.AcquisitionDisposal == InvestmentTaxCalculator.Enumerations.TradeType.DISPOSAL).ToList();
        disposals.Count.ShouldBe(1);
        var cashDisposal = disposals[0];

        // Proceeds = Cash Received = £3200
        cashDisposal.TotalProceeds.Amount.ShouldBe(3200m);

        // Expenses (Allowable Cost) calculated proportionally
        // Cash Ratio = 3200 / (3200 + 9600) = 3200 / 12800 = 0.25 (25%)
        // Allowable Cost = 25% of £1000 = £250
        cashDisposal.TotalAllowableCost.Amount.ShouldBe(250m);

        // Gain = 3200 - 250 = 2950
        cashDisposal.Gain.Amount.ShouldBe(2950m);

        // New Company Pool check
        var newCoS104 = _section104Pools.GetExistingOrInitialise("B");
        newCoS104.Quantity.ShouldBe(1600m);
        // Cost should be remaining 75% of £1000 = £750
        newCoS104.AcquisitionCostInBaseCurrency.Amount.ShouldBe(750m);
    }

    [Fact]
    public void TakeoverWithTradesBeforeAndAfter_ProcessesCorrectly()
    {
        // OLDCO: Buy 100 @ £10 (£1000) -> Takeover 1:1 to CORP1 -> CORP1: Buy 50 @ £20 (£1000) -> Takeover 1:1 to CORP2 

        // 1. Buy 100 OLDCO
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover OLDCO -> CORP1
        var takeover1 = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 2, 1),
            AcquiringCompanyTicker = "CORP1",
            OldToNewRatio = 1.0m,
            CashComponent = null,
            ElectTaxDeferral = false,
            NewSharesMarketValue = null
        };

        // 3. Buy 50 CORP1
        var buyCorp1 = new Trade
        {
            AssetName = "CORP1",
            Date = new DateTime(2023, 3, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 50m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 4. Takeover CORP1 -> CORP2
        var takeover2 = new TakeoverCorporateAction
        {
            AssetName = "CORP1",
            Date = new DateTime(2023, 4, 1),
            AcquiringCompanyTicker = "CORP2",
            OldToNewRatio = 1.0m,
            CashComponent = null,
            ElectTaxDeferral = false,
            NewSharesMarketValue = null
        };

        var tradeList = CreateMockTradeList([buyOld, buyCorp1], [takeover1, takeover2]);
        var calculator = new UkTradeCalculator(
            _section104Pools,
            tradeList,
            _tradeTaxCalculationFactory);

        // Act
        calculator.CalculateTax();

        // Assert
        // OLDCO Empty
        _section104Pools.GetExistingOrInitialise("OLDCO").Quantity.ShouldBe(0m);

        // CORP1 Empty
        var corp1S104 = _section104Pools.GetExistingOrInitialise("CORP1");
        // corp1S104.Quantity.ShouldBe(0m); // Fails (150m) unless takeover handles it. 
        // With correct logic (resetting state), it should process OLDCO->CORP1 (adding 100), then process CORP1.
        // Wait, ProcessOldCompany resets state.
        // Takeover of CORP1 by CORP2.
        // When processing CORP1 group (Phase 1 of takeover2):
        //   It empties CORP1 pool (100 from takeover + 50 purchase = 150).
        // Then processing CORP2 group (Phase 2 of takeover2):
        //   It adds 150 to CORP2.

        corp1S104.Quantity.ShouldBe(0m);

        // CORP2 has 150 shares
        var corp2S104 = _section104Pools.GetExistingOrInitialise("CORP2");
        corp2S104.Quantity.ShouldBe(150m);
    }

    [Fact]
    public void NonResidentTakeover_CashDisposal_IsNotTaxable()
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

        // 1. Buy 100 shares of OLDCO while resident
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover with cash while non-resident (June 2023)
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1), // Non-resident period
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m,
            CashComponent = new DescribedMoney(500m, "GBP", 1m),
            ElectTaxDeferral = false,
            NewSharesMarketValue = new DescribedMoney(2000m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
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
    public void ResidentTakeover_CashDisposal_IsTaxable()
    {
        // Arrange
        // Setup residency record: Resident throughout
        var residencyRecord = new ResidencyStatusRecord();
        // Default is Resident, no need to set anything

        var section104Pools = new UkSection104Pools(_taxYear, residencyRecord);
        var tradeTaxCalculationFactory = new TradeTaxCalculationFactory(residencyRecord);

        // 1. Buy 100 shares of OLDCO while resident
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover with cash while resident (June 2023)
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1), // Resident period
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m,
            CashComponent = new DescribedMoney(500m, "GBP", 1m),
            ElectTaxDeferral = false,
            NewSharesMarketValue = new DescribedMoney(2000m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
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
        cashDisposal.ResidencyStatusAtTrade.ShouldBe(InvestmentTaxCalculator.Enumerations.ResidencyStatus.Resident);

        // Verify the disposal is marked as TAXABLE
        cashDisposal.MatchHistory.Count.ShouldBe(1);
        cashDisposal.MatchHistory[0].IsTaxable.ShouldBe(InvestmentTaxCalculator.Enumerations.TaxableStatus.TAXABLE);
    }

    [Fact]
    public void TemporaryNonResidentTakeover_CashDisposal_IsTaxable()
    {
        // Arrange
        // Setup residency record: Temporary Non-Resident from March to December 2023
        var residencyRecord = new ResidencyStatusRecord();
        residencyRecord.SetResidencyStatus(
            new DateOnly(2023, 3, 1), 
            new DateOnly(2023, 12, 31), 
            InvestmentTaxCalculator.Enumerations.ResidencyStatus.TemporaryNonResident);

        var section104Pools = new UkSection104Pools(_taxYear, residencyRecord);
        var tradeTaxCalculationFactory = new TradeTaxCalculationFactory(residencyRecord);

        // 1. Buy 100 shares of OLDCO while resident
        var buyOld = new Trade
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = InvestmentTaxCalculator.Enumerations.TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };

        // 2. Takeover with cash while temporary non-resident (June 2023)
        var takeover = new TakeoverCorporateAction
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1), // Temporary non-resident period
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m,
            CashComponent = new DescribedMoney(500m, "GBP", 1m),
            ElectTaxDeferral = false,
            NewSharesMarketValue = new DescribedMoney(2000m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buyOld], [takeover]);
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
        cashDisposal.ResidencyStatusAtTrade.ShouldBe(InvestmentTaxCalculator.Enumerations.ResidencyStatus.TemporaryNonResident);

        // Verify the disposal is marked as TAXABLE (temporary non-residents are taxed)
        cashDisposal.MatchHistory.Count.ShouldBe(1);
        cashDisposal.MatchHistory[0].IsTaxable.ShouldBe(InvestmentTaxCalculator.Enumerations.TaxableStatus.TAXABLE);
    }
}
