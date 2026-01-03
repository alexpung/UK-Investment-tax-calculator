using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class DividendSummarySection(DividendCalculationResult dividendCalculationResult) : ISection
{
    public string Name { get; set; } = "Dividend Summary";
    public string Title { get; set; } = "Dividend Summary";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        IEnumerable<DividendSummary> dividendSummaries = dividendCalculationResult.DividendSummary.Where(i => i.TaxYear == taxYear);
        if (!dividendSummaries.Any() || dividendSummaries.First().RelatedDividendsAndTaxes.Count == 0)
        {
            section.AddParagraph($"No dividends received in the tax year {taxYear} - {taxYear + 1}.");
            return section;
        }

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(20, ParagraphAlignment.Left),
            (20, ParagraphAlignment.Right),
            (20, ParagraphAlignment.Right)]);

        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Region");
        headerRow.Cells[1].AddParagraph("Gross Dividend Received");
        headerRow.Cells[2].AddParagraph("Withholding Tax Paid");

        foreach (var summary in dividendSummaries)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph($"{summary.CountryOfOrigin.CountryName} ({summary.CountryOfOrigin.ThreeDigitCode})");
            row.Cells[1].AddParagraph(summary.TotalTaxableDividend.ToString());
            row.Cells[2].AddParagraph(summary.TotalForeignTaxPaid.ToString());
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Total");
        totalRow.Cells[1].AddParagraph(dividendCalculationResult.GetTotalDividend([taxYear]).ToString());
        totalRow.Cells[2].AddParagraph(dividendCalculationResult.GetForeignTaxPaid([taxYear]).ToString());

        foreach (var summary in dividendSummaries)
        {
            if (summary.RelatedDividendsAndTaxes.Count == 0)
            {
                continue;
            }
            Paragraph regionTableTitle = section.AddParagraph($"Dividend detail for {summary.CountryOfOrigin.CountryName} ({summary.CountryOfOrigin.ThreeDigitCode})");
            Style.StyleTableSubheading(regionTableTitle);
            Table dividendDetailTable = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (25, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
            headerRow = dividendDetailTable.AddRow();
            Style.StyleHeaderRow(headerRow);
            headerRow.Cells[0].AddParagraph("Payment Date");
            headerRow.Cells[1].AddParagraph("Description");
            headerRow.Cells[2].AddParagraph("Type");
            headerRow.Cells[3].AddParagraph("Dividend Received");
            headerRow.Cells[4].AddParagraph("Withholding Tax Paid");
            foreach (var dividend in summary.RelatedDividendsAndTaxes)
            {
                Row dividendRow = dividendDetailTable.AddRow();
                dividendRow.Cells[0].AddParagraph(dividend.Date.ToShortDateString());
                dividendRow.Cells[1].AddParagraph(dividend.Proceed.Description);
                dividendRow.Cells[2].AddParagraph(dividend.DividendType.GetDescription());
                dividendRow.Cells[3].AddParagraph(dividend.DividendReceived.ToString());
                dividendRow.Cells[4].AddParagraph(dividend.WithholdingTaxPaid.ToString());
            }
            totalRow = dividendDetailTable.AddRow();
            Style.StyleSumRow(totalRow);
            totalRow.Cells[0].AddParagraph("Total");
            totalRow.Cells[3].AddParagraph(summary.TotalTaxableDividend.ToString());
            totalRow.Cells[4].AddParagraph(summary.TotalForeignTaxPaid.ToString());
        }
        return section;
    }
}
