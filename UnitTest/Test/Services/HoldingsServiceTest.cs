using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.Services;

/// <summary>
/// The units actually held, as opposed to the section 104 pool quantity. The two agree for a plain buy and sell
/// history but deliberately part company where the same day or bed and breakfast matching rules apply, and the
/// holding is the figure that fixes excess reportable income liability (SI 2009/3001 reg. 94(3)).
/// </summary>
public class HoldingsServiceTest
{
    private const string AssetName = "FUND";

    private static Trade CreateTrade(TradeType tradeType, string date, decimal quantity, string assetName = AssetName) => new()
    {
        AssetName = assetName,
        AcquisitionDisposal = tradeType,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(quantity * 10m) },
    };

    private static HoldingsService BuildService(IEnumerable<TaxEvent> taxEvents)
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(taxEventLists.AllEvents);
        return new HoldingsService(taxEventLists, registry);
    }

    [Fact]
    public void TestRunningTotalOfAcquisitionsAndDisposals()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400)
        ]);

        service.GetHolding(AssetName, new DateOnly(2022, 12, 31)).Quantity.ShouldBe(0m);
        service.GetHolding(AssetName, new DateOnly(2023, 1, 1)).Quantity.ShouldBe(1000m);
        service.GetHolding(AssetName, new DateOnly(2023, 5, 31)).Quantity.ShouldBe(1000m);
        service.GetHolding(AssetName, new DateOnly(2023, 6, 1)).Quantity.ShouldBe(600m);
    }

    [Fact]
    public void TestBedAndBreakfastRepurchaseDoesNotInflateTheHoldingLikeTheSection104Pool()
    {
        // The 1 Jun disposal is matched with the 10 Jun repurchase (TCGA92/S106A), so neither leg reaches the
        // section 104 pool: between the two dates the pool still reads 1000 while only 950 units are held. The
        // holding, not the pool, is what an ERI period ending in that window must be computed on.
        List<TaxEvent> taxEvents = [
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 50),
            CreateTrade(TradeType.ACQUISITION, "10-Jun-23 10:00:00", 50)
        ];
        HoldingsService service = BuildService(taxEvents);

        TradeCalculationHelper.CalculateTrades(taxEvents, out UkSection104Pools section104Pools);
        UkSection104 pool = section104Pools.GetExistingOrInitialise(AssetName);
        pool.GetLastSection104History(new DateOnly(2023, 6, 5))!.NewQuantity.ShouldBe(1000m);

        service.GetHolding(AssetName, new DateOnly(2023, 6, 5)).Quantity.ShouldBe(950m);
        // They agree again once the repurchase has settled.
        service.GetHolding(AssetName, new DateOnly(2023, 6, 30)).Quantity.ShouldBe(1000m);
    }

    [Fact]
    public void TestStockSplitMultipliesTheHolding()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            new StockSplit
            {
                AssetName = AssetName,
                Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
                SplitTo = 2,
                SplitFrom = 1
            }
        ]);

        service.GetHolding(AssetName, new DateOnly(2023, 5, 31)).Quantity.ShouldBe(1000m);
        service.GetHolding(AssetName, new DateOnly(2023, 6, 1)).Quantity.ShouldBe(2000m);
    }

    [Fact]
    public void TestPartnerTransfersMoveUnitsInAndOut()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            new PartnerTransferCorporateAction
            {
                AssetName = AssetName,
                Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
                Direction = PartnerTransferDirection.GiftToPartner,
                Quantity = 300
            },
            new PartnerTransferCorporateAction
            {
                AssetName = AssetName,
                Date = DateTime.Parse("01-Jul-23 00:00:00", CultureInfo.InvariantCulture),
                Direction = PartnerTransferDirection.ReceiveFromPartner,
                Quantity = 100,
                TransferredCost = new DescribedMoney(500m, "GBP", 1m, "Transferred partner cost")
            }
        ]);

        service.GetHolding(AssetName, new DateOnly(2023, 6, 30)).Quantity.ShouldBe(700m);
        service.GetHolding(AssetName, new DateOnly(2023, 7, 31)).Quantity.ShouldBe(800m);
    }

    [Fact]
    public void TestTakeoverEmptiesTheOldTickerAndFillsTheAcquiringTicker()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, "OLDCO"),
            new TakeoverCorporateAction
            {
                AssetName = "OLDCO",
                Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
                AcquiringCompanyTicker = "NEWCO",
                OldToNewRatio = 0.5m
            }
        ]);

        service.GetHolding("OLDCO", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(0m);
        service.GetHolding("NEWCO", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(500m);
        // A holding asked for before the takeover is unaffected by it.
        service.GetHolding("OLDCO", new DateOnly(2023, 5, 31)).Quantity.ShouldBe(1000m);
    }

    [Fact]
    public void TestSpinoffAddsTheSpinoffTickerAndLeavesTheParentHoldingAlone()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, "PARENT"),
            new SpinoffCorporateAction
            {
                AssetName = "PARENT",
                Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
                SpinoffCompanyTicker = "SPINCO",
                SpinoffSharesPerParentShare = 0.25m,
                ParentMarketValue = new DescribedMoney(80m, "GBP", 1m, "Parent MV"),
                SpinoffMarketValue = new DescribedMoney(20m, "GBP", 1m, "Spinoff MV")
            }
        ]);

        service.GetHolding("PARENT", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(1000m);
        service.GetHolding("SPINCO", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(250m);
    }

    [Fact]
    public void TestTickerVariationsOfTheSameShareShareOneHolding()
    {
        // Two statements record the same share under different tickers; the ISIN ties them to one identity.
        Trade firstStatement = CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 600, "FB");
        firstStatement.Isin = "US30303M1027";
        Trade secondStatement = CreateTrade(TradeType.ACQUISITION, "01-Jun-23 10:00:00", 400, "META");
        secondStatement.Isin = "US30303M1027";
        HoldingsService service = BuildService([firstStatement, secondStatement]);

        service.GetHolding("META", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(1000m);
        service.GetHolding("FB", new DateOnly(2023, 12, 31)).Quantity.ShouldBe(1000m);
    }

    [Fact]
    public void TestDerivativesAreNotCountedTowardsTheShareHolding()
    {
        // An option's asset type is not STOCK or FUND: its "quantity" counts contracts and must not be pooled in.
        Trade shareTrade = CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000);
        OptionTrade optionTrade = new()
        {
            AssetName = AssetName,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Feb-23 10:00:00", CultureInfo.InvariantCulture),
            Quantity = 5,
            GrossProceed = new() { Amount = new(500m) },
            Underlying = AssetName,
            StrikePrice = new WrappedMoney(100m),
            ExpiryDate = DateTime.Parse("01-Dec-23 00:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100
        };
        HoldingsService service = BuildService([shareTrade, optionTrade]);

        service.GetHolding(AssetName, new DateOnly(2023, 12, 31)).Quantity.ShouldBe(1000m);
    }
}
