using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport;

public static class Style
{
    private const int _portraitPageWidth = 460;
    public static Paragraph StyleTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 14;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceAfter = 6;
        return paragraph;
    }

    public static void StyleBottomRowForSum(Row row)
    {
        row.Borders.Bottom.Width = 2;
        row.Borders.Bottom.Color = Colors.Black;
    }

    public static void StyleHeaderRow(Row row)
    {
        row.Shading.Color = Colors.LightBlue;
        row.Format.Font.Bold = true;
        row.HeadingFormat = true;
    }

    public static Table CreateTableWithProportionedWidth(Section section, List<int> columnProportionedWidth)
    {
        Table table = section.AddTable();
        double sectionWidth = _portraitPageWidth;
        foreach (var width in columnProportionedWidth)
        {
            table.AddColumn(Unit.FromPoint(sectionWidth * width / columnProportionedWidth.Sum()));
        }
        return table;
    }

    public static Table CreateTableWithProportionedWidth(Section section, List<(int, ParagraphAlignment)> columnProportionedWidthAndAlignment)
    {
        Table table = section.AddTable();
        double sectionWidth = _portraitPageWidth;
        int totalWidth = columnProportionedWidthAndAlignment.Sum(tuple => tuple.Item1);
        foreach (var (width, columnAlignment) in columnProportionedWidthAndAlignment)
        {
            Column column = table.AddColumn(Unit.FromPoint(sectionWidth * width / totalWidth));
            column.Format.Alignment = columnAlignment;
        }
        return table;
    }
}
