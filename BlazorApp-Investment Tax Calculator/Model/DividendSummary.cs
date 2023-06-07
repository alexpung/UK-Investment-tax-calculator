using Enum;
using System.Globalization;

namespace Model;

public record DividendSummary
{
    public required RegionInfo CountryOfOrigin { get; set; }
    public required int TaxYear { get; set; }
    public required List<Dividend> RelatedDividendsAndTaxes { get; set; }
    public decimal TotalTaxableDividend => (from dividend in RelatedDividendsAndTaxes
                                            where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND
                                            select dividend.Proceed.BaseCurrencyAmount)
                                            .Sum();
    public decimal TotalForeignTaxPaid => (from dividend in RelatedDividendsAndTaxes
                                           where dividend.DividendType is DividendType.WITHHOLDING
                                           select dividend.Proceed.BaseCurrencyAmount).Sum();
}
