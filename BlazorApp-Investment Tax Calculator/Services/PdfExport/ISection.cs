using MigraDoc.DocumentObjectModel;

namespace InvestmentTaxCalculator.Services.PdfExport;

public interface ISection
{
    public string Name { get; set; } // Name of the section in PDF configuration
    public string Title { get; set; } // Title shown in the PDF document
    public Section WriteSection(Section section, int taxYear);
}
