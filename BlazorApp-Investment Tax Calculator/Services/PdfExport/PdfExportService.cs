using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService(TaxYearCgtByTypeReportService taxYearCgtByTypeReportService, TaxYearReportService taxYearReportService, TradeCalculationResult tradeCalculationResult)
{
    public MemoryStream CreatePdf(int year)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(taxYearCgtByTypeReportService, taxYearReportService);
        ISection allTradesListSection = new AllTradesListSection(tradeCalculationResult);
        GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        var document = new Document();
        List<ISection> sections = [yearSummarySection, allTradesListSection];
        foreach (var ISection in sections)
        {
            Section pdfSection = document.AddSection();
            ISection.WriteSection(pdfSection, year);
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
