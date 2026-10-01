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
                                                         where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND or DividendType.EXCESS_REPORTABLE_INCOME
                                                         select dividend.Proceed.BaseCurrencyAmount).Sum();
    public virtual WrappedMoney TotalForeignTaxPaid => (from dividend in RelatedDividendsAndTaxes
                                                        where dividend.DividendType is DividendType.WITHHOLDING
                                                        select dividend.Proceed.BaseCurrencyAmount).Sum();

    public virtual WrappedMoney TotalExcessReportableIncomeDividend => (from dividend in RelatedDividendsAndTaxes
                                                                        where dividend.DividendType is DividendType.EXCESS_REPORTABLE_INCOME
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

    public virtual WrappedMoney TotalExcessReportableIncomeInterest => (from interest in RelatedInterestIncome
                                                                        where interest.InterestType is InterestType.EXCESSREPORTABLEINCOME
                                                                        select interest.Amount.BaseCurrencyAmount).Sum();
    public virtual WrappedMoney TotalEtfDividendIncome => (from interest in RelatedInterestIncome
                                                           where interest.InterestType is InterestType.ETFDIVIDEND
                                                           select interest.Amount.BaseCurrencyAmount).Sum();

    public virtual WrappedMoney TotalInterestIncome => (from interest in RelatedInterestIncome
                                                        where !interest.IsInterestExpense
                                                        select interest.Amount.BaseCurrencyAmount).Sum();

    /// <summary>
    /// Interest paid, represented as negative number. For reference only - not deductible and not included in <see cref="TotalInterestIncome"/>.
    /// </summary>
    public virtual WrappedMoney TotalInterestExpense => (from interest in RelatedInterestIncome
                                                         where interest.IsInterestExpense
                                                         select interest.Amount.BaseCurrencyAmount).Sum();

    public IEnumerable<InterestIncome> InterestIncomeExcludingExpenses => RelatedInterestIncome.Where(i => !i.IsInterestExpense);
    public IEnumerable<InterestIncome> InterestExpenses => RelatedInterestIncome.Where(i => i.IsInterestExpense);

}
