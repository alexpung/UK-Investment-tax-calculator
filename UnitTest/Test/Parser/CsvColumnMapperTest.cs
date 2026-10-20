using InvestmentTaxCalculator.Parser;

namespace UnitTest.Test.Parser;

public class CsvColumnMapperTest
{
    private static readonly List<CsvColumnDefinition> _columns =
    [
        new("Time", "Time", true, ["Time", "Time (UTC)"]),
        new("Total", "Total", true, ["Total (GBP)", "Total"]),
        new("Notes", "Notes", false, ["Notes"]),
    ];

    [Fact]
    public void TestAutoMapMatchesKnownHeadersIgnoringCaseAndSpaces()
    {
        CsvColumnMap map = CsvColumnMapper.AutoMap(_columns, [" time (utc) ", "TOTAL", "New column"]);

        map["Time"].ShouldBe(" time (utc) ");
        map["Total"].ShouldBe("TOTAL");
        map.TryGetHeader("Notes", out _).ShouldBeFalse();
        CsvColumnMapper.GetMissingRequiredColumns(_columns, map).ShouldBeEmpty();
    }

    [Fact]
    public void TestAutoMapPrefersPreferredHeaderWhenPresent()
    {
        CsvColumnMap preferred = new();
        preferred.Set("Total", "Amount");
        preferred.Set("Time", "Missing header");

        CsvColumnMap map = CsvColumnMapper.AutoMap(_columns, ["Time", "Total", "Amount"], preferred);

        map["Total"].ShouldBe("Amount");
        map["Time"].ShouldBe("Time");
    }

    [Fact]
    public void TestMissingRequiredColumnsAreReported()
    {
        CsvColumnMap map = CsvColumnMapper.AutoMap(_columns, ["Time", "Notes"]);

        CsvColumnMapper.GetMissingRequiredColumns(_columns, map).Select(c => c.Key).ShouldBe(["Total"]);
        Should.Throw<InvalidDataException>(() => map["Total"]);
    }

    [Fact]
    public void TestReadPreview()
    {
        (string[] headers, List<string[]> rows) = CsvColumnMapper.ReadPreview("A,B\n1,2\n3,4\n5,6", 2);

        headers.ShouldBe(["A", "B"]);
        rows.Count.ShouldBe(2);
        rows[1].ShouldBe(["3", "4"]);
    }
}
