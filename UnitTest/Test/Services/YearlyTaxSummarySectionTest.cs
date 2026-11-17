using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using UnitTest.Helper;

namespace UnitTest.Test.Services;

/// <summary>
/// The PDF's capital gains summary for a tax year in which nothing was traded, as when the year is only selected
/// for its dividend or interest income.
/// </summary>
public class YearlyTaxSummarySectionTest
{
    /// <summary>
    /// A £400 loss in 2022/23 (bought for £1,000, sold for £600) and no trades afterwards.
    /// </summary>
    private static (TradeCalculationResult Result, TaxYearReportService Service) CreateResultWithLossIn2022()
    {
        Trade buy = MockTrade.CreateMockTrade("ABC", new DateTime(2022, 5, 1), TradeType.ACQUISITION, 100, 1000m);
        Trade sell = MockTrade.CreateMockTrade("ABC", new DateTime(2022, 6, 1), TradeType.DISPOSAL, 100, 600m);
        List<ITradeTaxCalculation> calculations = TradeCalculationHelper.CalculateTrades([buy, sell], out _);
        UKTaxYear taxYear = new();
        TradeCalculationResult result = new(taxYear, new ResidencyStatusRecord());
        result.SetResult(calculations);
        return (result, new TaxYearReportService(result, taxYear));
    }

    [Fact]
    public void GetTaxYearReport_YearWithTrades_IsTheListedReport()
    {
        TaxYearReportService service = CreateResultWithLossIn2022().Service;

        TaxYearCgtReport report = service.GetTaxYearReport(2022);

        report.ShouldBe(service.GetTaxYearReports().Single());
        report.TotalLossInYear.Amount.ShouldBe(-400m);
    }

    [Fact]
    public void GetTaxYearReport_YearWithoutTrades_HasNoGainAndCarriesEarlierLosses()
    {
        TaxYearReportService service = CreateResultWithLossIn2022().Service;

        TaxYearCgtReport report = service.GetTaxYearReport(2024);

        report.TaxYear.ShouldBe(2024);
        report.TotalGainInYear.Amount.ShouldBe(0m);
        report.TotalLossInYear.Amount.ShouldBe(0m);
        report.NetCapitalGain.Amount.ShouldBe(0m);
        report.TaxableGainAfterAllowanceAndLossOffset.Amount.ShouldBe(0m);
        report.CapitalGainAllowance.Amount.ShouldBe(3000m);
        report.AvailableCapitalLossesFromPreviousYears.Amount.ShouldBe(400m);
        report.LossesAvailableToBroughtForward.Amount.ShouldBe(400m);
    }

    [Fact]
    public void GetTaxYearReport_YearBeforeAnyTrade_HasNoLossesCarried()
    {
        TaxYearReportService service = CreateResultWithLossIn2022().Service;

        service.GetTaxYearReport(2020).LossesAvailableToBroughtForward.Amount.ShouldBe(0m);
    }

    [Fact]
    public void WriteSection_YearWithoutTrades_WritesTheSummaryInsteadOfFailing()
    {
        (TradeCalculationResult result, TaxYearReportService service) = CreateResultWithLossIn2022();
        Section section = new Document().AddSection();

        new YearlyTaxSummarySection(result, service).WriteSection(section, 2024);

        string text = string.Join("\n", AllParagraphTexts(section));
        text.ShouldContain("Capital Gain Tax Summary");
        text.ShouldContain("Losses Available to Brought Forward");
        text.ShouldContain(new WrappedMoney(400m).ToString());
    }

    private static IEnumerable<string> AllParagraphTexts(DocumentObject parent)
    {
        switch (parent)
        {
            case Paragraph paragraph:
                yield return string.Concat(paragraph.Elements.OfType<Text>().Select(t => t.Content));
                break;
            case Section section:
                foreach (string text in section.Elements.Cast<DocumentObject>().SelectMany(AllParagraphTexts)) yield return text;
                break;
            case Table table:
                foreach (string text in table.Rows.Cast<Row>().SelectMany(row => row.Cells.Cast<Cell>()).SelectMany(AllParagraphTexts)) yield return text;
                break;
            case Cell cell:
                foreach (string text in cell.Elements.Cast<DocumentObject>().SelectMany(AllParagraphTexts)) yield return text;
                break;
        }
    }
}
