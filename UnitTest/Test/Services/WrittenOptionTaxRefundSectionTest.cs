using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.Services;

public class WrittenOptionTaxRefundSectionTest
{
    [Fact]
    public void WriteSection_WrittenTaxYear_ListsRefundUnderOptionsWrittenInYear()
    {
        Section section = WriteSection(CalculateWrittenAndClosedInLaterYear(), 2022);

        List<DocumentObject> elements = GetContentAfterSubheadings(section);
        Table writtenInYearTable = elements[0].ShouldBeOfType<Table>();
        writtenInYearTable.Rows.Count.ShouldBe(3); // header, one refund, total
        GetCellText(writtenInYearTable.Rows[1].Cells[0]).ShouldBe("2022 - 2023");
        GetCellText(writtenInYearTable.Rows[1].Cells[1]).ShouldBe("2023 - 2024");
        GetCellText(writtenInYearTable.Rows[1].Cells[2]).ShouldBe(new WrappedMoney(600).ToString());
        GetCellText(writtenInYearTable.Rows[2].Cells[2]).ShouldBe(new WrappedMoney(600).ToString());
        GetParagraphText(elements[1].ShouldBeOfType<Paragraph>()).ShouldBe("None.");
    }

    [Fact]
    public void WriteSection_ClosingTaxYear_ListsRefundUnderOptionsClosedInYear()
    {
        Section section = WriteSection(CalculateWrittenAndClosedInLaterYear(), 2023);

        List<DocumentObject> elements = GetContentAfterSubheadings(section);
        GetParagraphText(elements[0].ShouldBeOfType<Paragraph>()).ShouldBe("None.");
        Table closedInYearTable = elements[1].ShouldBeOfType<Table>();
        closedInYearTable.Rows.Count.ShouldBe(3);
        GetCellText(closedInYearTable.Rows[1].Cells[0]).ShouldBe("2022 - 2023");
        GetCellText(closedInYearTable.Rows[2].Cells[2]).ShouldBe(new WrappedMoney(600).ToString());
    }

    [Fact]
    public void WriteSection_NoRefund_WritesNoteAndNone()
    {
        Section section = WriteSection(CalculateWrittenAndClosedInLaterYear(), 2021);

        section.Elements.OfType<Table>().ShouldBeEmpty();
        List<string> texts = [.. section.Elements.OfType<Paragraph>().Select(GetParagraphText)];
        texts.ShouldContain(WrittenOptionTaxRefundSection.ExplanationNote);
        texts.Count(text => text == "None.").ShouldBe(2);
    }

    private static Section WriteSection(TradeCalculationResult tradeCalculationResult, int taxYear)
    {
        Section section = new Document().AddSection();
        new WrittenOptionTaxRefundSection(tradeCalculationResult).WriteSection(section, taxYear);
        return section;
    }

    private static TradeCalculationResult CalculateWrittenAndClosedInLaterYear()
    {
        var writeOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 240119C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("19-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "GBP", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write AAPL call option"
        };
        var buyBackTrade = new OptionTrade
        {
            AssetName = "AAPL 240119C00150000",
            Date = DateTime.Parse("10-May-23 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("19-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(600, "GBP", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy back AAPL call option"
        };
        ResidencyStatusRecord residencyStatusRecord = new();
        TradeCalculationResult result = new(new UKTaxYear(), residencyStatusRecord);
        result.SetResult(TradeCalculationHelper.CalculateTrades([writeOptionTrade, buyBackTrade], out _, residencyStatusRecord));
        return result;
    }

    /// <summary>
    /// Returns the table or "None." paragraph that follows each of the two subheadings (elements after title, note and subheading).
    /// </summary>
    private static List<DocumentObject> GetContentAfterSubheadings(Section section)
    {
        List<DocumentObject> elements = [.. section.Elements.Cast<DocumentObject>()];
        elements.Count.ShouldBe(6);
        return [elements[3], elements[5]];
    }

    private static string GetCellText(Cell cell) =>
        string.Concat(cell.Elements.OfType<Paragraph>().Select(GetParagraphText));

    private static string GetParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Elements.OfType<Text>().Select(text => text.Content));
}
