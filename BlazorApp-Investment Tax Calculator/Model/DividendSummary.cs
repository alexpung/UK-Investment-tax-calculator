using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;

namespace InvestmentTaxCalculator.Model;

public record DividendSummary
{
    public required RegionInfo CountryOfOrigin { get; set; }
    public virtual required int TaxYear { get; set; }
    public required List<Dividend> RelatedDividendsAndTaxes { get; set; }
    public virtual WrappedMoney TotalTaxableDividend => (from dividend in RelatedDividendsAndTaxes
                                                         where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND
                                                         select dividend.Proceed.BaseCurrencyAmount)
                                            .Sum();
    public virtual WrappedMoney TotalForeignTaxPaid => (from dividend in RelatedDividendsAndTaxes
                                                        where dividend.DividendType is DividendType.WITHHOLDING
                                                        select dividend.Proceed.BaseCurrencyAmount).Sum();
}
