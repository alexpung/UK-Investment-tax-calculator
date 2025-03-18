using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Pdf;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService(TaxYearCgtByTypeReportService taxYearCgtByTypeReportService, TaxYearReportService taxYearReportService, TradeCalculationResult tradeCalculationResult,
    UkSection104Pools uKSection104Pools)
{
    public MemoryStream CreatePdf(int year)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(tradeCalculationResult, taxYearReportService);
        ISection allTradesListSection = new AllTradesListSection(tradeCalculationResult);
        ISection section104Section = new Section104HistorySection(uKSection104Pools);
        var document = new Document();
        List<ISection> sections = [yearSummarySection, section104Section, allTradesListSection];
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
        var stream = new MemoryStream(); // Caller is responsible for disposing
        try
        {
            pdfRenderer.PdfDocument.Save(stream, false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
        return stream;
    }
}
