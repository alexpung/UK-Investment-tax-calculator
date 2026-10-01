using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace UnitTest.Test.Services;

public class InterestExpenseSectionTest
{
    [Fact]
    public void WriteSection_ListsOnlyInterestExpenses_WithReferenceOnlyNote()
    {
        DividendCalculationResult result = CreateResult(
            new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(10m, "GBP", 1m, "Credit interest") },
            new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 3), InterestType = InterestType.INTERESTEXPENSE, Amount = new DescribedMoney(-4m, "GBP", 1m, "Margin interest") });

        Section section = new Document().AddSection();
        new InterestExpenseSection(result).WriteSection(section, 2024);

        GetParagraphTexts(section).ShouldContain(InterestExpenseSection.ReferenceOnlyNote);
        Table table = section.Elements.OfType<Table>().Single();
        List<string> descriptions = [.. table.Rows.Cast<Row>().Skip(1).Select(row => GetCellText(row.Cells[2]))];
        descriptions.ShouldContain("Margin interest");
        descriptions.ShouldNotContain("Credit interest");
        Row totalRow = table.Rows[table.Rows.Count - 1];
        GetCellText(totalRow.Cells[4]).ShouldBe(new WrappedMoney(-4m, "GBP").ToString());
    }

    [Fact]
    public void WriteSection_NoExpenses_WritesNoteAndEmptyMessage()
    {
        DividendCalculationResult result = CreateResult(
            new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 1), InterestType = InterestType.SAVINGS, Amount = new DescribedMoney(10m, "GBP", 1m) });

        Section section = new Document().AddSection();
        new InterestExpenseSection(result).WriteSection(section, 2024);

        section.Elements.OfType<Table>().ShouldBeEmpty();
        List<string> texts = GetParagraphTexts(section);
        texts.ShouldContain(InterestExpenseSection.ReferenceOnlyNote);
        texts.ShouldContain("No interest expenses recorded in the tax year 2024 - 2025.");
    }

    [Fact]
    public void InterestIncomeSummarySection_ExcludesInterestExpenses()
    {
        DividendCalculationResult result = CreateResult(
            new InterestIncome { AssetName = "Broker interest", Date = new DateTime(2025, 1, 3), InterestType = InterestType.INTERESTEXPENSE, Amount = new DescribedMoney(-4m, "GBP", 1m, "Margin interest") });

        Section section = new Document().AddSection();
        new InterestIncomeSummarySection(result).WriteSection(section, 2024);

        section.Elements.OfType<Table>().ShouldBeEmpty();
        GetParagraphTexts(section).ShouldContain("No interest income received in the tax year 2024 - 2025.");
    }

    private static DividendCalculationResult CreateResult(params InterestIncome[] interests)
    {
        DividendSummary summary = new()
        {
            CountryOfOrigin = CountryCode.GetRegionByTwoDigitCode("GB"),
            TaxYear = 2024,
            RelatedDividendsAndTaxes = [],
            RelatedInterestIncome = [.. interests]
        };
        DividendCalculationResult result = new();
        result.SetResult([summary]);
        return result;
    }

    private static List<string> GetParagraphTexts(Section section) =>
        [.. section.Elements.OfType<Paragraph>().Select(GetParagraphText)];

    private static string GetCellText(Cell cell) =>
        string.Concat(cell.Elements.OfType<Paragraph>().Select(GetParagraphText));

    private static string GetParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Elements.OfType<Text>().Select(text => text.Content));
}
