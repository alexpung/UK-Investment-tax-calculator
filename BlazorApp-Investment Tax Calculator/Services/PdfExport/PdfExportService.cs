using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Fields;
using MigraDoc.Rendering;

using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService
{
    public MemoryStream CreatePdf()
    {
        return GeneratePdf("TEST", "HELLO!");
    }

    public MemoryStream GeneratePdf(string title, string content)
    {
        GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        // Create a new PDF document
        var document = new Document();
        var section = document.AddSection();
        var paragraph = section.AddParagraph();
        paragraph.Format.Font.Color = Colors.DarkBlue;
        paragraph.AddFormattedText("Hello, World! Welcome to Blazor Pdf Sharp Samples", TextFormat.Bold);
        var footer = section.Footers.Primary;
        paragraph = footer.AddParagraph();
        paragraph.Add(new DateField { Format = "yyyy/MM/dd HH:mm:ss" });
        paragraph.Format.Alignment = ParagraphAlignment.Center;

        var style = document.Styles[StyleNames.Normal]!;
        style.Font.Name = "Arial";

        // Create a renderer for the MigraDoc document.
        var pdfRenderer = new PdfDocumentRenderer
        {
            // Associate the MigraDoc document with a renderer.
            Document = document,
            PdfDocument =
                            {
                                // Change some settings before rendering the MigraDoc document.
                                PageLayout = PdfPageLayout.SinglePage,
                                ViewerPreferences =
                                {
                                    FitWindow = true
                                }
                            }
        };

        pdfRenderer.RenderDocument();
        var stream = new MemoryStream();
        pdfRenderer.PdfDocument.Save(stream, false);
        return stream;
    }
}
