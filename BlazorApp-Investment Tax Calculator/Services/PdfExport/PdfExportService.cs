using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Pdf;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService(TaxYearReportService taxYearReportService, TradeCalculationResult tradeCalculationResult,
    UkSection104Pools uKSection104Pools)
{
    /// <summary>
    /// Generates a PDF report that aggregates tax summary, section 104 history, and trade details for a given tax year.
    /// </summary>
    /// <param name="year">The tax year for which the PDF report is generated.</param>
    /// <returns>
    /// A MemoryStream containing the generated PDF document. The caller is responsible for disposing the stream.
    /// </returns>
    public MemoryStream CreatePdf(int year)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(tradeCalculationResult, taxYearReportService);
        ISection allTradesListSection = new AllTradesListSection(tradeCalculationResult);
        ISection section104Section = new Section104HistorySection(uKSection104Pools);
        ISection endOfYearSection104StatusSection = new EndOfYearSection104StatusSection(uKSection104Pools);
        var document = new Document();
        List<ISection> sections = [yearSummarySection, endOfYearSection104StatusSection, section104Section, allTradesListSection];
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
