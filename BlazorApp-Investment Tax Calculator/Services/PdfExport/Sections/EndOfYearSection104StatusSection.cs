using InvestmentTaxCalculator.Model.UkTaxModel;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class EndOfYearSection104StatusSection(UkSection104Pools ukSection104Pools) : ISection
{
    public string Name { get; set; } = "Section 104 Status";
    public string Title { get; set; } = "End of Tax Year Section 104 Status";

    public Section WriteSection(Section section, int taxYear)
    {
        Dictionary<string, Section104History> lastHistory = ukSection104Pools.GetEndOfYearSection104s(taxYear);

        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);

        if (lastHistory.Count == 0)
        {
            section.AddParagraph($"Section 104 is empty at the end of the tax year.");
            return section;
        }

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(20, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);

        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Name/Ticker");
        headerRow.Cells[1].AddParagraph("Date of last change");
        headerRow.Cells[2].AddParagraph("Quantity");
        headerRow.Cells[3].AddParagraph("Value");

        foreach (var history in lastHistory)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(history.Key);
            row.Cells[1].AddParagraph(history.Value.Date.ToShortDateString());
            row.Cells[2].AddParagraph(history.Value.NewQuantity.ToString());
            row.Cells[3].AddParagraph(history.Value.NewValue.ToString());
            if (history.Value.NewContractValue.Amount != 0)
            {
                Row contractValueRow = table.AddRow();
                contractValueRow.Cells[0].MergeRight = 2;
                contractValueRow.Cells[0].AddParagraph($"Total Contract Value: {history.Value.NewContractValue}");
            }
        }
        table.Format.SpaceAfter = Unit.FromPoint(20);
        return section;
    }
}

