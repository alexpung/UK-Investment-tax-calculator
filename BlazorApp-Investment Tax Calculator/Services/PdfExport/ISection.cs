using MigraDoc.DocumentObjectModel;

namespace InvestmentTaxCalculator.Services.PdfExport;

public interface ISection
{
    public string Name { get; set; }
    public string Title { get; set; }
    public Section WriteSection(Section section, int taxYear);
}
