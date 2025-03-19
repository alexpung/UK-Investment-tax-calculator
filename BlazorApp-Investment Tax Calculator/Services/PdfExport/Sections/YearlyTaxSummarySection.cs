using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Globalization;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class YearlyTaxSummarySection(TradeCalculationResult tradeCalculationResult, TaxYearReportService taxYearReportService) : ISection
{
    public string Name { get; set; } = "Tax Summary";
    public string Title { get; set; } = "Yearly Tax Summary";
    public Section WriteSection(Section section, int taxYear)
    {
        TaxYearCgtReport taxYearCgtReport = taxYearReportService.GetTaxYearReports().First(report => report.TaxYear == taxYear);
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        WriteTaxYearCgtByTypeTable(section, tradeCalculationResult, taxYear);
        WriteTaxYearCapitalGainTable(section, taxYearCgtReport);
        return section;
    }

    /// <summary>
    /// Constructs and adds a capital gains tax table, organized by asset type, to the provided PDF section for the specified tax year.
    /// </summary>
    /// <param name="section">The PDF section to which the table is added.</param>
    /// <param name="tradeCalculationResult">The tax calculation data used to extract asset disposal and gain/loss statistics.</param>
    /// <param name="taxYear">The tax year for which the tax summary is generated.</param>
    /// <remarks>
    /// The table is created with predetermined column widths and a header row displaying tax-related metrics. Rows are then added
    /// for each asset category classified as either Listed Shares or Other Assets. For each applicable asset type that has disposals,
    /// detailed statistics are appended by invoking helper methods. After processing each asset group, a summary row is added to
    /// display cumulative totals, and spacing is applied below the table.
    /// </remarks>
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
        table.Format.SpaceAfter = Unit.FromPoint(20);
    }

    /// <summary>
    /// Adds a row with tax summary statistics for the specified asset category and tax year to the table.
    /// </summary>
    /// <remarks>
    /// The method checks the number of disposals for the given asset category and tax year in the trade calculation result.
    /// If the data is missing or the number of disposals is zero, no row is added. Otherwise, a row is created displaying the asset
    /// category's description, disposal count, disposal proceeds, allowable costs, total gain, and total loss.
    /// </remarks>
    /// <param name="assetCategoryType">The asset category being processed (e.g., listed shares or other assets).</param>
    /// <param name="tradeCalculationResult">The results containing tax and trade statistics needed to build the summary.</param>
    /// <param name="table">The table to which the new row with statistics will be added.</param>
    /// <param name="taxYear">The tax year for which the statistics are being generated.</param>
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

    /// <summary>
    /// Writes a table of capital gains tax details for the tax year into the specified PDF section.
    /// </summary>
    /// <remarks>
    /// The method creates a two-column table that lists key financial figures such as total gain (excluding losses), total loss, net capital gain, 
    /// capital gain allowance, CGT allowance brought forward and used, taxable gain after allowance and loss offset, and losses available to be brought forward. 
    /// Monetary values are formatted as currency using the en-GB culture, and some summary rows receive additional styling.
    /// </remarks>
    /// <param name="section">The PDF section to which the capital gains table is added.</param>
    /// <param name="taxYearCgtReport">A report object containing the tax year's capital gains data.</param>
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
