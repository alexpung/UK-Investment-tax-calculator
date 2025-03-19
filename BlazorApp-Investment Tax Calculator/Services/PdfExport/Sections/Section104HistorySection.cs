using InvestmentTaxCalculator.Model.UkTaxModel;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class Section104HistorySection(UkSection104Pools ukSection104Pools) : ISection
{
    public string Name { get; set; } = "Section 104 History";
    public string Title { get; set; } = "Section 104 History change in tax year";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        List<UkSection104> ukSection104s = ukSection104Pools.GetActiveSection104s(taxYear);
        foreach (var ukSection104 in ukSection104s)
        {
            Paragraph tableSubheading = section.AddParagraph(ukSection104.AssetName);
            Style.StyleTableSubheading(tableSubheading);
            tableSubheading.Format.KeepWithNext = true;
            WriteSection104Table(section, ukSection104);
        }
        return section;
    }

    private static void WriteSection104Table(Section section, UkSection104 ukSection104)
    {
        List<(int, ParagraphAlignment)> tableColumns = [(7, ParagraphAlignment.Left),
            (5, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right),
            (7, ParagraphAlignment.Right),
            (8, ParagraphAlignment.Right),
            (8, ParagraphAlignment.Right)];
        bool extendFutureContractValueColumn = ukSection104.Section104HistoryList.Exists(history => history.NewContractValue.Amount != 0);
        if (extendFutureContractValueColumn)
        {
            tableColumns.Add((10, ParagraphAlignment.Right));
            tableColumns.Add((10, ParagraphAlignment.Right));
        }
        Table table = Style.CreateTableWithProportionedWidth(section, tableColumns);
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Date");
        headerRow.Cells[1].AddParagraph("Trade ID");
        headerRow.Cells[2].AddParagraph("New Quantity");
        headerRow.Cells[3].AddParagraph("ΔQuantity");
        headerRow.Cells[4].AddParagraph("New S104 Value");
        headerRow.Cells[5].AddParagraph("ΔValue");
        if (extendFutureContractValueColumn)
        {
            headerRow.Cells[6].AddParagraph("New Contract Value");
            headerRow.Cells[7].AddParagraph("ΔContract Value");
        }
        foreach (var section104History in ukSection104.Section104HistoryList)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(section104History.Date.ToString("dd/MM/yyyy"));
            row.Cells[1].AddParagraph(section104History.TradeTaxCalculation?.Id.ToString() ?? string.Empty);
            row.Cells[2].AddParagraph($"{section104History.NewQuantity:F2}");
            row.Cells[3].AddParagraph(section104History.QuantityChange.ToString("F2"));
            row.Cells[4].AddParagraph($"{section104History.NewValue}");
            row.Cells[5].AddParagraph(section104History.ValueChange.ToString());
            if (extendFutureContractValueColumn)
            {
                row.Cells[6].AddParagraph($"{section104History.NewContractValue}");
                row.Cells[7].AddParagraph(section104History.ContractValueChange.ToString());
            }
            row.KeepWith = 1;
            Row explanationRow = table.AddRow();
            explanationRow.Cells[0].MergeRight = 5;
            explanationRow.Cells[0].AddParagraph(section104History.Explanation);
        }
    }
}
