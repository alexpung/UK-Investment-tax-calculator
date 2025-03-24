﻿using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Pdf;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService
{
    public List<ISection> AllSections { get; set; }
    public string[]? SelectedSections { get; set; } = [];
    public PdfExportService(TaxYearReportService taxYearReportService, TradeCalculationResult tradeCalculationResult,
        UkSection104Pools uKSection104Pools, DividendCalculationResult dividendCalculationResult)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(tradeCalculationResult, taxYearReportService);
        ISection allTradesListSection = new AllTradesListInYearSection(tradeCalculationResult);
        ISection section104Section = new Section104HistorySection(uKSection104Pools);
        ISection endOfYearSection104StatusSection = new EndOfYearSection104StatusSection(uKSection104Pools);
        ISection dividendSummarySection = new DividendSummarySection(dividendCalculationResult);
        AllSections = [
            yearSummarySection,
            dividendSummarySection,
            endOfYearSection104StatusSection,
            section104Section,
            allTradesListSection
            ];
    }
    public MemoryStream CreatePdf(int year)
    {
        if (SelectedSections is null || SelectedSections.Length == 0)
        {
            throw new InvalidOperationException("No sections selected for export");
        }
        SelectedSections = [.. SelectedSections.OrderBy(section => AllSections.FindIndex(s => s.Name == section))];
        var document = new Document();

        foreach (var sectionName in SelectedSections)
        {
            Section pdfSection = document.AddSection();
            if (sectionName == SelectedSections[0])
            {
                AddDocumentTitle(pdfSection, $"Investment Tax Report for year {year}");
            }
            AllSections.Single(section => section.Name == sectionName).WriteSection(pdfSection, year);
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

    private static void AddDocumentTitle(Section section, string title)
    {
        Paragraph paragraph = section.AddParagraph(title);
        Style.StyleTopTitle(paragraph);
    }
}
