using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport;

public static class Style
{
    // Report colour palette
    public static readonly Color PrimaryColour = Color.FromRgb(0x1F, 0x38, 0x64);       // dark navy
    public static readonly Color AccentColour = Color.FromRgb(0x2E, 0x74, 0xB5);        // mid blue
    public static readonly Color SumRowShadingColour = Color.FromRgb(0xE9, 0xEF, 0xF7); // pale blue
    public static readonly Color SubRowShadingColour = Color.FromRgb(0xF2, 0xF4, 0xF8); // light neutral
    public static readonly Color TableBorderColour = Color.FromRgb(0xC9, 0xD3, 0xE0);   // soft grey-blue
    public static readonly Color MutedTextColour = Color.FromRgb(0x59, 0x59, 0x59);     // dark grey

    public static Paragraph StyleTopTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 22;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = PrimaryColour;
        paragraph.Format.SpaceAfter = Unit.FromPoint(2);
        return paragraph;
    }

    public static Paragraph StyleTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 14;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = PrimaryColour;
        paragraph.Format.SpaceAfter = Unit.FromPoint(10);
        paragraph.Format.Borders.Bottom = new Border { Width = Unit.FromPoint(1), Color = AccentColour };
        paragraph.Format.Borders.DistanceFromBottom = Unit.FromPoint(4);
        paragraph.Format.KeepWithNext = true;
        return paragraph;
    }

    public static void StyleSumRow(Row row)
    {
        row.Borders.Top.Width = Unit.FromPoint(1);
        row.Borders.Top.Color = PrimaryColour;
        row.Shading.Color = SumRowShadingColour;
        row.Format.Font.Bold = true;
    }

    public static void StyleHeaderRow(Row row)
    {
        row.Shading.Color = PrimaryColour;
        row.Format.Font.Color = Colors.White;
        row.Format.Font.Bold = true;
        row.HeadingFormat = true;
        row.VerticalAlignment = VerticalAlignment.Center;
    }

    public static Table CreateTableWithProportionedWidth(Section section, List<int> columnProportionedWidth)
    {
        Table table = section.AddTable();
        ApplyTableStyle(table);
        double sectionWidth = GetContentWidth(section);
        foreach (var width in columnProportionedWidth)
        {
            table.AddColumn(Unit.FromPoint(sectionWidth * width / columnProportionedWidth.Sum()));
        }
        return table;
    }

    public static Table CreateTableWithProportionedWidth(Section section, List<(int, ParagraphAlignment)> columnProportionedWidthAndAlignment)
    {
        Table table = section.AddTable();
        ApplyTableStyle(table);
        double sectionWidth = GetContentWidth(section);
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
        paragraph.Format.Font.Color = AccentColour;
        paragraph.Format.Font.Size = 11;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceBefore = Unit.FromPoint(10);
        paragraph.Format.SpaceAfter = Unit.FromPoint(6);
    }

    private static void ApplyTableStyle(Table table)
    {
        table.LeftPadding = Unit.FromPoint(4);
        table.RightPadding = Unit.FromPoint(4);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);
        table.Borders.Bottom.Width = Unit.FromPoint(0.25);
        table.Borders.Bottom.Color = TableBorderColour;
    }

    public static double GetContentWidth(Section section)
    {
        PageSetup pageSetup = section.PageSetup;
        double pageWidth = pageSetup.Orientation == Orientation.Landscape ? pageSetup.PageHeight.Point : pageSetup.PageWidth.Point;
        return pageWidth - pageSetup.LeftMargin.Point - pageSetup.RightMargin.Point;
    }
}
