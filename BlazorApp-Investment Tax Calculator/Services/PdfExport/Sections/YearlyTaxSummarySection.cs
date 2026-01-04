using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

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
        section.AddParagraph();
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
        foreach (AssetCategoryType assetType in Enum.GetValues<AssetCategoryType>())
        {
            if (assetType.GetHmrcAssetCategoryType() == AssetGroupType.LISTEDSHARES)
            {
                PrintAssetTypeStats(assetType, tradeCalculationResult, table, taxYear);
            }

        }
        Row? bottomRow = table.Rows.LastObject as Row;
        if (bottomRow is not null)
        {
            Row sumRow = table.AddRow();
            Style.StyleSumRow(sumRow);
            sumRow.Cells[0].AddParagraph("Listed Shares Total").Format.Font.Bold = true;
            sumRow.Cells[1].AddParagraph(tradeCalculationResult.GetNumberOfDisposals([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[2].AddParagraph(tradeCalculationResult.GetDisposalProceeds([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[3].AddParagraph(tradeCalculationResult.GetAllowableCosts([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[4].AddParagraph(tradeCalculationResult.GetTotalGain([taxYear], AssetGroupType.LISTEDSHARES).ToString());
            sumRow.Cells[5].AddParagraph(tradeCalculationResult.GetTotalLoss([taxYear], AssetGroupType.LISTEDSHARES).ToString());
        }
        table.AddRow(); // Empty row between sections
        foreach (AssetCategoryType assetType in Enum.GetValues<AssetCategoryType>())
        {
            if (assetType.GetHmrcAssetCategoryType() == AssetGroupType.OTHERASSETS)
            {
                PrintAssetTypeStats(assetType, tradeCalculationResult, table, taxYear);
            }
        }
        bottomRow = table.Rows.LastObject as Row;
        if (bottomRow is not null)
        {
            Row sumRow = table.AddRow();
            Style.StyleSumRow(sumRow);
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
        Table containerTable = section.AddTable();
        containerTable.Borders.Width = 0;
        containerTable.AddColumn("10cm");
        containerTable.AddColumn("1.7cm");  // The "Gutter" space
        containerTable.AddColumn("13cm");
        Row containerRow = containerTable.AddRow();

        Table leftTable = new();
        leftTable.AddColumn("5cm");
        var numberRowLeft = leftTable.AddColumn("5cm");
        numberRowLeft.Format.Alignment = ParagraphAlignment.Right;
        Row totalGainRow = leftTable.AddRow();
        totalGainRow.Cells[0].AddParagraph("Total Gain excluding loss");
        totalGainRow.Cells[1].AddParagraph(taxYearCgtReport.TotalGainInYear.ToString());
        Row totalLossRow = leftTable.AddRow();
        totalLossRow.Cells[0].AddParagraph("Total Loss");
        totalLossRow.Cells[1].AddParagraph(taxYearCgtReport.TotalLossInYear.ToString());
        Row netCapitalGainRow = leftTable.AddRow();
        Style.StyleSumRow(netCapitalGainRow);
        netCapitalGainRow.Cells[0].AddParagraph("Net Capital Gain");
        netCapitalGainRow.Cells[1].AddParagraph(taxYearCgtReport.NetCapitalGain.ToString());
        containerRow.Cells[0].Elements.Add(leftTable.Clone());

        Table rightTable = new();
        rightTable.AddColumn("8cm");
        var numberRowRight = rightTable.AddColumn("5cm");
        numberRowRight.Format.Alignment = ParagraphAlignment.Right;

        if (taxYearCgtReport.NetCapitalGain.Amount <= 0)
        {
            WriteCapitalLossBroughtForwardTable(rightTable, taxYearCgtReport);
        }
        else
        {
            WriteCapitalGainAllowanceTable(rightTable, taxYearCgtReport);
        }
        containerRow.Cells[2].Elements.Add(rightTable.Clone());
    }

    private static void WriteCapitalLossBroughtForwardTable(Table table, TaxYearCgtReport taxYearCgtReport)
    {
        Row AvailableLossRow = table.AddRow();
        AvailableLossRow.Cells[0].AddParagraph("Available Loss in previous years");
        AvailableLossRow.Cells[1].AddParagraph(taxYearCgtReport.AvailableCapitalLossesFromPreviousYears.ToString());
        Row netCapitalGainRow = table.AddRow();
        netCapitalGainRow.Cells[0].AddParagraph("Net Capital Loss");
        netCapitalGainRow.Cells[1].AddParagraph((taxYearCgtReport.NetCapitalGain * -1).ToString());
        Row lossesAvailableToBroughtForwardRow = table.AddRow();
        Style.StyleSumRow(lossesAvailableToBroughtForwardRow);
        lossesAvailableToBroughtForwardRow.Cells[0].AddParagraph("Losses Available to Brought Forward");
        lossesAvailableToBroughtForwardRow.Cells[1].AddParagraph(taxYearCgtReport.LossesAvailableToBroughtForward.ToString());
    }

    private static void WriteCapitalGainAllowanceTable(Table table, TaxYearCgtReport taxYearCgtReport)
    {
        Row netCapitalGainRow = table.AddRow();
        netCapitalGainRow.Cells[0].AddParagraph("Net Capital Gain");
        netCapitalGainRow.Cells[1].AddParagraph(taxYearCgtReport.NetCapitalGain.ToString());
        Row capitalGainAllowanceRow = table.AddRow();
        capitalGainAllowanceRow.Cells[0].AddParagraph("Capital Gain Allowance");
        capitalGainAllowanceRow.Cells[1].AddParagraph(taxYearCgtReport.CapitalGainAllowance.ToString());
        Row cgtAllowanceBroughtForwardAndUsedRow = table.AddRow();
        cgtAllowanceBroughtForwardAndUsedRow.Cells[0].AddParagraph("CGT Allowance Brought Forward and Used");
        cgtAllowanceBroughtForwardAndUsedRow.Cells[1].AddParagraph(taxYearCgtReport.CgtAllowanceBroughtForwardAndUsed.ToString());
        Row taxableGainAfterAllowanceAndLossOffsetRow = table.AddRow();
        Style.StyleSumRow(taxableGainAfterAllowanceAndLossOffsetRow);
        taxableGainAfterAllowanceAndLossOffsetRow.Cells[0].AddParagraph("Taxable Gain after Allowance and Loss Offset");
        taxableGainAfterAllowanceAndLossOffsetRow.Cells[1].AddParagraph(taxYearCgtReport.TaxableGainAfterAllowanceAndLossOffset.ToString());
        table.AddRow(); // Empty row
        Row AvailableLossRow = table.AddRow();
        AvailableLossRow.Cells[0].AddParagraph("Available Loss in previous years");
        AvailableLossRow.Cells[1].AddParagraph(taxYearCgtReport.AvailableCapitalLossesFromPreviousYears.ToString());
        Row cgtAllowanceBroughtForwardAndUsedRow2 = table.AddRow();
        cgtAllowanceBroughtForwardAndUsedRow2.Cells[0].AddParagraph("CGT Allowance Brought Forward and Used");
        cgtAllowanceBroughtForwardAndUsedRow2.Cells[1].AddParagraph(taxYearCgtReport.CgtAllowanceBroughtForwardAndUsed.ToString());
        Row lossesAvailableToBroughtForwardRow = table.AddRow();
        Style.StyleSumRow(lossesAvailableToBroughtForwardRow);
        lossesAvailableToBroughtForwardRow.Cells[0].AddParagraph("Losses Available to Brought Forward");
        lossesAvailableToBroughtForwardRow.Cells[1].AddParagraph(taxYearCgtReport.LossesAvailableToBroughtForward.ToString());
    }
}
