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
    public MemoryStream CreatePdf(int year)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(tradeCalculationResult, taxYearReportService);
        ISection allTradesListSection = new AllTradesListInYearSection(tradeCalculationResult);
        ISection section104Section = new Section104HistorySection(uKSection104Pools);
        ISection endOfYearSection104StatusSection = new EndOfYearSection104StatusSection(uKSection104Pools);
        var document = new Document();
        List<ISection> sections = [yearSummarySection, endOfYearSection104StatusSection, section104Section, allTradesListSection];
        foreach (var section in sections)
        {
            Section pdfSection = document.AddSection();
            if (section == sections[0])
            {
                AddDocumentTitle(pdfSection, $"Investment Tax Report for year {year}");
            }
            section.WriteSection(pdfSection, year);
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

    private static Section AddDocumentTitle(Section section, string title)
    {
        Paragraph paragraph = section.AddParagraph(title);
        Style.StyleTopTitle(paragraph);
        return section;
    }
}
