using MigraDoc.DocumentObjectModel;

namespace InvestmentTaxCalculator.Services.PdfExport;

public static class Style
{
    public static Paragraph StyleTitle(Paragraph paragraph)
    {
        paragraph.Format.Font.Size = 16;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceAfter = 6;
        return paragraph;
    }
}
