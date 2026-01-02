using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class InterestIncomeSummarySection(DividendCalculationResult incomeCalculationResult) : ISection
{
    public string Name { get; set; } = "Interest Income Summary";
    public string Title { get; set; } = "Interest Income Summary";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        IEnumerable<DividendSummary> incomeSummaries = incomeCalculationResult.DividendSummary.Where(i => i.TaxYear == taxYear);
        if (!incomeSummaries.Any() || incomeSummaries.First().RelatedInterestIncome.Count == 0)
        {
            section.AddParagraph($"No interest income received in the tax year {taxYear}.");
            return section;
        }

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(20, ParagraphAlignment.Left),
            (20, ParagraphAlignment.Right),
            (20, ParagraphAlignment.Right),
            (20, ParagraphAlignment.Right),
            (20, ParagraphAlignment.Right),
            (20, ParagraphAlignment.Right)]);

        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Region");
        headerRow.Cells[1].AddParagraph("Bond interest received");
        headerRow.Cells[2].AddParagraph("Saving interest received");
        headerRow.Cells[3].AddParagraph("Income profit accurred");
        headerRow.Cells[4].AddParagraph("Income loss accurred");
        headerRow.Cells[5].AddParagraph("Total interest income taxible");

        foreach (var summary in incomeSummaries)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph($"{summary.CountryOfOrigin.CountryName} ({summary.CountryOfOrigin.ThreeDigitCode})");
            row.Cells[1].AddParagraph(summary.TotalTaxableBondInterest.ToString());
            row.Cells[2].AddParagraph(summary.TotalTaxableSavingInterest.ToString());
            row.Cells[3].AddParagraph(summary.TotalAccurredIncomeProfit.ToString());
            row.Cells[4].AddParagraph(summary.TotalAccurredIncomeLoss.ToString());
            row.Cells[5].AddParagraph(summary.TotalInterestIncome.ToString());
        }

        foreach (var summary in incomeSummaries)
        {
            if (summary.RelatedInterestIncome.Count == 0)
            {
                continue;
            }
            Paragraph regionTableTitle = section.AddParagraph($"Interest income detail for {summary.CountryOfOrigin.CountryName} ({summary.CountryOfOrigin.ThreeDigitCode})");
            Style.StyleTableSubheading(regionTableTitle);
            Table incomeDetailTable = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (25, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right)]);
            headerRow = incomeDetailTable.AddRow();
            Style.StyleHeaderRow(headerRow);
            headerRow.Cells[0].AddParagraph("Asset Name");
            headerRow.Cells[1].AddParagraph("Payment Date");
            headerRow.Cells[2].AddParagraph("Description");
            headerRow.Cells[3].AddParagraph("Type");
            headerRow.Cells[4].AddParagraph("Interest Received");
            foreach (var income in summary.RelatedInterestIncome)
            {
                Row incomeRow = incomeDetailTable.AddRow();
                incomeRow.Cells[0].AddParagraph(income.AssetName);
                incomeRow.Cells[1].AddParagraph(income.Date.ToShortDateString());
                incomeRow.Cells[2].AddParagraph(income.Amount.Description);
                incomeRow.Cells[3].AddParagraph(income.InterestType.GetDescription());
                incomeRow.Cells[4].AddParagraph(income.Amount.Display());
            }
            Row totalRow = incomeDetailTable.AddRow();
            Style.StyleSumRow(totalRow);
            totalRow.Cells[0].AddParagraph("Total interest income");
            totalRow.Cells[4].AddParagraph(summary.TotalInterestIncome.ToString());
        }
        return section;
    }
}
