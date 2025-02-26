using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport;

public static class Style
{
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
        if (section.Document is not null)
        {
            section.PageSetup = section.Document.DefaultPageSetup.Clone();
        }
        section.PageSetup.PageFormat = PageFormat.A4;
        return section.PageSetup.PageWidth.Point - section.PageSetup.LeftMargin.Point - section.PageSetup.RightMargin.Point;
    }
}
