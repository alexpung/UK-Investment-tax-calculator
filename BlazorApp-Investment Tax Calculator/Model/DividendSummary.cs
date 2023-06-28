using Enum;
using NMoneys;
using System.Globalization;

namespace Model;

public record DividendSummary
{
    public required RegionInfo CountryOfOrigin { get; set; }
    public required int TaxYear { get; set; }
    public required List<Dividend> RelatedDividendsAndTaxes { get; set; }
    public Money TotalTaxableDividend => (from dividend in RelatedDividendsAndTaxes
                                          where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND
                                          select dividend.Proceed.BaseCurrencyAmount)
                                            .Sum();
    public Money TotalForeignTaxPaid => (from dividend in RelatedDividendsAndTaxes
                                         where dividend.DividendType is DividendType.WITHHOLDING
                                         select dividend.Proceed.BaseCurrencyAmount).Sum();
}
