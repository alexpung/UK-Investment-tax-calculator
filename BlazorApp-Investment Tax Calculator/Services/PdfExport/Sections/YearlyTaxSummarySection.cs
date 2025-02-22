using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class YearlyTaxSummarySection(TaxYearCgtByTypeReportService taxYearCgtByTypeReportService) : ISection
{
    public string Name { get; set; } = "Tax Summary";
    public string Title { get; set; } = "Yearly Tax Summary";
    public Section WriteSection(Section section, int taxYear)
    {
        var report = taxYearCgtByTypeReportService.GetTaxYearCgtByTypeReport(taxYear);
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        Table table = section.AddTable();
        Column column1 = table.AddColumn(Unit.FromPoint(100));
        column1.Format.Alignment = ParagraphAlignment.Left;
        Column column2 = table.AddColumn(Unit.FromPoint(70));
        column2.Format.Alignment = ParagraphAlignment.Right;
        Column column3 = table.AddColumn(Unit.FromPoint(70));
        column3.Format.Alignment = ParagraphAlignment.Right;
        Column column4 = table.AddColumn(Unit.FromPoint(70));
        column4.Format.Alignment = ParagraphAlignment.Right;
        Column column5 = table.AddColumn(Unit.FromPoint(70));
        column5.Format.Alignment = ParagraphAlignment.Right;
        Column column6 = table.AddColumn(Unit.FromPoint(70));
        column6.Format.Alignment = ParagraphAlignment.Right;

        Row headerRow = table.AddRow();
        headerRow.Shading.Color = Colors.LightGray;
        headerRow.Cells[0].AddParagraph();
        headerRow.Cells[1].AddParagraph("Number of Disposals");
        headerRow.Cells[2].AddParagraph("Disposal Proceeds");
        headerRow.Cells[3].AddParagraph("Allowable costs");
        headerRow.Cells[4].AddParagraph("Gain excluding loss");
        headerRow.Cells[5].AddParagraph("Loss");
        Row listedSecuritiesRow = table.AddRow();
        listedSecuritiesRow.Cells[0].AddParagraph("Listed securities");
        listedSecuritiesRow.Cells[1].AddParagraph(report.ListedSecurityNumberOfDisposals.ToString());
        listedSecuritiesRow.Cells[2].AddParagraph(report.ListedSecurityDisposalProceeds.ToString());
        listedSecuritiesRow.Cells[3].AddParagraph(report.ListedSecurityAllowableCosts.ToString());
        listedSecuritiesRow.Cells[4].AddParagraph(report.ListedSecurityGainExcludeLoss.ToString());
        listedSecuritiesRow.Cells[5].AddParagraph(report.ListedSecurityLoss.ToString());
        Row otherAssetRow = table.AddRow();
        otherAssetRow.Cells[0].AddParagraph("Other assets");
        otherAssetRow.Cells[1].AddParagraph(report.OtherAssetsNumberOfDisposals.ToString());
        otherAssetRow.Cells[2].AddParagraph(report.OtherAssetsDisposalProceeds.ToString());
        otherAssetRow.Cells[3].AddParagraph(report.OtherAssetsAllowableCosts.ToString());
        otherAssetRow.Cells[4].AddParagraph(report.OtherAssetsGainExcludeLoss.ToString());
        otherAssetRow.Cells[5].AddParagraph(report.OtherAssetsLoss.ToString());
        otherAssetRow.Borders.Bottom.Width = 2;
        otherAssetRow.Borders.Bottom.Color = Colors.Black;
        Row sumRow = table.AddRow();
        sumRow.Cells[0].AddParagraph("Total").Format.Font.Bold = true;
        sumRow.Cells[1].AddParagraph((report.ListedSecurityNumberOfDisposals + report.OtherAssetsNumberOfDisposals).ToString()).Format.Font.Bold = true;
        sumRow.Cells[2].AddParagraph((report.ListedSecurityDisposalProceeds + report.OtherAssetsDisposalProceeds).ToString()).Format.Font.Bold = true;
        sumRow.Cells[3].AddParagraph((report.ListedSecurityAllowableCosts + report.OtherAssetsAllowableCosts).ToString()).Format.Font.Bold = true;
        sumRow.Cells[4].AddParagraph((report.ListedSecurityGainExcludeLoss + report.OtherAssetsGainExcludeLoss).ToString()).Format.Font.Bold = true;
        sumRow.Cells[5].AddParagraph((report.ListedSecurityLoss + report.OtherAssetsLoss).ToString()).Format.Font.Bold = true;
        return section;
    }
}
