using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Globalization;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class YearlyTaxSummarySection(TaxYearCgtByTypeReportService taxYearCgtByTypeReportService, TaxYearReportService taxYearReportService) : ISection
{
    public string Name { get; set; } = "Tax Summary";
    public string Title { get; set; } = "Yearly Tax Summary";
    public Section WriteSection(Section section, int taxYear)
    {
        TaxYearCgtByTypeReport taxYearCgtByTypeReport = taxYearCgtByTypeReportService.GetTaxYearCgtByTypeReport(taxYear);
        TaxYearCgtReport taxYearCgtReport = taxYearReportService.GetTaxYearReports().First(report => report.TaxYear == taxYear);
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        WriteTaxYearCgtByTypeTable(section, taxYearCgtByTypeReport);
        WriteTaxYearCapitalGainTable(section, taxYearCgtReport);
        return section;
    }

    private static void WriteTaxYearCgtByTypeTable(Section section, TaxYearCgtByTypeReport report)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (7, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right)]);
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph();
        headerRow.Cells[1].AddParagraph("Number of Disposals");
        headerRow.Cells[2].AddParagraph("Disposal Proceeds");
        headerRow.Cells[3].AddParagraph("Allowable costs");
        headerRow.Cells[4].AddParagraph("Gain excluding loss");
        headerRow.Cells[5].AddParagraph("Loss");
        Row listedSecuritiesRow = table.AddRow();
        listedSecuritiesRow.Cells[0].AddParagraph("Listed securities");
        listedSecuritiesRow.Cells[1].AddParagraph(report.ListedSecurityNumberOfDisposals.ToString());
        listedSecuritiesRow.Cells[2].AddParagraph(report.ListedSecurityDisposalProceeds.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        listedSecuritiesRow.Cells[3].AddParagraph(report.ListedSecurityAllowableCosts.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        listedSecuritiesRow.Cells[4].AddParagraph(report.ListedSecurityGainExcludeLoss.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        listedSecuritiesRow.Cells[5].AddParagraph(report.ListedSecurityLoss.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Row otherAssetRow = table.AddRow();
        otherAssetRow.Cells[0].AddParagraph("Other assets");
        otherAssetRow.Cells[1].AddParagraph(report.OtherAssetsNumberOfDisposals.ToString());
        otherAssetRow.Cells[2].AddParagraph(report.OtherAssetsDisposalProceeds.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        otherAssetRow.Cells[3].AddParagraph(report.OtherAssetsAllowableCosts.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        otherAssetRow.Cells[4].AddParagraph(report.OtherAssetsGainExcludeLoss.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        otherAssetRow.Cells[5].AddParagraph(report.OtherAssetsLoss.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Style.StyleBottomRowForSum(otherAssetRow);
        Row sumRow = table.AddRow();
        sumRow.Cells[0].AddParagraph("Total").Format.Font.Bold = true;
        sumRow.Cells[1].AddParagraph((report.ListedSecurityNumberOfDisposals + report.OtherAssetsNumberOfDisposals).ToString()).Format.Font.Bold = true;
        sumRow.Cells[2].AddParagraph((report.ListedSecurityDisposalProceeds + report.OtherAssetsDisposalProceeds).ToString("C", CultureInfo.CreateSpecificCulture("en-GB"))).Format.Font.Bold = true;
        sumRow.Cells[3].AddParagraph((report.ListedSecurityAllowableCosts + report.OtherAssetsAllowableCosts).ToString("C", CultureInfo.CreateSpecificCulture("en-GB"))).Format.Font.Bold = true;
        sumRow.Cells[4].AddParagraph((report.ListedSecurityGainExcludeLoss + report.OtherAssetsGainExcludeLoss).ToString("C", CultureInfo.CreateSpecificCulture("en-GB"))).Format.Font.Bold = true;
        sumRow.Cells[5].AddParagraph((report.ListedSecurityLoss + report.OtherAssetsLoss).ToString("C", CultureInfo.CreateSpecificCulture("en-GB"))).Format.Font.Bold = true;
        table.Format.SpaceAfter = Unit.FromPoint(20);
    }

    private static void WriteTaxYearCapitalGainTable(Section section, TaxYearCgtReport taxYearCgtReport)
    {
        Table table = Style.CreateTableWithProportionedWidth(section, [(250, ParagraphAlignment.Left), (200, ParagraphAlignment.Right)]);
        Row totalGainRow = table.AddRow();
        totalGainRow.Cells[0].AddParagraph("Total Gain excluding loss");
        totalGainRow.Cells[1].AddParagraph(taxYearCgtReport.TotalGainInYear.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Row totalLossRow = table.AddRow();
        totalLossRow.Cells[0].AddParagraph("Total Loss");
        totalLossRow.Cells[1].AddParagraph(taxYearCgtReport.TotalLossInYear.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Style.StyleBottomRowForSum(totalLossRow);
        Row netCapitalGainRow = table.AddRow();
        netCapitalGainRow.Cells[0].AddParagraph("Net Capital Gain");
        netCapitalGainRow.Cells[1].AddParagraph(taxYearCgtReport.NetCapitalGain.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Row capitalGainAllowanceRow = table.AddRow();
        capitalGainAllowanceRow.Cells[0].AddParagraph("Capital Gain Allowance");
        capitalGainAllowanceRow.Cells[1].AddParagraph(taxYearCgtReport.CapitalGainAllowance.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Row cgtAllowanceBroughtForwardAndUsedRow = table.AddRow();
        cgtAllowanceBroughtForwardAndUsedRow.Cells[0].AddParagraph("CGT Allowance Brought Forward and Used");
        cgtAllowanceBroughtForwardAndUsedRow.Cells[1].AddParagraph(taxYearCgtReport.CgtAllowanceBroughtForwardAndUsed.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Row taxableGainAfterAllowanceAndLossOffsetRow = table.AddRow();
        taxableGainAfterAllowanceAndLossOffsetRow.Cells[0].AddParagraph("Taxable Gain after Allowance and Loss Offset");
        taxableGainAfterAllowanceAndLossOffsetRow.Cells[1].AddParagraph(taxYearCgtReport.TaxableGainAfterAllowanceAndLossOffset.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
        Style.StyleBottomRowForSum(taxableGainAfterAllowanceAndLossOffsetRow);
        Row lossesAvailableToBroughtForwardRow = table.AddRow();
        lossesAvailableToBroughtForwardRow.Cells[0].AddParagraph("Losses Available to Brought Forward");
        lossesAvailableToBroughtForwardRow.Cells[1].AddParagraph(taxYearCgtReport.LossesAvailableToBroughtForward.ToString("C", CultureInfo.CreateSpecificCulture("en-GB")));
    }
}
