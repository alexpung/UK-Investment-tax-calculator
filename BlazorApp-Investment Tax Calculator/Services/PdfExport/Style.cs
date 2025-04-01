using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport;

public static class Style
{

    public static Paragraph StyleTopTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 16;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceAfter = 6;
        return paragraph;
    }
    public static Paragraph StyleTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 12;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceAfter = 6;
        return paragraph;
    }

    public static void StyleSumRow(Row row)
    {
        row.Borders.Top.Width = 2;
        row.Borders.Top.Color = Colors.Black;
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
        double sectionWidth = GetSectionWidth(section);
        foreach (var width in columnProportionedWidth)
        {
            table.AddColumn(Unit.FromPoint(sectionWidth * width / columnProportionedWidth.Sum()));
        }
        return table;
    }

    public static Table CreateTableWithProportionedWidth(Section section, List<(int, ParagraphAlignment)> columnProportionedWidthAndAlignment)
    {
        Table table = section.AddTable();
        double sectionWidth = GetSectionWidth(section);
        int totalWidth = columnProportionedWidthAndAlignment.Sum(tuple => tuple.Item1);
        foreach (var (width, columnAlignment) in columnProportionedWidthAndAlignment)
        {
            Column column = table.AddColumn(Unit.FromPoint(sectionWidth * width / totalWidth));
            column.Format.Alignment = columnAlignment;
        }
        table.LeftPadding = Unit.FromPoint(2);
        table.RightPadding = Unit.FromPoint(2);
        table.Borders.Bottom.Width = 0.25;
        table.Borders.Bottom.Color = Colors.LightGray;
        return table;
    }

    public static void StyleTableSubheading(Paragraph paragraph)
    {
        paragraph.Format.Font.Color = Colors.DarkBlue;
        paragraph.Format.Font.Size = 14;
        paragraph.Format.SpaceAfter = Unit.FromPoint(10);
    }

    private static double GetSectionWidth(Section section)
    {
        return section.PageSetup.PageWidth.Point - section.PageSetup.LeftMargin.Point - section.PageSetup.RightMargin.Point;
    }
}
