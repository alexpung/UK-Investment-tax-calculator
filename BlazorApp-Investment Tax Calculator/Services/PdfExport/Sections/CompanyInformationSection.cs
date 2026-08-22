using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

/// <summary>
/// Lists the identity of each company/share relevant to the reported tax year: the name used in the report, the
/// full company name(s), and every ticker/ISIN combination with the date it was last seen, so renames, Newco
/// insertions and exchange suffix variations are documented in the report. A company is relevant when it has a
/// tax event dated in the tax year or is still held in a Section 104 pool at the end of it, so the section stays
/// limited to companies appearing elsewhere in the report instead of every company ever imported.
/// </summary>
public class CompanyInformationSection(ShareIdentityRegistry shareIdentityRegistry, TaxEventLists taxEventLists,
    ITaxYear taxYearConverter, UkSection104Pools section104Pools) : ISection
{
    public string Name { get; set; } = "Company Information";
    public string Title { get; set; } = "Company Information";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);

        HashSet<ShareIdentity> relevantIdentities = GetIdentitiesRelevantToTaxYear(taxYear);
        List<ShareIdentity> identities = [.. shareIdentityRegistry.Identities
            .Where(relevantIdentities.Contains)
            .Where(identity => identity.Isins.Count > 0 || identity.FullNames.Count > 0 || identity.Tickers.Count > 1)
            .OrderBy(identity => identity.UniqueTicker, StringComparer.OrdinalIgnoreCase)];

        if (identities.Count == 0)
        {
            section.AddParagraph("No company information is available for this tax year.");
            return section;
        }

        section.AddParagraph("Companies with a tax event in this tax year or still held at the end of it are " +
            "listed. Tickers and ISINs of the same company are listed together with the date each " +
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
            firstRow.Cells[0].AddParagraph(identity.UniqueTicker);
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

    /// <summary>
    /// The identities with a tax event dated in the given tax year, plus those still held in a Section 104 pool at
    /// the end of it (a held company appears in the Section 104 status section even without any event that year).
    /// </summary>
    private HashSet<ShareIdentity> GetIdentitiesRelevantToTaxYear(int taxYear)
    {
        HashSet<ShareIdentity> relevantIdentities = [];
        foreach (TaxEvent taxEvent in taxEventLists.AllEvents)
        {
            if (taxEvent.ShareIdentity is not null && taxYearConverter.ToTaxYear(taxEvent.Date) == taxYear)
            {
                relevantIdentities.Add(taxEvent.ShareIdentity);
            }
        }
        foreach (string assetName in section104Pools.GetEndOfYearSection104s(taxYear).Keys)
        {
            ShareIdentity? identity = shareIdentityRegistry.ResolveByTicker(assetName);
            if (identity is not null)
            {
                relevantIdentities.Add(identity);
            }
        }
        return relevantIdentities;
    }
}
