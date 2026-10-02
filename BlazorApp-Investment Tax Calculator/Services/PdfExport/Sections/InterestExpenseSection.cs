using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class InterestExpenseSection(DividendCalculationResult incomeCalculationResult) : ISection
{
    public const string ReferenceOnlyNote = "Note: Interest expenses are not deductible against investment income or capital gains. " +
        "They are not included in any summary or total in this report and are listed for reference only.";
    public string Name { get; set; } = "Interest Expenses";
    public string Title { get; set; } = "Interest Expenses (Reference Only)";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        Paragraph note = section.AddParagraph(ReferenceOnlyNote);
        note.Format.Font.Italic = true;
        note.Format.Font.Color = Style.MutedTextColour;
        note.Format.SpaceAfter = Unit.FromPoint(8);

        List<InterestIncome> expenses = incomeCalculationResult.DividendSummary
            .Where(i => i.TaxYear == taxYear)
            .SelectMany(summary => summary.InterestExpenses)
            .OrderBy(i => i.Date)
            .ToList();
        if (expenses.Count == 0)
        {
            section.AddParagraph($"No interest expenses recorded in the tax year {taxYear} - {taxYear + 1}.");
            return section;
        }

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (25, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Asset Name");
        headerRow.Cells[1].AddParagraph("Payment Date");
        headerRow.Cells[2].AddParagraph("Description");
        headerRow.Cells[3].AddParagraph("Amount");
        headerRow.Cells[4].AddParagraph("Sterling Amount");
        foreach (InterestIncome expense in expenses)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(expense.AssetName);
            row.Cells[1].AddParagraph(expense.Date.ToShortDateString());
            row.Cells[2].AddParagraph(expense.Amount.Description);
            row.Cells[3].AddParagraph(expense.Amount.Amount.ToString());
            row.Cells[4].AddParagraph(expense.Amount.BaseCurrencyAmount.ToString());
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Total interest expenses (not deductible)");
        totalRow.Cells[0].MergeRight = 2;
        totalRow.Cells[4].AddParagraph(expenses.Select(i => i.Amount.BaseCurrencyAmount).Sum().ToString());
        return section;
    }
}
