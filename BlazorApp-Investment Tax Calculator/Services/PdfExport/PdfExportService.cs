using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using PdfSharp.Pdf;

namespace InvestmentTaxCalculator.Services.PdfExport;

public class PdfExportService
{
    public List<ISection> AllSections { get; set; }
    public string[]? SelectedSections { get; set; }
    public PdfExportService(TaxYearReportService taxYearReportService, TradeCalculationResult tradeCalculationResult,
        UkSection104Pools uKSection104Pools, DividendCalculationResult dividendCalculationResult)
    {
        ISection yearSummarySection = new YearlyTaxSummarySection(tradeCalculationResult, taxYearReportService);
        ISection allTradesListSection = new AllTradesListInYearSection(tradeCalculationResult);
        ISection section104Section = new Section104HistorySection(uKSection104Pools);
        ISection endOfYearSection104StatusSection = new EndOfYearSection104StatusSection(uKSection104Pools);
        ISection dividendSummarySection = new DividendSummarySection(dividendCalculationResult);
        ISection disposalDetailSection = new DisposalDetailSection(tradeCalculationResult);
        ISection interestIncomeSummarySection = new InterestIncomeSummarySection(dividendCalculationResult);
        AllSections = [
            yearSummarySection,
            dividendSummarySection,
            interestIncomeSummarySection,
            disposalDetailSection,
            endOfYearSection104StatusSection,
            section104Section,
            allTradesListSection
            ];
        SelectedSections = [.. AllSections.Select(section => section.Name)];
    }
    public MemoryStream CreatePdf(int year)
    {
        if (SelectedSections is null || SelectedSections.Length == 0)
        {
            throw new InvalidOperationException("No sections selected for export");
        }
        //sort the sections in the order they appear in AllSections, as ordering of SelectedSections is not updated in sync with UI
        SelectedSections = [.. SelectedSections.OrderBy(section => AllSections.FindIndex(s => s.Name == section))];
        var document = new Document();
        document.Info.Title = $"Investment Tax Report {year} - {year + 1}";
        document.Info.Subject = $"UK investment tax calculations for the tax year 6 April {year} to 5 April {year + 1}";
        DefineDocumentStyles(document);

        foreach (var sectionName in SelectedSections)
        {
            Section pdfSection = document.AddSection();
            pdfSection.PageSetup = SetPageSetup(document);
            if (sectionName == SelectedSections[0])
            {
                // The cover block replaces the running header on the first page; headers and
                // footers are inherited by all following sections.
                pdfSection.PageSetup.DifferentFirstPageHeaderFooter = true;
                AddPageHeader(pdfSection, year);
                AddPageFooter(pdfSection);
                AddDocumentTitle(pdfSection, year);
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

    private static void DefineDocumentStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Open Sans";
        normal.Font.Size = 9;

        var header = document.Styles[StyleNames.Header]!;
        header.Font.Size = 8;
        header.Font.Color = Style.MutedTextColour;

        var footer = document.Styles[StyleNames.Footer]!;
        footer.Font.Size = 8;
        footer.Font.Color = Style.MutedTextColour;
    }

    private static void AddPageHeader(Section section, int year)
    {
        Paragraph headerParagraph = section.Headers.Primary.AddParagraph();
        headerParagraph.Format.AddTabStop(Unit.FromPoint(Style.GetContentWidth(section)), TabAlignment.Right);
        headerParagraph.Format.Borders.Bottom = new Border { Width = Unit.FromPoint(0.5), Color = Style.AccentColour };
        headerParagraph.Format.Borders.DistanceFromBottom = Unit.FromPoint(3);
        headerParagraph.AddText("Investment Tax Report");
        headerParagraph.AddTab();
        headerParagraph.AddText($"Tax Year {year} - {year + 1}");
    }

    private static void AddPageFooter(Section section)
    {
        Paragraph footerParagraph = section.Footers.Primary.AddParagraph();
        footerParagraph.Format.AddTabStop(Unit.FromPoint(Style.GetContentWidth(section)), TabAlignment.Right);
        footerParagraph.Format.Borders.Top = new Border { Width = Unit.FromPoint(0.5), Color = Style.TableBorderColour };
        footerParagraph.Format.Borders.DistanceFromTop = Unit.FromPoint(3);
        footerParagraph.AddText($"Generated on {DateTime.Now:d MMMM yyyy} by https://alexpung.github.io/UK-Investment-tax-calculator/ version {AppVersionInfo.Current}");
        footerParagraph.AddTab();
        footerParagraph.AddText("Page ");
        footerParagraph.AddPageField();
        footerParagraph.AddText(" of ");
        footerParagraph.AddNumPagesField();
        // Keep the footer on the cover page even though its running header is suppressed
        section.Footers.FirstPage = section.Footers.Primary.Clone();
    }

    private static void AddDocumentTitle(Section section, int year)
    {
        Paragraph title = section.AddParagraph("Investment Tax Report");
        Style.StyleTopTitle(title);
        Paragraph subtitle = section.AddParagraph($"Tax Year {year} - {year + 1} (6 April {year} to 5 April {year + 1})");
        subtitle.Format.Font.Size = 12;
        subtitle.Format.Font.Color = Style.MutedTextColour;
        subtitle.Format.SpaceAfter = Unit.FromPoint(2);
        Paragraph rule = section.AddParagraph();
        rule.Format.Borders.Bottom = new Border { Width = Unit.FromPoint(1.5), Color = Style.PrimaryColour };
        rule.Format.SpaceAfter = Unit.FromPoint(16);
    }

    private PageSetup SetPageSetup(Document document)
    {
        PageSetup pageSetup = document.DefaultPageSetup.Clone();
        pageSetup.Orientation = Orientation.Landscape;
        pageSetup.BottomMargin = Unit.FromInch(1);
        pageSetup.TopMargin = Unit.FromInch(1);
        pageSetup.LeftMargin = Unit.FromInch(1);
        pageSetup.RightMargin = Unit.FromInch(1);
        pageSetup.HeaderDistance = Unit.FromInch(0.5);
        pageSetup.FooterDistance = Unit.FromInch(0.5);
        return pageSetup;
    }
}
