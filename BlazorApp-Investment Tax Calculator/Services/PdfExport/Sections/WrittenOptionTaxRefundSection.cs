using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class WrittenOptionTaxRefundSection(TradeCalculationResult tradeCalculationResult) : ISection
{
    public const string ExplanationNote = "Note: The full premium of a written option is taxed in the tax year the option was written. " +
        "When the option is closed, assigned or cash settled in a later tax year, the refund amount is not deducted in the yearly tax summary " +
        "and reduces the gain of the tax year the option was written. The actual reclaimable tax should be manually calculated based on your marginal tax rate for that tax year. " +
        "No refund is listed if the calculation deducts the closing cost of written options in the tax year the option was written.";
    public string Name { get; set; } = "Written option tax refund";
    public string Title { get; set; } = "Tax Refund for Written Options Closed in a Later Tax Year";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        Paragraph note = section.AddParagraph(ExplanationNote);
        note.Format.Font.Italic = true;
        note.Format.Font.Color = Style.MutedTextColour;
        note.Format.SpaceAfter = Unit.FromPoint(8);

        List<TaxRepay> taxRepays = [.. tradeCalculationResult.CalculatedTrade
            .OfType<OptionTradeTaxCalculation>()
            .SelectMany(trade => trade.TaxRepayList)];

        AddSubheading(section, $"Options written in the tax year {taxYear} - {taxYear + 1} and closed in a later tax year");
        AddTaxRepayTable(section, [.. taxRepays.Where(taxRepay => taxRepay.WrittenTaxYear == taxYear)],
            $"Total reduction to gain of the tax year {taxYear} - {taxYear + 1}");

        AddSubheading(section, $"Options closed in the tax year {taxYear} - {taxYear + 1} and written in an earlier tax year");
        AddTaxRepayTable(section, [.. taxRepays.Where(taxRepay => taxRepay.ClosingTaxYear == taxYear)],
            "Total reduction to gain of earlier tax years");
        return section;
    }

    private static void AddSubheading(Section section, string text)
    {
        Paragraph subheading = section.AddParagraph(text);
        Style.StyleTableSubheading(subheading);
        subheading.Format.KeepWithNext = true;
    }

    private static void AddTaxRepayTable(Section section, List<TaxRepay> taxRepays, string totalDescription)
    {
        if (taxRepays.Count == 0)
        {
            section.AddParagraph("None.");
            return;
        }
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (12, ParagraphAlignment.Right),
            (50, ParagraphAlignment.Left)]);
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Tax Year Written");
        headerRow.Cells[1].AddParagraph("Tax Year Closed");
        headerRow.Cells[2].AddParagraph("Refund Amount");
        headerRow.Cells[3].AddParagraph("Reason");
        foreach (TaxRepay taxRepay in taxRepays.OrderBy(taxRepay => taxRepay.WrittenTaxYear).ThenBy(taxRepay => taxRepay.ClosingTaxYear).ThenBy(taxRepay => taxRepay.Reason))
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph($"{taxRepay.WrittenTaxYear} - {taxRepay.WrittenTaxYear + 1}");
            row.Cells[1].AddParagraph($"{taxRepay.ClosingTaxYear} - {taxRepay.ClosingTaxYear + 1}");
            row.Cells[2].AddParagraph(taxRepay.RefundAmount.ToString());
            row.Cells[3].AddParagraph(taxRepay.Reason);
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph(totalDescription);
        totalRow.Cells[0].MergeRight = 1;
        totalRow.Cells[2].AddParagraph(taxRepays.Select(taxRepay => taxRepay.RefundAmount).Sum().ToString());
    }
}
