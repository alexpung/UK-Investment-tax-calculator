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
}
