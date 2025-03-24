using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Globalization;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class YearlyTaxSummarySection(TradeCalculationResult tradeCalculationResult, TaxYearReportService taxYearReportService) : ISection
{
    public string Name { get; set; } = "Capital Gain Tax Summary";
    public string Title { get; set; } = "Capital Gain Tax Summary";
    public Section WriteSection(Section section, int taxYear)
    {
        TaxYearCgtReport taxYearCgtReport = taxYearReportService.GetTaxYearReports().First(report => report.TaxYear == taxYear);
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        WriteTaxYearCgtByTypeTable(section, tradeCalculationResult, taxYear);
        WriteTaxYearCapitalGainTable(section, taxYearCgtReport);
        return section;
    }

    private static void WriteTaxYearCgtByTypeTable(Section section, TradeCalculationResult tradeCalculationResult, int taxYear)
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
        foreach (AssetCategoryType assetType in Enum.GetValues(typeof(AssetCategoryType)))
        {
            if (assetType.GetHmrcAssetCategoryType() == AssetGroupType.LISTEDSHARES)
            {
                PrintAssetTypeStats(assetType, tradeCalculationResult, table, taxYear);
            }

        }
        Row? bottomRow = table.Rows.LastObject as Row;
        if (bottomRow is not null)
        {
            Style.StyleBottomRowForSum(bottomRow);
            Row sumRow = table.AddRow();
            sumRow.Cells[0].AddParagraph("Listed Shares Total").Format.Font.Bold = true;
            sumRow.Cells[1].AddParagraph(tradeCalculationResult.GetNumberOfDisposals([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[2].AddParagraph(tradeCalculationResult.GetDisposalProceeds([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[3].AddParagraph(tradeCalculationResult.GetAllowableCosts([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[4].AddParagraph(tradeCalculationResult.GetTotalGain([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[5].AddParagraph(tradeCalculationResult.GetTotalLoss([taxYear], AssetGroupType.LISTEDSHARES).ToString());
        }
        foreach (AssetCategoryType assetType in Enum.GetValues(typeof(AssetCategoryType)))
        {
            if (assetType.GetHmrcAssetCategoryType() == AssetGroupType.OTHERASSETS)
            {
                PrintAssetTypeStats(assetType, tradeCalculationResult, table, taxYear);
            }
        }
        bottomRow = table.Rows.LastObject as Row;
        if (bottomRow is not null)
        {
            Style.StyleBottomRowForSum(bottomRow);
            Row sumRow = table.AddRow();
            sumRow.Cells[0].AddParagraph("Other Assets Total").Format.Font.Bold = true;
            sumRow.Cells[1].AddParagraph(tradeCalculationResult.GetNumberOfDisposals([taxYear], AssetGroupType.OTHERASSETS).ToString());
            sumRow.Cells[2].AddParagraph(tradeCalculationResult.GetDisposalProceeds([taxYear], AssetGroupType.OTHERASSETS).ToString());
            sumRow.Cells[3].AddParagraph(tradeCalculationResult.GetAllowableCosts([taxYear], AssetGroupType.OTHERASSETS).ToString());
            sumRow.Cells[4].AddParagraph(tradeCalculationResult.GetTotalGain([taxYear], AssetGroupType.OTHERASSETS).ToString());
            sumRow.Cells[5].AddParagraph(tradeCalculationResult.GetTotalLoss([taxYear], AssetGroupType.OTHERASSETS).ToString());
        }
    }

    private static void PrintAssetTypeStats(AssetCategoryType assetCategoryType, TradeCalculationResult tradeCalculationResult, Table table, int taxYear)
    {
        if (!tradeCalculationResult.NumberOfDisposals.TryGetValue((taxYear, assetCategoryType), out int numOfDisposal)) return;
        if (numOfDisposal == 0) return;
        Row row = table.AddRow();
        row.Cells[0].AddParagraph(assetCategoryType.GetDescription());
        row.Cells[1].AddParagraph(tradeCalculationResult.NumberOfDisposals[(taxYear, assetCategoryType)].ToString());
        row.Cells[2].AddParagraph(tradeCalculationResult.DisposalProceeds[(taxYear, assetCategoryType)].ToString());
        row.Cells[3].AddParagraph(tradeCalculationResult.AllowableCosts[(taxYear, assetCategoryType)].ToString());
        row.Cells[4].AddParagraph(tradeCalculationResult.TotalGain[(taxYear, assetCategoryType)].ToString());
        row.Cells[5].AddParagraph(tradeCalculationResult.TotalLoss[(taxYear, assetCategoryType)].ToString());
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
