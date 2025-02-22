using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService(TaxYearCgtByTypeReportService taxYearCgtByTypeReportService)
{
    public MemoryStream CreatePdf()
    {
        ISection section = new YearlyTaxSummarySection(taxYearCgtByTypeReportService);
        return GeneratePdf([section]);
    }

    public MemoryStream GeneratePdf(List<ISection> sections)
    {
        GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        var document = new Document();
        foreach (var ISection in sections)
        {
            Section pdfSection = document.AddSection();
            ISection.WriteSection(pdfSection, 2023);
        }
        var pdfRenderer = new PdfDocumentRenderer
        {
            Document = document,
            PdfDocument =
                            {
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
