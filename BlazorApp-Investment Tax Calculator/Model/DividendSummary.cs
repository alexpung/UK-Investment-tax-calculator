using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model;

public record DividendSummary
{
    public required CountryCode CountryOfOrigin { get; set; }
    public virtual required int TaxYear { get; set; }
    public required List<Dividend> RelatedDividendsAndTaxes { get; set; }
    public required List<InterestIncome> RelatedInterestIncome { get; set; }
    public virtual WrappedMoney TotalTaxableDividend => (from dividend in RelatedDividendsAndTaxes
                                                         where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND
                                                         select dividend.Proceed.BaseCurrencyAmount).Sum();
    public virtual WrappedMoney TotalForeignTaxPaid => (from dividend in RelatedDividendsAndTaxes
                                                        where dividend.DividendType is DividendType.WITHHOLDING
                                                        select dividend.Proceed.BaseCurrencyAmount).Sum();

    public virtual WrappedMoney TotalTaxableSavingInterest => (from interest in RelatedInterestIncome
                                                               where interest.InterestType is InterestType.SAVINGS
                                                               select interest.Amount.BaseCurrencyAmount).Sum();

    public virtual WrappedMoney TotalTaxableBondInterest => (from interest in RelatedInterestIncome
                                                             where interest.InterestType is InterestType.BOND
                                                             select interest.Amount.BaseCurrencyAmount).Sum();
    public virtual WrappedMoney TotalAccurredIncomeProfit => (from interest in RelatedInterestIncome
                                                              where interest.InterestType is InterestType.ACCURREDINCOMEPROFIT
                                                              select interest.Amount.BaseCurrencyAmount).Sum();

    /// <summary>
    /// Loss is represented as negative number here
    /// </summary>
    public virtual WrappedMoney TotalAccurredIncomeLoss => (from interest in RelatedInterestIncome
                                                            where interest.InterestType is InterestType.ACCURREDINCOMELOSS
                                                            select interest.Amount.BaseCurrencyAmount).Sum();
    public virtual WrappedMoney TotalInterestIncome => TotalTaxableSavingInterest + TotalTaxableBondInterest + TotalAccurredIncomeProfit + TotalAccurredIncomeLoss;

}
