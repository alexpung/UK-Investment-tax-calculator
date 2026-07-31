using InvestmentTaxCalculator.Model;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

/// <summary>
/// Lists the identity of each company/share in the imported data: the name used in the report, the full company
/// name(s), and every ticker/ISIN combination with the date it was last seen, so renames, Newco insertions and
/// exchange suffix variations are documented in the report.
/// </summary>
public class CompanyInformationSection(ShareIdentityRegistry shareIdentityRegistry) : ISection
{
    public string Name { get; set; } = "Company Information";
    public string Title { get; set; } = "Company Information";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);

        List<ShareIdentity> identities = [.. shareIdentityRegistry.Identities
            .Where(identity => identity.Isins.Count > 0 || identity.FullNames.Count > 0 || identity.Tickers.Count > 1)
            .OrderBy(identity => identity.PrimaryTicker, StringComparer.OrdinalIgnoreCase)];

        if (identities.Count == 0)
        {
            section.AddParagraph("No company information is available.");
            return section;
        }

        section.AddParagraph("Tickers and ISINs of the same company are listed together with the date each " +
            "combination was last seen in the imported data, so the entry with the latest date is the current " +
            "ticker/ISIN and earlier entries are former ones (e.g. before a rename or Newco insertion).");

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (18, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (12, ParagraphAlignment.Left),
            (8, ParagraphAlignment.Right)]);

        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Name in report");
        headerRow.Cells[1].AddParagraph("Company name(s)");
        headerRow.Cells[2].AddParagraph("Ticker");
        headerRow.Cells[3].AddParagraph("ISIN");
        headerRow.Cells[4].AddParagraph("Last seen");

        foreach (ShareIdentity identity in identities)
        {
            IReadOnlyList<ShareIdentityObservation> observations = identity.Observations;
            if (observations.Count == 0)
            {
                observations = [.. identity.Tickers.Select(ticker => new ShareIdentityObservation(ticker, string.Empty, DateTime.MinValue))];
            }
            Row firstRow = table.AddRow();
            firstRow.Cells[0].AddParagraph(identity.PrimaryTicker);
            firstRow.Cells[1].AddParagraph(string.Join("\n", identity.FullNames));
            for (int i = 0; i < observations.Count; i++)
            {
                Row row = i == 0 ? firstRow : table.AddRow();
                row.Cells[2].AddParagraph(observations[i].Ticker);
                row.Cells[3].AddParagraph(observations[i].Isin);
                row.Cells[4].AddParagraph(observations[i].LastSeen == DateTime.MinValue ? "" : observations[i].LastSeen.ToShortDateString());
            }
        }
        return section;
    }
}
