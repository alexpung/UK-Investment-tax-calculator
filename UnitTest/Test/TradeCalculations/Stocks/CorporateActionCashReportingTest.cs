using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class CorporateActionCashReportingTest
{
    [Fact]
    public void StockSplitCashInLieu_AppearsInTaxAndSection104Reports_WhenDeferralIsFalse()
    {
        ResidencyStatusRecord residencyStatus = new();
        Trade buy = new()
        {
            AssetName = "REPORT_SPLIT",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 95,
            GrossProceed = new() { Amount = new(950m) }
        };
        StockSplit split = new()
        {
            AssetName = "REPORT_SPLIT",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 10,
            CashInLieu = new DescribedMoney(6m, "GBP", 1m),
            ElectTaxDeferral = false
        };

        List<ITradeTaxCalculation> calculations = TradeCalculationHelper.CalculateTrades([buy, split], out UkSection104Pools pools, residencyStatus);

        string disposalReport = BuildDisposalReport(calculations, residencyStatus);
        disposalReport.ShouldContain("Corporate Action: REPORT_SPLIT undergoes a stock split 1 for 10 with cash-in-lieu");

        string section104Report = new UkSection104ExportService(pools).PrintToTextFile();
        section104Report.ShouldContain("Fractional shares (0.5000) removed for cash-in-lieu");
        section104Report.ShouldContain("Cash-in-lieu received:");
        section104Report.ShouldContain("Involved event: REPORT_SPLIT undergoes a stock split 1 for 10 with cash-in-lieu");
    }

    [Fact]
    public void TakeoverCashComponent_AppearsInSection104Report()
    {
        Trade buy = new()
        {
            AssetName = "OLDCO_REPORT",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) }
        };
        TakeoverCorporateAction takeover = new()
        {
            AssetName = "OLDCO_REPORT",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            AcquiringCompanyTicker = "NEWCO_REPORT",
            OldToNewRatio = 1m,
            CashComponent = new DescribedMoney(50m, "GBP", 1m),
            ElectTaxDeferral = false,
            NewSharesMarketValue = new DescribedMoney(2000m, "GBP", 1m)
        };

        _ = TradeCalculationHelper.CalculateTrades([buy, takeover], out UkSection104Pools pools);
        string section104Report = new UkSection104ExportService(pools).PrintToTextFile();

        section104Report.ShouldContain("Takeover by NEWCO_REPORT");
        section104Report.ShouldContain("Cash received:");
    }

    [Fact]
    public void SpinoffCashInLieu_AppearsInSection104Report()
    {
        Trade buy = new()
        {
            AssetName = "PARENT_REPORT",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) }
        };
        SpinoffCorporateAction spinoff = new()
        {
            AssetName = "PARENT_REPORT",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SpinoffCompanyTicker = "SPIN_REPORT",
            SpinoffSharesPerParentShare = 0.25m,
            ParentMarketValue = new DescribedMoney(800m, "GBP", 1m),
            SpinoffMarketValue = new DescribedMoney(200m, "GBP", 1m),
            CashInLieu = new DescribedMoney(50m, "GBP", 1m),
            ElectTaxDeferral = false
        };

        _ = TradeCalculationHelper.CalculateTrades([buy, spinoff], out UkSection104Pools pools);
        string section104Report = new UkSection104ExportService(pools).PrintToTextFile();

        section104Report.ShouldContain("Spinoff of SPIN_REPORT");
        section104Report.ShouldContain("Cash-in-lieu received:");
    }

    private static string BuildDisposalReport(List<ITradeTaxCalculation> calculations, ResidencyStatusRecord residencyStatus)
    {
        UKTaxYear taxYear = new();
        TradeCalculationResult tradeCalculationResult = new(taxYear, residencyStatus);
        tradeCalculationResult.SetResult(calculations);

        TaxYearCgtByTypeReportService byType = new(tradeCalculationResult, taxYear);
        TaxYearReportService byYear = new(tradeCalculationResult, taxYear);
        UkCalculationResultExportService export = new(taxYear, tradeCalculationResult, byType, byYear);
        return export.PrintToTextFile();
    }
}
